
using System;
using System.Collections;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [Header("Finish Settings")]
    [SerializeField, Min(0.1f)]
    private float _width = 10f;
    
    [Header("Door Settings")]
    [SerializeField] private Animator _doorAnimator;

    [SerializeField, Min(0.1f)]
    private float _proximityDistance = 5f;

    [SerializeField, Min(0.01f)]
    private float _checkInterval = 0.5f;

    private static readonly int IsOpen =
        Animator.StringToHash("IsOpen");

    private bool _completed;
    private bool _wasBeforeLine;
    private bool _isPlayerNear;
    
    private Player _player;

    private Coroutine _distanceCheckCoroutine;

    public event Action OnReached;

    public void ResetFor(Player player)
    {
        if (_distanceCheckCoroutine != null)
        {
            StopCoroutine(_distanceCheckCoroutine);
            _distanceCheckCoroutine = null;
        }

        _player = player;

        _completed = false;
        _isPlayerNear = false;

        _wasBeforeLine = player != null &&
            Vector3.Dot(
                player.transform.position - transform.position,
                transform.forward
            ) < 0f;

        if (_doorAnimator != null)
            _doorAnimator.SetBool(IsOpen, false);

        if (isActiveAndEnabled && _player != null)
        {
            _distanceCheckCoroutine =
                StartCoroutine(CheckPlayerDistance());
        }
    }

    public bool TryGetForwardDistance(
        Vector3 origin,
        Vector3 forward,
        out float distance)
    {
        distance = 0f;

        float denominator = Vector3.Dot(
            forward.normalized,
            transform.forward
        );

        if (denominator <= 0.0001f)
            return false;

        distance = Vector3.Dot(
            transform.position - origin,
            transform.forward
        ) / denominator;

        return distance > 0f;
    }

    public void CheckForCrossing(Player player)
    {
        if (_completed || player == null || player.IsDead)
            return;

        Vector3 offset =
            player.transform.position - transform.position;

        float signedDistance = Vector3.Dot(
            offset,
            transform.forward
        );

        bool isBeforeLine = signedDistance < 0f;

        float lateralDistance = Mathf.Abs(
            Vector3.Dot(offset, transform.right)
        );

        bool withinWidth = lateralDistance <=
            _width * 0.5f + player.Radius;

        if (_wasBeforeLine && !isBeforeLine && withinWidth)
        {
            _completed = true;

            if (_distanceCheckCoroutine != null)
            {
                StopCoroutine(_distanceCheckCoroutine);
                _distanceCheckCoroutine = null;
            }

            OnReached?.Invoke();
        }

        _wasBeforeLine = isBeforeLine;
    }

    private IEnumerator CheckPlayerDistance()
    {
        WaitForSeconds delay =
            new WaitForSeconds(_checkInterval);

        while (!_isPlayerNear && !_completed)
        {
            if (_player != null && !_player.IsDead)
            {
                Vector3 offset =
                    _player.transform.position - transform.position;

                float forwardDistance = -Vector3.Dot(
                    offset,
                    transform.forward
                );

                float lateralDistance = Mathf.Abs(
                    Vector3.Dot(offset, transform.right)
                );

                bool withinWidth = lateralDistance <=
                    _width * 0.5f + _player.Radius;

                if (forwardDistance >= 0f &&
                    forwardDistance <= _proximityDistance &&
                    withinWidth)
                {
                    _isPlayerNear = true;
                    OpenDoor();
                    break;
                }
            }

            yield return delay;
        }

        _distanceCheckCoroutine = null;
    }

    private void OpenDoor()
    {
        if (_doorAnimator != null)
            _doorAnimator.SetBool(IsOpen, true);
    }

    private void OnDisable()
    {
        if (_distanceCheckCoroutine != null)
        {
            StopCoroutine(_distanceCheckCoroutine);
            _distanceCheckCoroutine = null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 halfWidth =
            transform.right * (_width * 0.5f);

        Gizmos.DrawLine(
            transform.position - halfWidth,
            transform.position + halfWidth
        );

        Gizmos.DrawRay(
            transform.position,
            transform.forward * 2f
        );

        // Visualize the proximity detection zone.
        Gizmos.color = Color.yellow;

        Vector3 center =
            transform.position -
            transform.forward * (_proximityDistance * 0.5f);

        Vector3 size = new Vector3(
            _width,
            0.1f,
            _proximityDistance
        );

        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            center,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, size);

        Gizmos.matrix = previousMatrix;
    }
}
