using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "Config", Public = true)]
    public partial struct Config
    {
        [PrimaryKey]
        public uint Id;
        public ulong WorldSize;
        public float UpdateEntityTickRate;
        public float UpdateEntityInterval;
    }
    
    [Table(Name = "EntityUpdate", Public = false)]
    public partial struct EntityUpdate
    {
        [PrimaryKey]
        public uint Id;
        public ulong SequenceId;
        public Timestamp LastTickedAt;
        public float DeltaTime;
    }
    
    [Table(Name = "Entity", Public = true)]
    public partial struct Entity
    {
        [PrimaryKey] 
        public uint EntityId;
        public float Speed;
        public DbVector2 Position;
        public DbVector2 Direction;
        public ulong SequenceId;
    }
    
    [Table(Name = "Player", Public = true)]
    public partial struct Player
    {
        [PrimaryKey]
        public Identity Identity;
        [Unique, AutoInc]
        public uint PlayerId;
        public string Name;
    }

    [Table(Name = "PlayerInput", Public = false)]
    public partial struct PlayerInput
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public uint PlayerId;
        public DbVector2 Direction;
        [SpacetimeDB.Index.BTree]
        public ulong SequenceId;
    }
    
    [Table(Name = "moveAllEntitiesTimer", Scheduled = nameof(MoveAllEntities), ScheduledAt = nameof(ScheduledAt))]
    public partial struct MoveAllEntitiesTimer
    {
        [PrimaryKey, AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }
    
    [Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        Log.Info($"Initializing...");
        var config = ctx.Db.Config.Id.Find(0) ?? ctx.Db.Config.Insert(new Config());
        config.WorldSize = 100;
        config.UpdateEntityTickRate = 0.01f;
        config.UpdateEntityInterval = 0.05f;
        ctx.Db.Config.Id.Update(config);
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? ctx.Db.EntityUpdate.Insert(new EntityUpdate());
        entityUpdate.LastTickedAt = ctx.Timestamp;
        ctx.Db.EntityUpdate.Id.Update(entityUpdate);
        
        ctx.Db.moveAllEntitiesTimer.Insert(new MoveAllEntitiesTimer
        {
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(config.UpdateEntityTickRate))
        });
    }

    [Reducer(ReducerKind.ClientConnected)]
    public static void Connect(ReducerContext ctx)
    {
        var playerResult = ctx.Db.Player.Identity.Find(ctx.Sender);
        
        if(playerResult.HasValue) throw new Exception($"Player {ctx.Sender} is already connected");
        
        ctx.Db.Player.Insert(new Player
        {
            Identity = ctx.Sender,
            Name = "",
        });
    }

    [Reducer(ReducerKind.ClientDisconnected)]
    public static void Disconnect(ReducerContext ctx)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        ctx.Db.Player.Identity.Delete(player.Identity);
        ctx.Db.Entity.EntityId.Delete(player.PlayerId);
        ctx.Db.PlayerInput.PlayerId.Delete(player.PlayerId);
    }

    [Reducer]
    public static void EnterGame(ReducerContext ctx, string name)
    {
        Log.Info($"Creating player with name {name}");
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        player.Name = name;
        ctx.Db.Player.Identity.Update(player);
        SpawnPlayer(ctx, player.PlayerId);
    }

    [Reducer]
    public static void UpdatePlayerInput(ReducerContext ctx, Input input)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        ctx.Db.PlayerInput.Insert(
            new PlayerInput
            {
                PlayerId = player.PlayerId,
                Direction = input.Direction,
                SequenceId = input.SequenceId
            }
        );
    }

    [Reducer]
    public static void MoveAllEntities(ReducerContext ctx, MoveAllEntitiesTimer timer)
    {
        if (ctx.Sender != ctx.Identity)
        {
            throw new Exception("MoveAllEntities may not be invoked by clients, only via scheduling.");
        }
        
        var config = ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found");
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? throw new Exception("EntityUpdate not found");
        
        TimeSpan timeSinceLastTick = ctx.Timestamp.TimeDurationSince(entityUpdate.LastTickedAt);
        var secondsSinceLastTick = timeSinceLastTick.Milliseconds / 1000f;
        entityUpdate.DeltaTime += secondsSinceLastTick;
        
        while (entityUpdate.DeltaTime >= config.UpdateEntityInterval)
        {
            var playerInputs = ctx.Db.PlayerInput.SequenceId.Filter(entityUpdate.SequenceId)
                .GroupBy(pi => pi.PlayerId)
                .Select(grp => (grp.Key, grp.First()))
                .ToDictionary();
            
            entityUpdate.DeltaTime -= config.UpdateEntityInterval;
            var entities = ctx.Db.Entity.Iter().ToArray();
            
            foreach (var entity in entities)
            {
                var checkEntityQuery = ctx.Db.Entity.EntityId.Find(entity.EntityId);
                if (!checkEntityQuery.HasValue) continue;
                var updateEntity = UpdateEntity(
                    checkEntityQuery.Value, playerInputs, entityUpdate.SequenceId, config
                );
                ctx.Db.Entity.EntityId.Update(updateEntity);
            }
            
            ctx.Db.PlayerInput.SequenceId.Delete((0, entityUpdate.SequenceId));
            entityUpdate.SequenceId++;
        }
        
        entityUpdate.LastTickedAt = ctx.Timestamp;
        ctx.Db.EntityUpdate.Id.Update(entityUpdate);
    }

    private static Entity UpdateEntity(
        Entity entity,
        Dictionary<uint, PlayerInput> playerInputs,
        ulong sequenceId,
        Config config)
    {
        var hasInput = playerInputs.TryGetValue(entity.EntityId, out var playerInput);
        Log.Info($"Found player inputs: {playerInput.SequenceId}");
        
        if (hasInput)
        {
            var movementPerInterval = entity.Speed * config.UpdateEntityInterval;
            var newPos = entity.Position + playerInput.Direction * movementPerInterval;
            entity.Position.X = Math.Clamp(newPos.X, 0, config.WorldSize);
            entity.Position.Y= Math.Clamp(newPos.Y, 0, config.WorldSize);
        }
        
        entity.SequenceId = sequenceId;
        return entity;
    }

    private static void SpawnPlayer(ReducerContext ctx, uint playerId)
    {
        var config = ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found");
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? throw new Exception("EntityUpdate not found");
        var x = ctx.Rng.NextSingle() * config.WorldSize;
        var y = ctx.Rng.NextSingle() * config.WorldSize;
        ctx.Db.Entity.Insert(new Entity
        {
            EntityId = playerId,
            Position = new DbVector2(x, y),
            Direction = new DbVector2(0,0),
            SequenceId = entityUpdate.SequenceId,
            Speed = 10f
        });
    }
}