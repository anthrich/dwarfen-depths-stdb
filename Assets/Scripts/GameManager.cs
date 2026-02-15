using System;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Input = SpacetimeDB.Types.Input;
using Vector2 = SharedPhysics.Vector2;
using Vector3 = SharedPhysics.Vector3;

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
    public static event Action OnDisconnected;
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

        Conn.SubscriptionBuilder()
            .Subscribe(new []
                {
                    "SELECT * FROM Player",
                    "SELECT * FROM Config",
                    "SELECT * FROM Entity",
                }
            );

        OnConnected?.Invoke();
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
        OnDisconnected?.Invoke();
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

    private static void OnConfigInserted(EventContext context, Config insertedValue)
    {
        Debug.Log($"Got config: {insertedValue}");
        Config = insertedValue;
        var loadOp = SceneManager.LoadSceneAsync(insertedValue.MapName, LoadSceneMode.Additive);
        loadOp.completed += _ => Simulation.Instance.Init(insertedValue.MapName);
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
            Position = new Vector3(newEntity.Position.X, newEntity.Position.Y, newEntity.Position.Z),
            Direction = new Vector2(newEntity.Direction.X, newEntity.Direction.Y),
            SequenceId = newEntity.SequenceId,
            Id = newEntity.EntityId,
            Speed = newEntity.Speed,
            VerticalVelocity = newEntity.VerticalVelocity,
            IsGrounded = newEntity.IsGrounded,
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
        Debug.Log($"Game entered: {playerName}");
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
            Position = new Vector3(dbEntity.Position.X, dbEntity.Position.Y, dbEntity.Position.Z),
            Direction = new Vector2(dbEntity.Direction.X, dbEntity.Direction.Y),
            SequenceId = dbEntity.SequenceId,
            Id = dbEntity.EntityId,
            Speed = dbEntity.Speed,
            VerticalVelocity = dbEntity.VerticalVelocity,
            IsGrounded = dbEntity.IsGrounded,
        });
        Entities.Add(LocalPlayer.EntityId, entityController);
        Conn.Reducers.OnEnterGame -= OnGameEntered;
    }

    private void OnDbPlayerUpdated(EventContext context, Player oldPlayer, Player newPlayer)
    {
        if (newPlayer?.Identity != LocalIdentity) return;
        LocalPlayer.EntityId = newPlayer.EntityId;
        SendMessage("OnPlayerUpdated", newPlayer);
    }
}
