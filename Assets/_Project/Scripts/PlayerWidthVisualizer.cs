using UnityEngine;

public class PlayerWidthVisualizer : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private Player _player;

    [Header("Line Settings")] [SerializeField, Min(1f)]
    private float _length = 100f;

    [SerializeField, Min(0.001f)] private float _thickness = 0.02f;
    [SerializeField] private float _groundHeight = 0f;
    [SerializeField] private float _surfaceOffset = 0.01f;

    private void LateUpdate()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<Player>();

            if (_player == null)
                return;
        }

        UpdateVisualizer();
    }

    private void UpdateVisualizer()
    {
        Vector3 position = _player.transform.position;

        position.y = _groundHeight + _surfaceOffset;

        transform.position = position;

        transform.localScale = new Vector3(
            1,
            _thickness,
            _length
        );
    }
}