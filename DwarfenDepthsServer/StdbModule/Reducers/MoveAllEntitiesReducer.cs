using System.Collections.Generic;
using System.Linq;
using SharedPhysics;
using SpacetimeDB;

public static partial class Module
{
    private static string? _loadedMapName;
    private static Heightmap? _heightmap;
    private static Dictionary<(int, int), Triangle[]> _triangleCache = new();

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

        var mapCfg = ctx.Db.MapConfig.MapName.Find(config.MapName);
        if (!mapCfg.HasValue)
        {
            // Map data not yet uploaded — skip simulation until it arrives.
            entityUpdate.LastTickedAt = ctx.Timestamp;
            ctx.Db.EntityUpdate.Id.Update(entityUpdate);
            return;
        }

        // Heightmap maps: lazy-load all patches once into a Heightmap object
        if (mapCfg.Value.HeightmapResolution > 0 && _heightmap == null)
        {
            var patches = ctx.Db.MapHeightmapPatch.Iter()
                .Where(p => p.MapName == config.MapName)
                .ToList();
            _heightmap = ReconstructHeightmap(mapCfg.Value, patches);
            _loadedMapName = config.MapName;
        }

        while (entityUpdate.DeltaTime >= config.UpdateEntityInterval)
        {
            var playerInputs = ctx.Db.PlayerInput.SequenceId.Filter(entityUpdate.SequenceId)
                .GroupBy(pi => pi.EntityId)
                .Select(grp => (grp.Key, grp.First()))
                .ToDictionary();

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
                    mapCfg.Value,
                    ctx
                );
                ctx.Db.Entity.EntityId.Update(updateEntity);
            }

            ctx.Db.PlayerInput.SequenceId.Delete((0, entityUpdate.SequenceId));
            entityUpdate.SequenceId++;
        }

        entityUpdate.LastTickedAt = ctx.Timestamp;
        ctx.Db.EntityUpdate.Id.Update(entityUpdate);
    }

    private static Heightmap ReconstructHeightmap(MapConfig cfg, List<MapHeightmapPatch> patches)
    {
        var heights = new float[cfg.HeightmapResolution * cfg.HeightmapResolution];
        int ps = cfg.HeightmapPatchSize;
        foreach (var patch in patches)
        {
            for (int row = 0; row < ps; row++)
            for (int col = 0; col < ps; col++)
            {
                int globalZ = patch.PatchZ * ps + row;
                int globalX = patch.PatchX * ps + col;
                if (globalZ < cfg.HeightmapResolution && globalX < cfg.HeightmapResolution)
                    heights[globalZ * cfg.HeightmapResolution + globalX] = patch.Heights[row * ps + col];
            }
        }
        return new Heightmap(heights, cfg.HeightmapResolution,
            cfg.HeightmapOriginX, cfg.HeightmapOriginZ,
            cfg.HeightmapSizeX, cfg.HeightmapSizeZ);
    }

    private static ITerrain? BuildLocalTerrain(ReducerContext ctx, MapConfig mapCfg, DbVector3 pos)
    {
        if (mapCfg.HeightmapResolution > 0) return _heightmap; // pre-loaded once

        // Triangle map: lazy-cache cells near entity position
        float cs = mapCfg.TriangleCellSize;
        if (cs <= 0) return null;
        float radius = cs * 1.5f;
        int minCX = (int)MathF.Floor((pos.X - radius) / cs);
        int maxCX = (int)MathF.Floor((pos.X + radius) / cs);
        int minCZ = (int)MathF.Floor((pos.Z - radius) / cs);
        int maxCZ = (int)MathF.Floor((pos.Z + radius) / cs);

        var tris = new List<Triangle>();
        for (int cx = minCX; cx <= maxCX; cx++)
        for (int cz = minCZ; cz <= maxCZ; cz++)
        {
            var key = (cx, cz);
            if (!_triangleCache.TryGetValue(key, out var cached))
            {
                cached = ctx.Db.MapTriangleCell.MapName_CellX_CellZ
                    .Filter((mapCfg.MapName, cx, cz))
                    .Select(c => new Triangle(
                        new Vector3(c.V0X, c.V0Y, c.V0Z),
                        new Vector3(c.V1X, c.V1Y, c.V1Z),
                        new Vector3(c.V2X, c.V2Y, c.V2Z)))
                    .ToArray();
                _triangleCache[key] = cached;
            }
            tris.AddRange(cached);
        }

        // Sort descending centroid-Y so TerrainGrid returns uppermost surface first
        tris.Sort((a, b) =>
            ((b.V0.Y + b.V1.Y + b.V2.Y) / 3f)
            .CompareTo((a.V0.Y + a.V1.Y + a.V2.Y) / 3f));
        return new TerrainGrid(tris.ToArray());
    }

    private static Entity UpdateEntity(
        Entity entity,
        Dictionary<uint, PlayerInput> playerInputs,
        ulong sequenceId,
        Config config,
        MapConfig mapCfg,
        ReducerContext ctx)
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

        var terrain = BuildLocalTerrain(ctx, mapCfg, entity.Position);
        var simulated = Engine.Simulate(
            config.UpdateEntityInterval,
            sequenceId,
            [physicsEntity],
            terrain
        );

        return Entity.FromPhysics(simulated[0]);
    }
}
