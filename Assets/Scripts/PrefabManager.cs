using SpacetimeDB.Types;
using UnityEngine;
using PlayerInput = UnityEngine.InputSystem.PlayerInput;

public class PrefabManager : MonoBehaviour
{
    private static PrefabManager _instance;
    public PlayerController playerPrefab;
    public EntityController entityPrefab;

    private void Awake()
    {
        _instance = this;
    }

    public static PlayerController SpawnPlayer(Player player)
    {
        var playerController = Instantiate(_instance.playerPrefab);
        playerController.name = $"PlayerController - {player.Name}";
        playerController.Initialize(player);
        return playerController;
    }
    
    public static EntityController SpawnEntity(Entity entity, PlayerController owner)
    {
        var entityController = Instantiate(_instance.entityPrefab);
        entityController.name = $"Entity:{entity.EntityId}";
        entityController.Spawn(entity.EntityId);
        
        if (owner.isLocalPlayer)
        {
            var playerMovement = entityController.gameObject.AddComponent<PlayerMovement>();
            playerMovement.OnEntitySpawned(entity);
            entityController.GetComponent<PlayerInput>().enabled = true;
        }

        return entityController;
    }
}