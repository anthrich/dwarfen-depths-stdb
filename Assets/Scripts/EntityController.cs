using SpacetimeDB.Types;
using Unity.VisualScripting;
using UnityEngine;

public class EntityController : MonoBehaviour
{
    private const float LerpDurationSec = 0.1f;

    [DoNotSerialize]
    public uint entityId;

    protected float LerpTime;
    protected Vector3 LerpStartPosition;
    protected Vector3 LerpTargetPosition;

    public void Spawn(uint spawnedEntityId)
    {
        entityId = spawnedEntityId;
        var entity = GameManager.Conn.Db.Entity.EntityId.Find(spawnedEntityId);
        var pos = entity?.Position.ToUnityVector2() ?? Vector2.zero;
        LerpStartPosition = LerpTargetPosition = transform.position = new Vector3(pos.x, transform.position.y, pos.y);
    }

    public void OnEntityUpdated(Entity newVal)
    {
        LerpTime = 0.0f;
        LerpStartPosition = transform.position;
        var newVector2 = newVal.Position.ToUnityVector2();
        LerpTargetPosition = new Vector3(newVector2.x, transform.position.y, newVector2.y);
    }

    public void OnDelete(EventContext context)
    {
        Destroy(gameObject);
    }

    public void Update()
    {
        LerpTime = Mathf.Min(LerpTime + Time.deltaTime, LerpDurationSec);
        transform.position = Vector3.Lerp(LerpStartPosition, LerpTargetPosition, LerpTime / LerpDurationSec);
    }
}