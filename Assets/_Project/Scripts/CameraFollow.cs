using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform _target;
    private Vector3 _offset;

    private void LateUpdate()
    {
        if (_target == null)
        {
            FindPlayer();
            return;
        }

        transform.position = _target.position + _offset;
    }

    private void FindPlayer()
    {
        Player player = FindFirstObjectByType<Player>();

        if (player == null)
            return;

        _target = player.transform;
        _offset = transform.position - _target.position;
    }
}
