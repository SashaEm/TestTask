
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleArcDetector : MonoBehaviour
{
    [Header("Arc Settings")]
    [SerializeField, Range(1f, 360f)]
    private float _arcAngle = 90f;

    [SerializeField, Min(0.01f)]
    private float _reachMultiplier = 4f;

    [SerializeField]
    private LayerMask _obstacleLayers;

    [Header("Impact Propagation")]
    [SerializeField, Min(0f)]
    private float _delayBetweenObstacles = 0.08f;

    private readonly List<Obstacle> _orderedObstacles = new();

    public IReadOnlyList<Obstacle> OrderedObstacles => _orderedObstacles;

    private Coroutine _propagationCoroutine;
    public event Action OnPropagationCompleted;

    private Vector3 _lastImpactPosition;
    private Vector3 _lastDirection;
    private float _lastReach;
    private bool _hasImpact;

    public IReadOnlyList<Obstacle> FindObstaclesInArc(
        Vector3 impactPosition,
        float projectileRadius,
        Vector3 forward,
        Collider directlyHit)
    {
        _orderedObstacles.Clear();

        Dictionary<Obstacle, float> distances = new();

        Obstacle hitObstacle = directlyHit != null
            ? directlyHit.GetComponentInParent<Obstacle>()
            : null;

        if (hitObstacle != null)
            distances[hitObstacle] = 0f;

        float reach = projectileRadius * _reachMultiplier;

        Vector3 direction = Vector3.ProjectOnPlane(
            forward,
            Vector3.up
        ).normalized;

        if (reach > 0f && direction.sqrMagnitude > 0f)
        {
            Collider[] colliders = Physics.OverlapSphere(
                impactPosition,
                reach,
                _obstacleLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider collider in colliders)
            {
                Obstacle obstacle =
                    collider.GetComponentInParent<Obstacle>();

                if (obstacle == null || obstacle == hitObstacle)
                    continue;

                Vector3 closestPoint =
                    collider.ClosestPoint(impactPosition);

                Vector3 offset = closestPoint - impactPosition;

                Vector3 toObstacle = Vector3.ProjectOnPlane(
                    offset,
                    Vector3.up
                );

                if (toObstacle.sqrMagnitude > 0.0001f)
                {
                    float angle = Vector3.Angle(
                        direction,
                        toObstacle
                    );

                    if (angle > _arcAngle * 0.5f)
                        continue;
                }

                float distance = offset.magnitude;

                // An obstacle may have multiple colliders.
                // Keep only its nearest valid distance.
                if (!distances.TryGetValue(
                        obstacle, out float existingDistance) ||
                    distance < existingDistance)
                {
                    distances[obstacle] = distance;
                }
            }
        }

        List<KeyValuePair<Obstacle, float>> sorted = new(distances);

        sorted.Sort((a, b) =>
        {
            // The directly hit obstacle is always first.
            if (a.Key == hitObstacle) return -1;
            if (b.Key == hitObstacle) return 1;

            int comparison = a.Value.CompareTo(b.Value);

            return comparison != 0
                ? comparison
                : a.Key.GetInstanceID().CompareTo(
                    b.Key.GetInstanceID()
                );
        });

        foreach (var entry in sorted)
            _orderedObstacles.Add(entry.Key);

        return _orderedObstacles;
    }

    public void ApplyImpact(
        Vector3 impactPosition,
        float projectileRadius,
        Vector3 forward,
        Collider directlyHit)
    {
        if (_propagationCoroutine != null)
        {
            StopCoroutine(_propagationCoroutine);
            _propagationCoroutine = null;
        }

        _lastImpactPosition = impactPosition;
        _lastDirection = Vector3.ProjectOnPlane(
            forward,
            Vector3.up
        ).normalized;

        _lastReach = projectileRadius * _reachMultiplier;
        _hasImpact = true;

        FindObstaclesInArc(
            impactPosition,
            projectileRadius,
            forward,
            directlyHit
        );

        if (_orderedObstacles.Count > 0)
        {
            _propagationCoroutine =
                StartCoroutine(PropagateImpact());
        }
        else
        {
            OnPropagationCompleted?.Invoke();
        }
    }

    
    private IEnumerator PropagateImpact()
    {
        for (int i = 0; i < _orderedObstacles.Count; i++)
        {
            Obstacle obstacle = _orderedObstacles[i];

            if (obstacle != null)
                obstacle.Activate();

            if (i < _orderedObstacles.Count - 1 &&
                _delayBetweenObstacles > 0f)
            {
                yield return new WaitForSeconds(
                    _delayBetweenObstacles
                );
            }
        }

        _propagationCoroutine = null;

        OnPropagationCompleted?.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (!_hasImpact || _lastDirection.sqrMagnitude == 0f)
            return;

        Gizmos.color = Color.cyan;

        const int segments = 32;

        Vector3 previous = Quaternion.AngleAxis(
            -_arcAngle * 0.5f,
            Vector3.up
        ) * _lastDirection;

        Gizmos.DrawLine(
            _lastImpactPosition,
            _lastImpactPosition + previous * _lastReach
        );

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                -_arcAngle * 0.5f +
                _arcAngle * i / segments;

            Vector3 next = Quaternion.AngleAxis(
                angle,
                Vector3.up
            ) * _lastDirection;

            Gizmos.DrawLine(
                _lastImpactPosition + previous * _lastReach,
                _lastImpactPosition + next * _lastReach
            );

            previous = next;
        }

        Gizmos.DrawLine(
            _lastImpactPosition,
            _lastImpactPosition + previous * _lastReach
        );
    }
}
