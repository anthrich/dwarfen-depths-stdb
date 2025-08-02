using JetBrains.Annotations;
using SpacetimeDB.Types;
using UnityEngine;

[RequireComponent(typeof(EntityInterpolation))]
public class ServerEntityMovement : MonoBehaviour
{
    public EntityInterpolation entityInterpolation;
    private float _yPos;

    public void Init(Entity entity)
    {
        _yPos = transform.position.y;
        transform.position = entity.Position.ToGamePosition(_yPos);
    }

    private void Start()
    {
        if(!entityInterpolation) entityInterpolation = GetComponent<EntityInterpolation>();
    }
    
    [UsedImplicitly]
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        entityInterpolation.SetCanonicalPosition(
            newServerEntityState.Position.ToGamePosition(_yPos)
        );
    }
}