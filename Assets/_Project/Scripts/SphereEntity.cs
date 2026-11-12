using UnityEngine;

public abstract class SphereEntity : MonoBehaviour
{
    [SerializeField] private Transform _ballTransform;
    [SerializeField, Min(0.01f)] private float _movementSpeed = 5f;

    protected float CurrentRadius { get; private set; }

    public float Radius => CurrentRadius;
    public bool IsMoving => _isMoving;

    private Vector3 _movementDirection;
    private float _remainingDistance;
    private bool _isMoving;

    protected virtual void Update()
    {
        if (!_isMoving)
            return;

        float step = _movementSpeed * Time.deltaTime;

        if (!float.IsPositiveInfinity(_remainingDistance))
            step = Mathf.Min(step, _remainingDistance);

        transform.position += _movementDirection * step;

        if (float.IsPositiveInfinity(_remainingDistance))
            return;

        _remainingDistance -= step;

        if (_remainingDistance <= 0f)
        {
            StopMovement();
            OnMovementCompleted();
        }
    }

    protected void SetRadius(float radius)
    {
        CurrentRadius = Mathf.Max(0f, radius);
        UpdateSphereScale();
    }

    protected void StartMovement(Vector3 direction, float distance)
    {
        if (direction.sqrMagnitude == 0f || distance <= 0f)
        {
            StopMovement();
            return;
        }

        _movementDirection = direction.normalized;
        _remainingDistance = distance;
        _isMoving = true;
    }

    protected void StopMovement()
    {
        _isMoving = false;
        _movementDirection = Vector3.zero;
        _remainingDistance = 0f;
    }

    protected virtual void OnMovementCompleted() { }

    private void UpdateSphereScale()
    {
        if (_ballTransform == null)
            return;

        _ballTransform.localScale =
            Vector3.one * (CurrentRadius * 2f);
    }
}