using SpacetimeDB.Types;
using UnityEngine;
using PlayerInput = UnityEngine.InputSystem.PlayerInput;

public class PrefabManager : MonoBehaviour
{
    private static PrefabManager _instance;
    public LatencyChart latencyChart;
    public PlayerController playerPrefab;
    public EntityController entityPrefab;
    public MeshFilter floorMeshPrefab;
    public GameObject mapContainer;

    private void Awake()
    {
        _instance = this;
    }

    public static void SpawnMapTile(MapTile tile)
    {
        MeshFilter meshFilter = Instantiate(_instance.floorMeshPrefab, _instance.mapContainer.transform);
        Bounds bounds = meshFilter.mesh.bounds;
        float originalWidth = bounds.size.x;
        float originalLength = bounds.size.z;

        Vector3 scale = new Vector3(tile.Width / originalWidth, 1f, tile.Height / originalLength);
        meshFilter.transform.localScale = scale;
        meshFilter.transform.position = tile.Position.ToGamePosition(0);
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
            var playerInput = entityController.GetComponent<PlayerInput>();
            playerInput.enabled = true;
            var playerMovement = entityController.gameObject.AddComponent<PlayerMovement>();
            var cameraMovement = entityController.gameObject.AddComponent<CameraMovement>();
            cameraMovement.Init(GameManager.Instance.cinemachineCamera, playerInput);
            playerMovement.OnEntitySpawned(entity);
            playerMovement.Subscribe(_instance.latencyChart);
            entityController.GetComponent<PlayerInput>().enabled = true;
        }
        else
        {
            var serverEntityMovement = entityController.gameObject.AddComponent<ServerEntityMovement>();
            serverEntityMovement.Init(entity);
        }

        return entityController;
    }
}