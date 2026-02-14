using SpacetimeDB.Types;
using UnityEngine;
using PlayerInput = UnityEngine.InputSystem.PlayerInput;

public class PrefabManager : MonoBehaviour
{
    private static PrefabManager _instance;
    public LatencyChart latencyChart;
    public PlayerController playerPrefab;
    public EntityController entityPrefab;
    public Material targetCircleMaterial;

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

    public static EntityController SpawnPlayerEntity(Entity entity)
    {
        var entityController = Instantiate(_instance.entityPrefab);
        entityController.name = $"Entity:{entity.EntityId}";
        entityController.Spawn(entity.EntityId);
        var playerInput = entityController.GetComponent<PlayerInput>();
        playerInput.enabled = true;
        var playerMovement = entityController.gameObject.AddComponent<PlayerMovement>();
        var cameraMovement = entityController.gameObject.AddComponent<CameraMovement>();
        var playerTargetting = entityController.gameObject.AddComponent<PlayerTargetting>();
        playerTargetting.circleMaterial = _instance.targetCircleMaterial;
        cameraMovement.Init(GameManager.Instance.cinemachineCamera, playerInput);
        Simulation.Instance.Subscribe(playerMovement);
        Simulation.Instance.Subscribe(_instance.latencyChart);
        playerMovement.OnEntitySpawned(entity);
        entityController.GetComponent<PlayerInput>().enabled = true;
        return entityController;
    }

    public static EntityController SpawnEntity(Entity entity)
    {
        var entityController = Instantiate(_instance.entityPrefab);
        entityController.name = $"Entity:{entity.EntityId}";
        entityController.Spawn(entity.EntityId);
        var entityRotationInterpolation = entityController.gameObject.AddComponent<EntityRotationInterpolation>();
        entityRotationInterpolation.Init(GameManager.Config.UpdateEntityInterval);
        var serverEntityMovement = entityController.gameObject.AddComponent<ServerEntityMovement>();
        serverEntityMovement.Init(entity);
        return entityController;
    }
}
