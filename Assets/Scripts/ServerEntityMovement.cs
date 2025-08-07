using JetBrains.Annotations;
using SpacetimeDB.Types;
using UnityEngine;

[RequireComponent(typeof(EntityInterpolation))]
[RequireComponent(typeof(EntityAnimator))]
public class ServerEntityMovement : MonoBehaviour
{
    public EntityInterpolation entityInterpolation;
    public EntityAnimator entityAnimator;
    private float _yPos;

    public void Init(Entity entity)
    {
        _yPos = transform.position.y;
        transform.position = entity.Position.ToGamePosition(_yPos);
    }

    private void Start()
    {
        if(!entityInterpolation) entityInterpolation = GetComponent<EntityInterpolation>();
        if(!entityAnimator) entityAnimator = GetComponent<EntityAnimator>();
        entityInterpolation.lerpDuration = GameManager.Config.UpdateEntityInterval * 1.5f;
    }
    
    [UsedImplicitly]
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        var position = newServerEntityState.Position.ToGamePosition(_yPos);
        var direction = newServerEntityState.Direction.ToGamePosition(_yPos);
        entityInterpolation.SetCanonicalPosition(position);
        entityInterpolation.SetMovementDirection(direction);
        entityAnimator.SetDirection(direction);
    }
}