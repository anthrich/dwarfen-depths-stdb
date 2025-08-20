using JetBrains.Annotations;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class CameraMovement : MonoBehaviour
{
    public bool lookEnabled;
    public bool freeLookEnabled;
    public float lookSensitivity = 0.2f;
    private Vector2 _lastMousePosition;
    private PlayerInput _playerInput;
    private CinemachineCamera _camera;
    private InputAction _enableLookAction;
    private InputAction _enableFreeLookAction;
    private CinemachineOrbitalFollow _orbitalFollow;
    
    public void Init(CinemachineCamera cinemachineCamera, PlayerInput playerInput)
    {
        _playerInput = playerInput;
        _camera = cinemachineCamera;
        _orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        _enableLookAction = _playerInput.actions.FindAction("EnableLook");
        _enableLookAction.performed += LookEnabled;
        _enableLookAction.canceled += LookDisabled;
        _enableFreeLookAction = _playerInput.actions.FindAction("EnableFreeLook");
        _enableFreeLookAction.performed += FreeLookEnabled;
        _enableFreeLookAction.canceled += FreeLookDisabled;
    }

    private void OnDestroy()
    {
        _enableLookAction.performed -= LookEnabled;
        _enableLookAction.canceled -= LookDisabled;
        _enableFreeLookAction.performed -= FreeLookEnabled;
        _enableFreeLookAction.canceled -= FreeLookDisabled;
    }

    private void LookEnabled(InputAction.CallbackContext obj)
    {
        lookEnabled = true;
        EnableLook();
    }
    
    private void LookDisabled(InputAction.CallbackContext obj)
    {
        if(!lookEnabled) return;
        lookEnabled = false;
        DisableLook();
    }
    
    private void FreeLookEnabled(InputAction.CallbackContext obj)
    {
        freeLookEnabled = true;
        EnableLook();
    }
    
    private void FreeLookDisabled(InputAction.CallbackContext obj)
    {
        if(!freeLookEnabled) return;
        freeLookEnabled = false;
        DisableLook();
    }

    private void EnableLook()
    {
        _lastMousePosition = Mouse.current.position.ReadValue();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void DisableLook()
    {
        Cursor.lockState = CursorLockMode.None;
        Mouse.current.WarpCursorPosition(_lastMousePosition);
        Cursor.visible = true;
    }

    [UsedImplicitly]
    private void OnLook(InputValue value)
    {
        if(!lookEnabled && !freeLookEnabled) return;
        var inputVector2 = value.Get<Vector2>();
        _orbitalFollow.HorizontalAxis.Value += inputVector2.x * lookSensitivity;
        _orbitalFollow.VerticalAxis.Value -= inputVector2.y * lookSensitivity;
        _orbitalFollow.VerticalAxis.Value = Mathf.Clamp(_orbitalFollow.VerticalAxis.Value, -10, 80);
        if(!freeLookEnabled) SendMessage("OnLookApplied", SendMessageOptions.DontRequireReceiver);
    }
    
    [UsedImplicitly]
    private void OnZoom(InputValue value)
    {
        var inputVector2 = value.Get<Vector2>();
        _orbitalFollow.Radius -= inputVector2.y;
        _orbitalFollow.Radius = Mathf.Clamp(_orbitalFollow.Radius, 3, 15);
    }
}
