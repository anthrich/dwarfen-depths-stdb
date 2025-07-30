using SpacetimeDB;

public static partial class Module
{
    const uint EntitySpeed = 10;
    private const float ScheduleEntityMovementTime = 0.05f;
    
    [Table(Name = "Config", Public = true)]
    public partial struct Config
    {
        [PrimaryKey]
        public uint Id;
        public ulong WorldSize;
        public ulong CurrentSequenceId;
    }
    
    [Table(Name = "Entity", Public = true)]
    public partial struct Entity
    {
        [PrimaryKey] 
        public uint EntityId;
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
        var config = ctx.Db.Config.Id.Find(0) ?? ctx.Db.Config.Insert(new Config());;
        config.WorldSize = 100;
        config.CurrentSequenceId = 0;
        ctx.Db.Config.Id.Update(config);
        
        ctx.Db.moveAllEntitiesTimer.Insert(new MoveAllEntitiesTimer
        {
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(1000 * ScheduleEntityMovementTime))
        });
    }
    
    [Reducer(ReducerKind.ClientConnected)]
    public static void Connect(ReducerContext ctx)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender);
        
        if(!player.HasValue)
        {
            ctx.Db.Player.Insert(new Player
            {
                Identity = ctx.Sender,
                Name = "",
            });
        }
    }
    
    [Reducer(ReducerKind.ClientDisconnected)]
    public static void Disconnect(ReducerContext ctx)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        ctx.Db.Player.Identity.Delete(player.Identity);
        ctx.Db.Entity.EntityId.Delete(player.PlayerId);
    }
    
    [Reducer]
    public static void UpdatePlayerInput(ReducerContext ctx, DbVector2 direction, ulong sequenceId)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        var directionNormalized = direction.Normalized;
        var playerInputQuery = ctx.Db.PlayerInput.PlayerId.Find(player.PlayerId);
        
        if (!playerInputQuery.HasValue) {
            ctx.Db.PlayerInput.Insert(new PlayerInput()
            {
                PlayerId = player.PlayerId,
                Direction = directionNormalized,
                SequenceId = sequenceId
            });
            
            return;
        }
        
        if(sequenceId < playerInputQuery.Value.SequenceId) return;
        var playerInput = playerInputQuery.GetValueOrDefault();
        playerInput.Direction = direction.Normalized;
        playerInput.SequenceId = sequenceId;
        ctx.Db.PlayerInput.PlayerId.Update(playerInput);
    }
    
    [Reducer]
    public static void MoveAllEntities(ReducerContext ctx, MoveAllEntitiesTimer timer)
    {
        var config = ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found");
        var worldSize = config.WorldSize;
        
        var playerInputs = ctx.Db.PlayerInput.Iter().Select(pi => (pi.PlayerId, pi)).ToDictionary();
        
        foreach (var entity in ctx.Db.Entity.Iter())
        {
            var checkEntityQuery = ctx.Db.Entity.EntityId.Find(entity.EntityId);
            if (!checkEntityQuery.HasValue) continue;
            var checkEntity = checkEntityQuery.GetValueOrDefault();
            var hasInput = playerInputs.TryGetValue(checkEntity.EntityId, out var playerInput);
            if(!hasInput) continue;
            var newPos = checkEntity.Position + playerInput.Direction * (EntitySpeed * ScheduleEntityMovementTime);
            checkEntity.Position.X = Math.Clamp(newPos.X, 0, worldSize);
            checkEntity.Position.Y= Math.Clamp(newPos.Y, 0, worldSize);
            checkEntity.SequenceId = playerInput.SequenceId;
            ctx.Db.Entity.EntityId.Update(checkEntity);
        }

        config.CurrentSequenceId++;
        ctx.Db.Config.Id.Update(config);
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
    
    public static Entity SpawnPlayer(ReducerContext ctx, uint playerId)
    {
        var rng = ctx.Rng;
        var worldSize = (ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found")).WorldSize;
        var x = rng.NextSingle() * worldSize;
        var y = rng.NextSingle() * worldSize;
        return SpawnPlayerAt(
            ctx,
            playerId,
            new DbVector2(x, y)
        );
    }

    public static Entity SpawnPlayerAt(
        ReducerContext ctx, uint playerId, DbVector2 position)
    {
        var entity = ctx.Db.Entity.Insert(new Entity
        {
            EntityId = playerId,
            Position = position,
            Direction = new DbVector2(0,0),
        });
        
        return entity;
    }
}