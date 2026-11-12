
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoopManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private bool _createPlayerOnStart = true;
    [SerializeField] private Player _playerPrefab;
    [SerializeField] private Player _scenePlayer;
    [SerializeField] private Transform _playerSpawnPoint;

    [Header("Projectile")]
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField, Min(0.01f)] private float _minProjectileRadius = 0.2f;
    [SerializeField, Min(0.01f)] private float _maxProjectileRadius = 1f;
    [SerializeField, Min(0f)] private float _chargeGrowthPerSecond = 0.7f;

    [Header("Obstacle Detection")]
    [SerializeField] private LayerMask _obstacleLayers;
    [SerializeField, Min(0.01f)] private float _desiredObstacleDistance = 2f;
    [SerializeField, Min(0.01f)] private float _maxDetectionDistance = 100f;

    [Header("Finish")]
    [SerializeField] private FinishLine _finishLine;
    [SerializeField, Min(0.01f)] private float _finishOverrunDistance = 1f;

    private const float DistanceTolerance = 0.01f;

    private PlayerConfig currentPlayerConfig;
    private Player _player;

    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private bool _inputLocked;

    private readonly List<Projectile> _spawnedProjectiles = new();

    public event Action OnLevelFinished;
    public event Action OnPlayerDied;

    public Player CurrentPlayer => _player;

    public float PlayerRadius =>
        _player != null ? _player.Radius : 0f;

    public Vector3 PlayerPosition =>
        _player != null ? _player.transform.position : Vector3.zero;

    public Vector3 PlayerForward =>
        _player != null ? _player.transform.forward : Vector3.forward;

    public float MinProjectileRadius => _minProjectileRadius;

    public float MaxProjectileRadius =>
        Mathf.Min(_maxProjectileRadius, PlayerRadius);

    public float ChargeGrowthPerSecond => _chargeGrowthPerSecond;

    
    public bool isProjectileAllowed
    {
        get
        {
            if (_inputLocked ||
                _player == null ||
                _player.IsMoving ||
                _player.IsShrinking ||
                _player.IsDead ||
                HasProjectileInFlight ||
                _projectilePrefab == null ||
                MaxProjectileRadius < MinProjectileRadius)
                return false;

            float detectionDistance = GetFireDetectionDistance();

            if (!TryFindObstacle(
                    _player.Radius,
                    detectionDistance,
                    out RaycastHit hit))
                return false;

            return Mathf.Abs(
                hit.distance - _desiredObstacleDistance
            ) <= DistanceTolerance;
        }
    }
    
    private bool HasProjectileInFlight
    {
        get
        {
            foreach (Projectile projectile in _spawnedProjectiles)
            {
                if (projectile != null && projectile.IsMoving)
                    return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        LoadPlayerConfig();
    }

    private void Start()
    {
        if (currentPlayerConfig == null)
            return;

        if (_createPlayerOnStart)
        {
            if (_playerPrefab == null)
            {
                Debug.LogError("Player prefab is missing.");
                return;
            }

            Transform spawn = _playerSpawnPoint != null
                ? _playerSpawnPoint
                : transform;

            _player = Instantiate(
                _playerPrefab,
                spawn.position,
                spawn.rotation
            );
        }
        else
        {
            _player = _scenePlayer;
        }

        if (_player == null)
        {
            Debug.LogError("Player reference is missing.");
            return;
        }

        Transform start = _playerSpawnPoint != null
            ? _playerSpawnPoint
            : _player.transform;

        _startPosition = start.position;
        _startRotation = start.rotation;

        _player.transform.SetPositionAndRotation(
            _startPosition,
            _startRotation
        );

        _player.Initialize(currentPlayerConfig.InitialPlayerRadius);
        _player.OnDeath += HandlePlayerDeath;

        if (_finishLine != null)
        {
            _finishLine.ResetFor(_player);
            _finishLine.OnReached += HandleFinishReached;
        }
    }

    private void Update()
    {
        if (_inputLocked)
            return;

        UpdatePlayerPosition();
    }

    private void LateUpdate()
    {
        if (_inputLocked || _player == null)
            return;

        _finishLine?.CheckForCrossing(_player);
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.OnDeath -= HandlePlayerDeath;

        if (_finishLine != null)
            _finishLine.OnReached -= HandleFinishReached;
    }

    private void LoadPlayerConfig()
    {
        PlayerConfig[] configs =
            Resources.LoadAll<PlayerConfig>("");

        if (configs.Length == 0)
        {
            Debug.LogError("No PlayerConfig found in Resources.");
            return;
        }

        currentPlayerConfig = configs[0];
    }

    private bool TryFindObstacle(
        float radius,
        float maxDistance,
        out RaycastHit hit)
    {
        hit = default;

        if (_player == null || maxDistance <= 0f)
            return false;

        return Physics.SphereCast(
            _player.transform.position,
            Mathf.Max(0.001f, radius),
            _player.transform.forward,
            out hit,
            maxDistance,
            _obstacleLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private float GetFireDetectionDistance()
    {
        if (_player == null)
            return 0f;

        float distance = _maxDetectionDistance;

        if (_finishLine != null)
        {
            if (!_finishLine.TryGetForwardDistance(
                _player.transform.position,
                _player.transform.forward,
                out float finishDistance))
                return 0f;

            distance = Mathf.Min(distance, finishDistance);
        }

        return distance;
    }

    private void UpdatePlayerPosition()
    {
        if (_player == null ||
            _player.IsMoving ||
            _player.IsShrinking ||
            _player.IsDead)
            return;

        // When a finish exists, check the entire path to it.
        if (_finishLine != null)
        {
            if (!_finishLine.TryGetForwardDistance(
                _player.transform.position,
                _player.transform.forward,
                out float distanceToFinish))
                return;

            if (TryFindObstacle(
                _player.Radius,
                distanceToFinish,
                out RaycastHit hit))
            {
                MoveToObstacleGap(hit.distance);
            }
            else
            {
                // No obstacles before the finish:
                // move through the line.
                _player.MoveForDistance(
                    _player.transform.forward,
                    distanceToFinish + _finishOverrunDistance
                );
            }

            return;
        }

        // Preserve previous behavior if no finish is assigned.
        if (TryFindObstacle(
            _player.Radius,
            _maxDetectionDistance,
            out RaycastHit nearestHit))
        {
            MoveToObstacleGap(nearestHit.distance);
        }
    }

    private void MoveToObstacleGap(float obstacleDistance)
    {
        float difference =
            obstacleDistance - _desiredObstacleDistance;

        if (Mathf.Abs(difference) <= DistanceTolerance)
            return;

        Vector3 direction = difference > 0f
            ? _player.transform.forward
            : -_player.transform.forward;

        _player.MoveForDistance(
            direction,
            Mathf.Abs(difference)
        );
    }

    
    public bool TryFireProjectile(float radius)
    {
        if (!isProjectileAllowed)
            return false;

        if (radius < MinProjectileRadius ||
            radius > MaxProjectileRadius)
            return false;

        float maxDistance = GetFireDetectionDistance();

        if (maxDistance <= 0f)
            return false;

        bool hasTarget = TryFindObstacle(
            radius,
            maxDistance,
            out RaycastHit hit
        ) && hit.distance > 0f;

        float travelDistance = hasTarget
            ? hit.distance
            : maxDistance;

        Projectile projectile = Instantiate(
            _projectilePrefab,
            _player.transform.position,
            _player.transform.rotation
        );

        _spawnedProjectiles.RemoveAll(p => p == null);
        _spawnedProjectiles.Add(projectile);

        projectile.Launch(
            radius,
            travelDistance,
            hasTarget ? hit.collider : null
        );

        if (!_player.TryConsumeProjectileVolume(radius))
        {
            _spawnedProjectiles.Remove(projectile);
            Destroy(projectile.gameObject);
            return false;
        }

        return true;
    }

    public void SetInputLocked(bool locked)
    {
        _inputLocked = locked;

        if (locked && _player != null)
            _player.Stop();
    }

    public void ClearProjectiles()
    {
        foreach (Projectile projectile in _spawnedProjectiles)
        {
            if (projectile == null)
                continue;

            projectile.gameObject.SetActive(false);
            Destroy(projectile.gameObject);
        }

        _spawnedProjectiles.Clear();
    }

    public void ResetPlayerToStart()
    {
        if (_player == null || currentPlayerConfig == null)
            return;

        _player.Stop();

        _player.transform.SetPositionAndRotation(
            _startPosition,
            _startRotation
        );

        _player.Initialize(currentPlayerConfig.InitialPlayerRadius);

        if (_finishLine != null)
            _finishLine.ResetFor(_player);
    }

    private void HandleFinishReached()
    {
        OnLevelFinished?.Invoke();
    }

    private void HandlePlayerDeath()
    {
        OnPlayerDied?.Invoke();
    }
}
