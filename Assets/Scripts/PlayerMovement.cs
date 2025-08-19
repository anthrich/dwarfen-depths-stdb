using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Entity = SpacetimeDB.Types.Entity;
using Vector2 = UnityEngine.Vector2;

[RequireComponent(typeof(EntityPositionInterpolation))]
[RequireComponent(typeof(EntityAnimator))]
public class PlayerMovement :
    MonoBehaviour,
    ISubscriber<SharedPhysics.Entity>
{
    public Transform cameraTransform;
    [FormerlySerializedAs("entityInterpolation")] public EntityPositionInterpolation entityPositionInterpolation;
    public EntityAnimator entityAnimator;
    public Transform serverStateObject;
    
    private Vector2 _movementInput = Vector2.zero;
    private Vector2 _movement = Vector2.zero;
    private float _yPosition;

    void Start()
    {
        if(cameraTransform == default) cameraTransform = Camera.main?.transform ?? transform;
        if(entityPositionInterpolation == default) entityPositionInterpolation = GetComponent<EntityPositionInterpolation>();
        if(!entityAnimator) entityAnimator = GetComponent<EntityAnimator>();
        if (serverStateObject == default) serverStateObject = transform.GetChild(0);
        entityPositionInterpolation.SetCanonicalPosition(transform.position);
        _yPosition = transform.position.y;
    }

    public void OnEntitySpawned(Entity newServerEntityState)
    {
        Debug.Log($"Entity spawned: {newServerEntityState}");
    }
    
    [UsedImplicitly]
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        if(!serverStateObject) return;
        serverStateObject.transform.position = newServerEntityState.Position.ToGamePosition(_yPosition);
    }

    [UsedImplicitly]
    private void OnMove(InputValue value)
    {
        var newInput = value.Get<Vector2>();
        _movementInput = newInput;
        UpdateMovement();
    }

    [UsedImplicitly]
    private void OnLookApplied()
    {
        var cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        transform.rotation = Quaternion.LookRotation(cameraForward);
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        var transformedMovement = transform.TransformDirection(_movementInput.ToGamePosition(_yPosition));
        transformedMovement.y = 0;
        transformedMovement = transformedMovement.normalized;
        _movement =  new Vector2(transformedMovement.x, transformedMovement.z);
        Simulation.Instance.SetInputDirection(_movement);
        Simulation.Instance.SetInputRotation(transform.rotation.eulerAngles.y);
    }

    private void Update()
    {
        entityAnimator.SetMovement(_movement, _movementInput);
    }

    public void SubscriptionUpdate(SharedPhysics.Entity update)
    {
        entityPositionInterpolation?.SetCanonicalPosition(update.Position.ToGamePosition(_yPosition));
    }
}