using JetBrains.Annotations;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class CameraMovement : MonoBehaviour
{
    public bool lookEnabled;
    public float lookSensitivity = 0.2f;
    private PlayerInput _playerInput;
    private CinemachineCamera _camera;
    private InputAction _enableLookAction;
    private CinemachineOrbitalFollow _orbitalFollow;
    
    public void Init(CinemachineCamera cinemachineCamera, PlayerInput playerInput)
    {
        _playerInput = playerInput;
        _camera = cinemachineCamera;
        _orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        _enableLookAction = _playerInput.actions.FindAction("EnableLook");
        _enableLookAction.performed += LookEnabled;
        _enableLookAction.canceled += LookDisabled;
    }

    private void OnDestroy()
    {
        _enableLookAction.performed -= LookEnabled;
        _enableLookAction.canceled -= LookDisabled;
    }

    private void LookEnabled(InputAction.CallbackContext obj)
    {
        lookEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void LookDisabled(InputAction.CallbackContext obj)
    {
        lookEnabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    [UsedImplicitly]
    private void OnLook(InputValue value)
    {
        if(!lookEnabled) return;
        var inputVector2 = value.Get<Vector2>();
        _orbitalFollow.HorizontalAxis.Value += inputVector2.x * lookSensitivity;
        _orbitalFollow.VerticalAxis.Value -= inputVector2.y * lookSensitivity;
        _orbitalFollow.VerticalAxis.Value = Mathf.Clamp(_orbitalFollow.VerticalAxis.Value, -10, 80);
        SendMessage("OnLookApplied", SendMessageOptions.DontRequireReceiver);
    }
    
    [UsedImplicitly]
    private void OnZoom(InputValue value)
    {
        var inputVector2 = value.Get<Vector2>();
        _orbitalFollow.Radius -= inputVector2.y;
        _orbitalFollow.Radius = Mathf.Clamp(_orbitalFollow.Radius, 3, 15);
    }
}
