using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using Entity = SpacetimeDB.Types.Entity;
using Vector2 = UnityEngine.Vector2;

[RequireComponent(typeof(EntityInterpolation))]
[RequireComponent(typeof(EntityAnimator))]
public class PlayerMovement :
    MonoBehaviour,
    ISubscriber<SharedPhysics.Entity>
{
    public Transform cameraTransform;
    public EntityInterpolation entityInterpolation;
    public EntityAnimator entityAnimator;
    public Transform serverStateObject;
    
    private Vector2 _movementInput = Vector2.zero;
    private Vector2 _movement = Vector2.zero;
    private float _yPosition;

    void Start()
    {
        if(cameraTransform == default) cameraTransform = Camera.main?.transform ?? transform;
        if(entityInterpolation == default) entityInterpolation = GetComponent<EntityInterpolation>();
        if(!entityAnimator) entityAnimator = GetComponent<EntityAnimator>();
        if (serverStateObject == default) serverStateObject = transform.GetChild(0);
        entityInterpolation.SetCanonicalPosition(transform.position);
        _yPosition = transform.position.y;
    }

    public void OnEntitySpawned(Entity newServerEntityState)
    {
        Debug.Log($"Entity spawned: {newServerEntityState}");
    }
    
    [UsedImplicitly]
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        serverStateObject.transform.position = newServerEntityState.Position.ToGamePosition(_yPosition);
    }

    [UsedImplicitly]
    private void OnMove(InputValue value)
    {
        var newInput = value.Get<Vector2>();
        if(newInput.ApproximatesTo(_movementInput)) return;
        _movementInput = newInput;
        _movement = TransformMovementWithCamera();
        Simulation.Instance.SetInputState(new InputState { Direction = _movement });
    }

    [UsedImplicitly]
    private void OnLookApplied()
    {
        _movement = TransformMovementWithCamera();
        Simulation.Instance.SetInputState(new InputState { Direction = _movement });
    }

    private Vector2 TransformMovementWithCamera()
    {
        var transformedMovement = cameraTransform.TransformDirection(_movementInput.ToGamePosition(_yPosition));
        transformedMovement.y = 0;
        transformedMovement = transformedMovement.normalized;
        return new Vector2(transformedMovement.x, transformedMovement.z);
    }

    private void Update()
    {
        entityInterpolation.SetMovementDirection(_movement.ToGamePosition(_yPosition));
        entityAnimator.SetDirection(_movement);
    }

    public void SubscriptionUpdate(SharedPhysics.Entity update)
    {
        entityInterpolation?.SetCanonicalPosition(update.Position.ToGamePosition(_yPosition));
    }
}