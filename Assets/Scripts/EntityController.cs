using JetBrains.Annotations;
using SpacetimeDB.Types;
using Unity.VisualScripting;
using UnityEngine;

public class EntityController : MonoBehaviour
{
    public uint entityId;

    public void Spawn(uint spawnedEntityId)
    {
        entityId = spawnedEntityId;
    }

    public void OnDelete(EventContext context)
    {
        Destroy(gameObject);
    }
}