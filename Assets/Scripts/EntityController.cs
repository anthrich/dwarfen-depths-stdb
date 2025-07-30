using JetBrains.Annotations;
using SpacetimeDB.Types;
using Unity.VisualScripting;
using UnityEngine;

public class EntityController : MonoBehaviour
{

    [DoNotSerialize]
    public uint entityId;

    public void Spawn(uint spawnedEntityId)
    {
        entityId = spawnedEntityId;
        var entity = GameManager.Conn.Db.Entity.EntityId.Find(spawnedEntityId);
        var pos = entity?.Position.ToUnityVector2() ?? Vector2.zero;
        transform.position = new Vector3(pos.x, transform.position.y, pos.y);
    }

    /*[UsedImplicitly]
    public void OnEntityUpdated(Entity newVal)
    {
        var newVector2 = newVal.Position.ToUnityVector2();
        transform.position = new Vector3(newVector2.x, transform.position.y, newVector2.y);
    }*/

    public void OnDelete(EventContext context)
    {
        Destroy(gameObject);
    }
}