using System;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;
    
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
        // In order to build a connection to SpacetimeDB we need to register
        // our callbacks and specify a SpacetimeDB server URI and module name.
        var builder = DbConnection.Builder()
            .OnConnect(HandleConnect)
            .OnConnectError(HandleConnectError)
            .OnDisconnect(HandleDisconnect)
            .WithUri(ServerURL)
            .WithModuleName(ModuleName);

        // If the user has a SpacetimeDB auth token stored in the Unity PlayerPrefs,
        // we can use it to authenticate the connection.
        if (AuthToken.Token != "")
        {
            builder = builder.WithToken(AuthToken.Token);
        }

        // Building the connection will establish a connection to the SpacetimeDB
        // server.
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
        Debug.Log(conn.Db.Config.Count);
        conn.Db.Config.OnInsert += ConfigOnInsert;
        conn.Db.Entity.OnInsert += EntityOnInsert;
        conn.Db.Entity.OnUpdate += EntityOnUpdate;
        conn.Db.Entity.OnDelete += EntityOnDelete;
        conn.Db.Player.OnInsert += PlayerOnInsert;
        conn.Db.Player.OnDelete += PlayerOnDelete;

        OnConnected?.Invoke();

        // Request all tables
        Conn.SubscriptionBuilder()
            .OnApplied(HandleSubscriptionApplied)
            .SubscribeToAllTables();
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
    
    private static void ConfigOnInsert(EventContext context, Config insertedValue)
    {
        Debug.Log($"Got config: {insertedValue}");
        Config = insertedValue;
    }
    
    private static void EntityOnInsert(EventContext context, Entity insertedValue)
    {
        var player = GetOrCreatePlayer(insertedValue.EntityId);
        var entityController = PrefabManager.SpawnEntity(insertedValue, player);
        
        if (player.isLocalPlayer)
        {
            Instance.cinemachineCamera.Target.TrackingTarget = entityController.transform;
        }

        Entities.Add(insertedValue.EntityId, entityController);
    }
    
    private static void EntityOnUpdate(EventContext context, Entity oldEntity, Entity newEntity)
    {
        if (!Entities.TryGetValue(newEntity.EntityId, out var entityController))
        {
            return;
        }
        
        entityController.SendMessage("OnEntityUpdated", newEntity);
    }

    private static void EntityOnDelete(EventContext context, Entity oldEntity)
    {
        if (Entities.Remove(oldEntity.EntityId, out var entityController))
        {
            entityController.OnDelete(context);
        }
    }
    
    private static void PlayerOnInsert(EventContext context, Player insertedPlayer)
    {
        GetOrCreatePlayer(insertedPlayer.PlayerId);
    }

    private static void PlayerOnDelete(EventContext context, Player deletedvalue)
    {
        if (Players.Remove(deletedvalue.PlayerId, out var playerController))
        {
            Destroy(playerController.gameObject);
        }
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
