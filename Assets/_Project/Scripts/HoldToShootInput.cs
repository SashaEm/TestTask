
using UnityEngine;
using UnityEngine.InputSystem;

public class HoldToShootInput : MonoBehaviour
{
    [SerializeField] private PlayerLoopManager _loopManager;
    [SerializeField] private Transform _chargePreview;
    [SerializeField] private float _previewSpacing = 0.15f;

    private InputAction _pressAction;

    private bool _isCharging;
    private float _holdDuration;
    private float _chargeRadius;

    private void Awake()
    {
        _pressAction = new InputAction(
            "ChargeProjectile",
            InputActionType.Button,
            "<Pointer>/press"
        );

        _pressAction.performed += OnPressed;
        _pressAction.canceled += OnReleased;
    }

    private void OnEnable()
    {
        HidePreview();
        _pressAction.Enable();
    }

    private void OnDisable()
    {
        CancelCharge();
        _pressAction.Disable();
    }

    private void OnDestroy()
    {
        _pressAction.performed -= OnPressed;
        _pressAction.canceled -= OnReleased;
        _pressAction.Dispose();
    }

    private void Update()
    {
        if (!_isCharging)
            return;

        if (_loopManager == null ||
            !_loopManager.isProjectileAllowed)
        {
            CancelCharge();
            return;
        }

        _holdDuration += Time.deltaTime;

        _chargeRadius = Mathf.Min(
            _loopManager.MaxProjectileRadius,
            _loopManager.MinProjectileRadius +
            _holdDuration * _loopManager.ChargeGrowthPerSecond
        );

        UpdateChargePreview();
    }

    private void OnPressed(InputAction.CallbackContext context)
    {
        if (_isCharging ||
            _loopManager == null ||
            !_loopManager.isProjectileAllowed)
            return;

        _isCharging = true;
        _holdDuration = 0f;
        _chargeRadius = _loopManager.MinProjectileRadius;

        UpdateChargePreview();
    }

    private void OnReleased(InputAction.CallbackContext context)
    {
        if (!_isCharging)
            return;

        _isCharging = false;
        HidePreview();

        _loopManager.TryFireProjectile(_chargeRadius);
    }

    private void CancelCharge()
    {
        _isCharging = false;
        _holdDuration = 0f;
        HidePreview();
    }

    private void UpdateChargePreview()
    {
        if (_chargePreview == null)
            return;

        _chargePreview.gameObject.SetActive(true);

        // Visual indicator above the player, not the spawn point.
        _chargePreview.position =
            _loopManager.PlayerPosition;

        _chargePreview.localScale =
            Vector3.one * (_chargeRadius * 2f);
    }

    private void HidePreview()
    {
        if (_chargePreview != null)
            _chargePreview.gameObject.SetActive(false);
    }
}
