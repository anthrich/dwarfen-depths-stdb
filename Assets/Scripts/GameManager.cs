using System;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using Unity.Cinemachine;
using UnityEngine;
using Input = SpacetimeDB.Types.Input;
using Vector2 = SharedPhysics.Vector2;

[RequireComponent(typeof(NetworkTime))]
public class GameManager : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;
    public NetworkTime networkTime;
    
    public static readonly Dictionary<string, string> ServerChoices = new()
    {
        {"Local", "http://127.0.0.1:3000"},
        {"Maincloud", "https://maincloud.spacetimedb.com"}
    };
    
    const string ModuleName = "dwarfen-depths";

    public static event Action OnConnected;
    public static event Action OnSubscriptionApplied;

	public static GameManager Instance { get; private set; }
    public static Identity LocalIdentity { get; private set; }
    public static PlayerController LocalPlayer { get; private set; }
    public static Config Config { get; private set; }
    public static DbConnection Conn { get; private set; }

    private static readonly Dictionary<uint, EntityController> Entities = new();

    private void Start()
    {
        Instance = this;
        PlayerPrefs.DeleteAll();
        if (!networkTime) networkTime = GetComponent<NetworkTime>();
    }

    public void Connect(string server)
    {
        var builder = DbConnection.Builder()
            .OnConnect(HandleConnect)
            .OnConnectError(HandleConnectError)
            .OnDisconnect(HandleDisconnect)
            .WithUri(ServerChoices[server])
            .WithModuleName(ModuleName);
        
        if (AuthToken.Token != "")
        {
            builder = builder.WithToken(AuthToken.Token);
        }
        
        Conn = builder.Build();
    }

    // Called when we connect to SpacetimeDB and receive our client identity
    void HandleConnect(DbConnection conn, Identity identity, string token)
    {
        Debug.Log("Connected.");
        AuthToken.SaveToken(token);
        LocalIdentity = identity;
        conn.Db.Config.OnInsert += OnConfigInserted;
        conn.Db.Entity.OnInsert += OnEntityInserted;
        conn.Db.Entity.OnUpdate += OnEntityUpdated;
        conn.Db.Entity.OnDelete += OnEntityDeleted;
        conn.Db.Player.OnInsert += OnDbPlayerInserted;
        conn.Db.Player.OnUpdate += OnDbPlayerUpdated;
        conn.Db.MapTile.OnInsert += OnMapTileInserted;
        conn.Db.Line.OnInsert += LineOnOnInsert;

        
        Conn.SubscriptionBuilder()
            .Subscribe(new []
                {
                    "SELECT * FROM Player",
                    "SELECT * FROM Config",
                    "SELECT * FROM Entity",
                    "SELECT * FROM MapTile",
                    "SELECT * FROM Line",
                }
            );
        
        OnConnected?.Invoke();
    }

    private void LineOnOnInsert(EventContext context, Line row)
    {
        var line = new SharedPhysics.Line(new Vector2(row.Start.X, row.Start.Y), new Vector2(row.End.X, row.End.Y));
        Simulation.Instance.Register(line);
        PrefabManager.SpawnWall(line);
    }

    void HandleConnectError(Exception ex)
    {
        Debug.LogError($"Connection error: {ex}");
    }

    void HandleDisconnect(DbConnection conn, Exception ex)
    {
        Debug.Log("Disconnected.");
        if (ex != null)
        {
            Debug.LogException(ex);
        }
    }

    private void HandleSubscriptionApplied(SubscriptionEventContext ctx)
    {
        Debug.Log("Subscription applied!");
        OnSubscriptionApplied?.Invoke();
    }

    public static bool IsConnected()
    {
        return Conn != null && Conn.IsActive;
    }

    public void Disconnect()
    {
        Conn.Disconnect();
        Conn = null;
    }

    public static void SendInput(List<Input> inputs)
    {
        if(IsConnected() && LocalPlayer) Conn.Reducers.UpdatePlayerInput(inputs);
    }

    private static void OnMapTileInserted(EventContext context, MapTile tile)
    {
        PrefabManager.SpawnMapTile(tile);
    }
    
    private static void OnConfigInserted(EventContext context, Config insertedValue)
    {
        Debug.Log($"Got config: {insertedValue}");
        Config = insertedValue;
        Simulation.Instance.Init();
    }
    
    private static void OnEntityInserted(EventContext context, Entity insertedValue)
    {
        var entityController = PrefabManager.SpawnEntity(insertedValue);
        Entities.Add(insertedValue.EntityId, entityController);
    }
    
    private static void OnEntityUpdated(EventContext context, Entity oldEntity, Entity newEntity)
    {
        if (!Entities.TryGetValue(newEntity.EntityId, out var entityController))
        {
            return;
        }
        
        Simulation.Instance.OnEntityUpdated(new SharedPhysics.Entity()
        {
            Position = new Vector2(newEntity.Position.X, newEntity.Position.Y),
            Direction = new Vector2(newEntity.Direction.X, newEntity.Direction.Y),
            SequenceId = newEntity.SequenceId,
            Id = newEntity.EntityId,
            Speed = newEntity.Speed
        });
        
        entityController?.SendMessage("OnEntityUpdated", newEntity);
    }

    private static void OnEntityDeleted(EventContext context, Entity oldEntity)
    {
        if (Entities.Remove(oldEntity.EntityId, out var entityController))
        {
            entityController.OnDelete(context);
        }
    }
    
    private void OnDbPlayerInserted(EventContext context, Player insertedPlayer)
    {
        if (insertedPlayer?.Identity != LocalIdentity) return;
        LocalPlayer = PrefabManager.SpawnPlayer(insertedPlayer);
        Conn.Reducers.OnEnterGame += OnGameEntered;
        Conn.Reducers.EnterGame("Player");
    }

    private void OnGameEntered(ReducerEventContext context, string playerName)
    {
        var player = Conn.Db.Player.Identity.Find(LocalIdentity);
        if(player == default) return;
        LocalPlayer.EntityId = player.EntityId;
        if (Entities.Remove(LocalPlayer.EntityId, out var existingController))
        {
            Destroy(existingController.gameObject);
        }
        
        var dbEntity = Conn.Db.Entity.EntityId.Find(LocalPlayer.EntityId);
        var entityController = PrefabManager.SpawnPlayerEntity(dbEntity);
        Instance.cinemachineCamera.Target.TrackingTarget = entityController.transform;
        Simulation.Instance.SetLocalPlayerEntity(new SharedPhysics.Entity()
        {
            Position = new Vector2(dbEntity.Position.X, dbEntity.Position.Y),
            Direction = new Vector2(dbEntity.Direction.X, dbEntity.Direction.Y),
            SequenceId = dbEntity.SequenceId,
            Id = dbEntity.EntityId,
            Speed = dbEntity.Speed
        });
        Entities.Add(LocalPlayer.EntityId, entityController);
    }

    private void OnDbPlayerUpdated(EventContext context, Player oldPlayer, Player newPlayer)
    {
        if (newPlayer?.Identity != LocalIdentity) return;
        LocalPlayer.EntityId = newPlayer.EntityId;
        SendMessage("OnPlayerUpdated", newPlayer);
    }
}
