using System;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(NetworkTime))]
public class GameManager : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;
    public NetworkTime networkTime;
    
    public static readonly Dictionary<string, string> ServerChoices = new()
    {
        {"Local", "http://127.0.0.1:3000"}
    };
    
    const string ServerURL = "http://127.0.0.1:3000";
    const string ModuleName = "spacetimer";

    public static event Action OnConnected;
    public static event Action OnSubscriptionApplied;

	public static GameManager Instance { get; private set; }
    public static Identity LocalIdentity { get; private set; }
    public static PlayerController LocalPlayer { get; private set; }
    public static Config Config { get; private set; }
    public static DbConnection Conn { get; private set; }

    private static readonly Dictionary<uint, EntityController> Entities = new();
    private static readonly Dictionary<uint, PlayerController> Players = new();

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

    public void JoinGame()
    {
        Conn.Reducers.EnterGame("Player " + (Players.Count + 1));
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
        conn.Db.Player.OnInsert += OnPlayerInserted;
        conn.Db.Player.OnUpdate += OnPlayerUpdated;
        conn.Db.Player.OnDelete += OnPlayerDeleted;
        conn.Db.MapTile.OnInsert += OnMapTileInserted;

        // Request all tables
        Conn.SubscriptionBuilder()
            .OnApplied(HandleSubscriptionApplied)
            .SubscribeToAllTables();
        
        OnConnected?.Invoke();
        
        Conn.Reducers.EnterGame("Player " + (Players.Count + 1));
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

    private static void OnMapTileInserted(EventContext context, MapTile tile)
    {
        PrefabManager.SpawnMapTile(tile);
    }
    
    private static void OnConfigInserted(EventContext context, Config insertedValue)
    {
        Debug.Log($"Got config: {insertedValue}");
        Config = insertedValue;
    }
    
    private static void OnEntityInserted(EventContext context, Entity insertedValue)
    {
        var player = GetOrCreatePlayer(insertedValue.EntityId);
        var entityController = PrefabManager.SpawnEntity(insertedValue, player);
        
        if (player.isLocalPlayer)
        {
            Instance.cinemachineCamera.Target.TrackingTarget = entityController.transform;
        }

        Entities.Add(insertedValue.EntityId, entityController);
    }
    
    private static void OnEntityUpdated(EventContext context, Entity oldEntity, Entity newEntity)
    {
        if (!Entities.TryGetValue(newEntity.EntityId, out var entityController))
        {
            return;
        }
        
        entityController.SendMessage("OnEntityUpdated", newEntity);
    }

    private static void OnEntityDeleted(EventContext context, Entity oldEntity)
    {
        if (Entities.Remove(oldEntity.EntityId, out var entityController))
        {
            entityController.OnDelete(context);
        }
    }
    
    private static void OnPlayerInserted(EventContext context, Player insertedPlayer)
    {
        GetOrCreatePlayer(insertedPlayer.PlayerId);
    }

    private static void OnPlayerDeleted(EventContext context, Player deletedvalue)
    {
        if (Players.Remove(deletedvalue.PlayerId, out var playerController))
        {
            Destroy(playerController.gameObject);
        }
    }

    private static void OnPlayerUpdated(EventContext context, Player oldPlayer, Player newPlayer)
    {
        Instance.SendMessage("OnPlayerUpdated", newPlayer);
    }
    
    private static PlayerController GetOrCreatePlayer(uint playerId)
    {
        if (Players.TryGetValue(playerId, out var playerController))
            return playerController;
        
        var player = Conn.Db.Player.PlayerId.Find(playerId);
        playerController = PrefabManager.SpawnPlayer(player);
        if (player?.Identity == LocalIdentity) LocalPlayer = playerController;
        Players.Add(playerId, playerController);

        return playerController;
    }
}
