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
        public uint PlayerId;
        public DbVector2 Direction;
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
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? ctx.Db.Player.Insert(new Player
        {
            Identity = ctx.Sender,
            Name = "",
        });
        var playerInput = ctx.Db.PlayerInput.PlayerId.Find(player.PlayerId);
        if (playerInput.HasValue) return;
        ctx.Db.PlayerInput.Insert(new PlayerInput
        {
            PlayerId = player.PlayerId,
            Direction = new DbVector2(),
            SequenceId = 0
        });
    }

    [Reducer(ReducerKind.ClientDisconnected)]
    public static void Disconnect(ReducerContext ctx)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        ctx.Db.Player.Identity.Delete(player.Identity);
        ctx.Db.PlayerInput.PlayerId.Delete(player.PlayerId);
        ctx.Db.Entity.EntityId.Delete(player.PlayerId);
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
        var playerInputQuery = ctx.Db.PlayerInput.PlayerId.Find(player.PlayerId);
        if (!playerInputQuery.HasValue) return;
        var playerInput = playerInputQuery.GetValueOrDefault();
        playerInput.Direction = input.Direction.Normalized;
        playerInput.SequenceId = input.SequenceId;
        ctx.Db.PlayerInput.PlayerId.Update(playerInput);
    }

    [Reducer]
    public static void MoveAllEntities(ReducerContext ctx, MoveAllEntitiesTimer timer)
    {
        var config = ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found");
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? throw new Exception("EntityUpdate not found");
        var worldSize = config.WorldSize;
        
        var playerInputs = ctx.Db.PlayerInput.Iter().Select(pi => (pi.PlayerId, pi)).ToDictionary();
        var timeSinceLastTick =
            ((TimeSpan)ctx.Timestamp.TimeDurationSince(entityUpdate.LastTickedAt)).Milliseconds / 1000f;
        entityUpdate.DeltaTime += timeSinceLastTick;
        var entities = ctx.Db.Entity.Iter().ToArray();
        
        while (entityUpdate.DeltaTime >= config.UpdateEntityInterval)
        {
            entityUpdate.DeltaTime -= config.UpdateEntityInterval;
            
            foreach (var entity in entities)
            {
                var checkEntityQuery = ctx.Db.Entity.EntityId.Find(entity.EntityId);
                if (!checkEntityQuery.HasValue) continue;
                var checkEntity = checkEntityQuery.GetValueOrDefault();
                var hasInput = playerInputs.TryGetValue(checkEntity.EntityId, out var playerInput);
                if(!hasInput) continue;
                var movementPerInterval = checkEntity.Speed * config.UpdateEntityInterval;
                var newPos = checkEntity.Position + playerInput.Direction * movementPerInterval;
                checkEntity.Position.X = Math.Clamp(newPos.X, 0, worldSize);
                checkEntity.Position.Y= Math.Clamp(newPos.Y, 0, worldSize);
                checkEntity.SequenceId = playerInput.SequenceId;
                ctx.Db.Entity.EntityId.Update(checkEntity);
            }
        }
        
        entityUpdate.LastTickedAt = ctx.Timestamp;
        ctx.Db.EntityUpdate.Id.Update(entityUpdate);
    }

    public static Entity SpawnPlayer(ReducerContext ctx, uint playerId)
    {
        var rng = ctx.Rng;
        var worldSize = (ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found")).WorldSize;
        var x = rng.NextSingle() * worldSize;
        var y = rng.NextSingle() * worldSize;
        return SpawnEntityAt(
            ctx,
            playerId,
            new DbVector2(x, y)
        );
    }

    public static Entity SpawnEntityAt(
        ReducerContext ctx, uint playerId, DbVector2 position)
    {
        var entity = ctx.Db.Entity.Insert(new Entity
        {
            EntityId = playerId,
            Position = position,
            Direction = new DbVector2(0,0),
            SequenceId = 0,
            Speed = 10f
        });
        
        return entity;
    }
}