using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Transform cameraTransform;
    private Vector2 _movement = Vector2.zero;
    
    const int MaxInputsPerSecond = 60;
    private uint _inputCount = 0;

    void Start()
    {
        if(cameraTransform == default) cameraTransform = Camera.main?.transform ?? transform;
        InvokeRepeating(nameof(ClearInputs), 1, 1);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(ClearInputs));
    }

    private void ClearInputs()
    {
        _inputCount = 0;
    }

    private void OnMove(InputValue value)
    {
        var movementVector = value.Get<Vector2>();
        var convertedVector3 =  new Vector3(movementVector.x, 0, movementVector.y);
        var transformedMovement = cameraTransform.TransformDirection(convertedVector3);
        transformedMovement.y = 0;
        transformedMovement = transformedMovement.normalized;
        var newMovement = new Vector2(transformedMovement.x, transformedMovement.z);
        if(newMovement.ApproximatesTo(_movement)) return;
        if(_inputCount > MaxInputsPerSecond) return;
        _movement = newMovement;
        var direction = new DbVector2(_movement.x, _movement.y);
        GameManager.Conn.Reducers.UpdatePlayerInput(direction);
        _inputCount++;
    }
}
