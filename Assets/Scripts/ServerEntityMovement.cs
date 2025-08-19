using JetBrains.Annotations;
using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(EntityPositionInterpolation))]
[RequireComponent(typeof(EntityAnimator))]
[RequireComponent(typeof(EntityRotationInterpolation))]
public class ServerEntityMovement : MonoBehaviour
{
    public EntityPositionInterpolation entityPositionInterpolation;
    public EntityRotationInterpolation entityRotationInterpolation;
    public EntityAnimator entityAnimator;
    private float _yPos;

    public void Init(Entity entity)
    {
        _yPos = transform.position.y;
        transform.position = entity.Position.ToGamePosition(_yPos);
        transform.rotation = Quaternion.Euler(0, entity.Rotation, 0);
    }

    private void Start()
    {
        if(!entityPositionInterpolation) entityPositionInterpolation = GetComponent<EntityPositionInterpolation>();
        if(!entityRotationInterpolation) entityRotationInterpolation = GetComponent<EntityRotationInterpolation>();
        if(!entityAnimator) entityAnimator = GetComponent<EntityAnimator>();
        entityPositionInterpolation.lerpDuration = GameManager.Config.UpdateEntityInterval * 1.5f;
    }
    
    [UsedImplicitly]
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        var position = newServerEntityState.Position.ToGamePosition(_yPos);
        var direction = newServerEntityState.Direction.ToGamePosition(_yPos);
        entityPositionInterpolation?.SetCanonicalPosition(position);
        entityRotationInterpolation?.SetCanonicalRotation(Quaternion.Euler(0, newServerEntityState.Rotation, 0));
        var relativeDirection = transform.InverseTransformDirection(direction);
        entityAnimator?.SetMovement(direction, new Vector2(relativeDirection.x, relativeDirection.z));
    }
}