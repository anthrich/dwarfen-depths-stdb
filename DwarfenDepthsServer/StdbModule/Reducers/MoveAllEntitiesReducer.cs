using SharedPhysics;
using SpacetimeDB;

public static partial class Module
{
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

            var mapTiles = ctx.Db.MapTile.Iter().ToList();
            var lines = ctx.Db.Line.Iter().Select(Line.ToPhysics).ToArray();
            
            entityUpdate.DeltaTime -= config.UpdateEntityInterval;
            var entities = ctx.Db.Entity.Iter().ToArray();
            
            foreach (var entity in entities)
            {
                var checkEntityQuery = ctx.Db.Entity.EntityId.Find(entity.EntityId);
                if (!checkEntityQuery.HasValue) continue;
                var updateEntity = UpdateEntity(
                    checkEntityQuery.Value,
                    playerInputs,
                    entityUpdate.SequenceId,
                    config,
                    mapTiles,
                    lines
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
        Config config,
        List<MapTile> mapTiles,
        SharedPhysics.Line[] lines)
    {
        var hasInput = playerInputs.TryGetValue(entity.EntityId, out var playerInput);
        var movementPerInterval = entity.Speed * config.UpdateEntityInterval;
        var direction = DbVector2.ToPhysics(hasInput ? playerInput.Direction : entity.Direction);
        var targetMovement = direction * movementPerInterval;
        var targetPosition = DbVector2.ToPhysics(entity.Position) + direction * movementPerInterval;
        
        var targetIsInsideARoom = mapTiles.Any(mt => mt.PositionIsInside(DbVector2.ToDb(targetPosition)));
        if (targetIsInsideARoom)
        {
            entity.Position = DbVector2.ToDb(targetPosition);
        }
        else
        {
            var movement = new SharedPhysics.Line(DbVector2.ToPhysics(entity.Position), targetPosition);
            var nearbyLines = Engine.GetNearbyLines(movement, lines);
            foreach (var line in nearbyLines)
            {
                var intersection = Engine.GetIntersection(movement, line);
                if (!intersection.HasValue) continue;
                var safeMovement = (intersection.Value - DbVector2.ToPhysics(entity.Position)) * 0.99f; // Small buffer
                var safePosition = DbVector2.ToPhysics(entity.Position) + safeMovement;
                var remainingMovement = targetMovement - safeMovement;
                var glideMovement = SharedPhysics.Line.GlideAlong(line, remainingMovement);
                var glideTarget = safePosition + glideMovement;
                entity.Position = DbVector2.ToDb(glideTarget);
                break;
            }
        }
        
        entity.Direction = DbVector2.ToDb(direction);
        entity.SequenceId = sequenceId;
        return entity;
    }

    private static bool PositionIsInside(this MapTile mapTile, DbVector2 position)
    {
        return mapTile.Position.X - mapTile.Width / 2 <= position.X &&
               mapTile.Position.X + mapTile.Width / 2 > position.X &&
               mapTile.Position.Y - mapTile.Height / 2 <= position.Y &&
               mapTile.Position.Y + mapTile.Height / 2 > position.Y;
    }
}