
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLoopManager _playerLoopManager;
    [SerializeField] private ObstacleGenerator _obstacleGenerator;
    [SerializeField] private CanvasGroup _screenFade;

    [Header("Transition")]
    [SerializeField, Min(0f)]
    private float _fadeDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float _blackScreenDuration = 0.2f;

    private bool _isRestarting;

    private void Awake()
    {
        if (_screenFade != null)
        {
            _screenFade.alpha = 0f;
            _screenFade.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        if (_playerLoopManager == null)
        {
            Debug.LogError("PlayerLoopManager is missing.");
            return;
        }

        _playerLoopManager.OnLevelFinished += HandleLevelFinished;
        _playerLoopManager.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDestroy()
    {
        if (_playerLoopManager == null)
            return;

        _playerLoopManager.OnLevelFinished -= HandleLevelFinished;
        _playerLoopManager.OnPlayerDied -= HandlePlayerDeath;
    }

    private void HandleLevelFinished()
    {
        Debug.Log("Level completed.");
        RestartLevel();
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("Player died.");
        RestartLevel();
    }

    public void RestartLevel()
    {
        if (_isRestarting)
            return;

        if (_playerLoopManager == null ||
            _obstacleGenerator == null ||
            _screenFade == null)
        {
            Debug.LogError(
                "GameManager references are not configured."
            );
            return;
        }

        StartCoroutine(RestartSequence());
    }

    private IEnumerator RestartSequence()
    {
        _isRestarting = true;

        _playerLoopManager.SetInputLocked(true);
        _screenFade.blocksRaycasts = true;

        // 1. Cover the screen.
        yield return FadeTo(1f);

        // 2. Remove projectiles and obstacles.
        _playerLoopManager.ClearProjectiles();
        _obstacleGenerator.Clear();

        // Allow Destroy() to finish at the end of the frame.
        yield return null;

        // 3. Generate a new random seed.
        int newSeed = Random.Range(0, int.MaxValue);

        if (newSeed == _obstacleGenerator.Seed)
            newSeed = (newSeed + 1) % int.MaxValue;

        _obstacleGenerator.Seed = newSeed;

        // 4. Generate the new level.
        _obstacleGenerator.Generate();

        // 5. Reset player position, rotation, and radius.
        _playerLoopManager.ResetPlayerToStart();

        // Ensure new collider transforms are synchronized.
        Physics.SyncTransforms();

        // Give newly instantiated objects a frame to initialize.
        yield return null;

        if (_blackScreenDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                _blackScreenDuration
            );
        }

        // 6. Reveal the new level.
        yield return FadeTo(0f);

        _screenFade.blocksRaycasts = false;
        _playerLoopManager.SetInputLocked(false);

        _isRestarting = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (_fadeDuration <= 0f)
        {
            _screenFade.alpha = targetAlpha;
            yield break;
        }

        float initialAlpha = _screenFade.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsedTime / _fadeDuration
            );

            _screenFade.alpha = Mathf.Lerp(
                initialAlpha,
                targetAlpha,
                t
            );

            yield return null;
        }

        _screenFade.alpha = targetAlpha;
    }
}
