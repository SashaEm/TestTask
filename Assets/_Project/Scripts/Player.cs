
using System;
using System.Collections;
using UnityEngine;

public class Player : SphereEntity
{
    [Header("Shrinking")]
    [SerializeField, Min(0f)]
    private float _shrinkDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float _minimalRadius = 0.5f;

    private float _initialRadius;
    private float _targetRadius;

    private Coroutine _shrinkingCoroutine;
    private bool _isShrinking;

    public bool IsShrinking => _isShrinking;
    public bool IsDead { get; private set; }

    public event Action OnDeath;

    public void Initialize(float initialRadius)
    {
        if (_shrinkingCoroutine != null)
        {
            StopCoroutine(_shrinkingCoroutine);
            _shrinkingCoroutine = null;
        }

        _initialRadius = initialRadius;
        _targetRadius = initialRadius;

        _isShrinking = false;
        IsDead = false;

        StopMovement();
        SetRadius(_initialRadius);
    }

    public void MoveForDistance(Vector3 direction, float distance)
    {
        if (IsDead || IsShrinking || IsMoving)
            return;

        StartMovement(direction, distance);
    }

    public void Stop()
    {
        StopMovement();
    }

    public bool TryConsumeProjectileVolume(float projectileRadius)
    {
        if (IsDead || IsShrinking || IsMoving ||
            projectileRadius <= 0f ||
            projectileRadius > CurrentRadius)
            return false;

        float playerVolume =
            CurrentRadius * CurrentRadius * CurrentRadius;

        float projectileVolume =
            projectileRadius * projectileRadius * projectileRadius;

        float remainingVolume = Mathf.Max(
            0f,
            playerVolume - projectileVolume
        );

        float newRadius =
            Mathf.Pow(remainingVolume, 1f / 3f);

        ShrinkRadius(CurrentRadius - newRadius);

        return true;
    }

    public void ShrinkRadius(float subtractRadius)
    {
        if (IsDead || IsShrinking || subtractRadius < 0f)
            return;

        _targetRadius = Mathf.Max(
            0f,
            CurrentRadius - subtractRadius
        );

        UpdateSphere();
    }

    private void UpdateSphere(bool lerp = true)
    {
        if (_shrinkingCoroutine != null)
        {
            StopCoroutine(_shrinkingCoroutine);
            _shrinkingCoroutine = null;
        }

        if (!lerp || _shrinkDuration <= 0f)
        {
            SetRadius(_targetRadius);
            CheckDeath();
            return;
        }

        _isShrinking = true;

        _shrinkingCoroutine =
            StartCoroutine(ShrinkCoroutine(_targetRadius));
    }

    private IEnumerator ShrinkCoroutine(float targetRadius)
    {
        float startRadius = CurrentRadius;
        float elapsedTime = 0f;

        while (elapsedTime < _shrinkDuration)
        {
            elapsedTime = Mathf.Min(
                elapsedTime + Time.deltaTime,
                _shrinkDuration
            );

            float t = elapsedTime / _shrinkDuration;

            SetRadius(Mathf.Lerp(startRadius, targetRadius, t));

            if (CurrentRadius < _minimalRadius)
            {
                _isShrinking = false;
                _shrinkingCoroutine = null;

                CheckDeath();
                yield break;
            }

            yield return null;
        }

        SetRadius(targetRadius);

        _isShrinking = false;
        _shrinkingCoroutine = null;

        CheckDeath();
    }

    private void CheckDeath()
    {
        if (IsDead || CurrentRadius >= _minimalRadius)
            return;

        IsDead = true;
        StopMovement();

        OnDeath?.Invoke();
    }
}
