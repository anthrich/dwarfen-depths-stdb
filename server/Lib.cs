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
    }
    
    [Table(Name = "Entity", Public = true)]
    public partial struct Entity
    {
        [PrimaryKey] 
        public uint EntityId;
        public DbVector2 Position;
        public DbVector2 Direction;
        public Timestamp LastSplitTime;
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
        ctx.Db.Config.Insert(new Config { WorldSize = 100 });
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
    public static void UpdatePlayerInput(ReducerContext ctx, DbVector2 direction)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        var entityQuery = ctx.Db.Entity.EntityId.Find(player.PlayerId);
        if (entityQuery.HasValue)
        {
            var entity = entityQuery.GetValueOrDefault();
            entity.Direction = direction.Normalized;
            ctx.Db.Entity.EntityId.Update(entity);
        }
    }
    
    [Reducer]
    public static void MoveAllEntities(ReducerContext ctx, MoveAllEntitiesTimer timer)
    {
        var worldSize = (ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found")).WorldSize;

        var entityDirections = ctx.Db.Entity.Iter().Select(c => (c.EntityId, c.Direction * EntitySpeed)).ToDictionary();
        
        foreach (var entity in ctx.Db.Entity.Iter())
        {
            var checkEntityQuery = ctx.Db.Entity.EntityId.Find(entity.EntityId);
            if (!checkEntityQuery.HasValue) continue;
            var checkEntity = checkEntityQuery.GetValueOrDefault();
            var direction = entityDirections[checkEntity.EntityId];
            var newPos = checkEntity.Position + direction * ScheduleEntityMovementTime;
            checkEntity.Position.X = Math.Clamp(newPos.X, 0, worldSize);
            checkEntity.Position.Y= Math.Clamp(newPos.Y, 0, worldSize);
            ctx.Db.Entity.EntityId.Update(checkEntity);
        }
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