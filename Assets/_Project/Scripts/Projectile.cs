using UnityEngine;

[RequireComponent(typeof(ObstacleArcDetector))]
public class Projectile : SphereEntity
{
    private ObstacleArcDetector _arcDetector;
    private Collider _targetCollider;
    private Renderer[] _renderers;

    private void Awake()
    {
        _arcDetector = GetComponent<ObstacleArcDetector>();
        _arcDetector.OnPropagationCompleted += HandlePropagationCompleted;

        _renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnDestroy()
    {
        if (_arcDetector != null)
            _arcDetector.OnPropagationCompleted -= HandlePropagationCompleted;
    }

    public void Launch(
        float radius,
        float travelDistance,
        Collider targetCollider = null)
    {
        _targetCollider = targetCollider;

        SetRadius(radius);
        StartMovement(transform.forward, travelDistance);
    }

    protected override void OnMovementCompleted()
    {
        if (_targetCollider == null)
        {
            Destroy(gameObject);
            return;
        }

        HideVisual();

        _arcDetector.ApplyImpact(
            transform.position,
            Radius,
            transform.forward,
            _targetCollider
        );
    }

    private void HideVisual()
    {
        foreach (Renderer renderer in _renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
    }

    private void HandlePropagationCompleted()
    {
        Destroy(gameObject);
    }
}