using SharedPhysics;
using SpacetimeDB;

public static partial class Module
{
    private static TerrainGrid? _terrainGrid;

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

        var map = MapData.GetMap(config.MapName);
        _terrainGrid ??= map.Triangles.Length > 0 ? new TerrainGrid(map.Triangles) : null;

        while (entityUpdate.DeltaTime >= config.UpdateEntityInterval)
        {
            var playerInputs = ctx.Db.PlayerInput.SequenceId.Filter(entityUpdate.SequenceId)
                .GroupBy(pi => pi.EntityId)
                .Select(grp => (grp.Key, grp.First()))
                .ToDictionary();

            var lines = map.Lines;

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
                    lines,
                    _terrainGrid
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
        SharedPhysics.Line[] lines,
        TerrainGrid? terrain)
    {
        var hasInput = playerInputs.TryGetValue(entity.EntityId, out var playerInput);
        entity.Direction = hasInput ? playerInput.Direction : entity.Direction;
        entity.Rotation = hasInput ? playerInput.Rotation : entity.Rotation;

        var physicsEntity = Entity.ToPhysics(entity);

        if (hasInput && playerInput.Jump && physicsEntity.IsGrounded)
        {
            physicsEntity.VerticalVelocity = Engine.JumpImpulse;
            physicsEntity.IsGrounded = false;
        }

        var simulated = Engine.Simulate(
            config.UpdateEntityInterval,
            sequenceId,
            [physicsEntity],
            lines,
            terrain
        );

        return Entity.FromPhysics(simulated[0]);
    }
}
