using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Transform cameraTransform;
    private Vector2 _movement = Vector2.zero;
    
    void Start()
    {
        if(cameraTransform == default) cameraTransform = Camera.main?.transform ?? transform;
    }

    private void OnMove(InputValue value)
    {
        var movementVector = value.Get<Vector2>();
        var newMovement = ApplyCameraHeading(movementVector);
        if(newMovement.ApproximatesTo(_movement)) return;
        _movement = newMovement;
        var direction = new DbVector2(_movement.x, _movement.y);
        GameManager.Conn.Reducers.UpdatePlayerInput(direction);
    }

    private Vector2 ApplyCameraHeading(Vector2 movementVector)
    {
        var convertedVector3 =  new Vector3(movementVector.x, 0, movementVector.y);
        var transformedMovement = cameraTransform.TransformDirection(convertedVector3);
        transformedMovement.y = 0;
        transformedMovement = transformedMovement.normalized;
        var newMovement = new Vector2(transformedMovement.x, transformedMovement.z);
        return newMovement;
    }
}
