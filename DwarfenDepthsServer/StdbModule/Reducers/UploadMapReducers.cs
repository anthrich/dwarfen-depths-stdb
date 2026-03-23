using SharedPhysics;
using SpacetimeDB;

public static partial class Module
{
    private static void RequireAdmin(ReducerContext ctx)
    {
        var admin = ctx.Db.MapAdmin.Id.Find(0);
        if (admin == null)
        {
            // First caller claims admin role
            ctx.Db.MapAdmin.Insert(new MapAdmin { Id = 0, AdminIdentity = ctx.Sender });
            return;
        }
        if (ctx.Sender != admin.Value.AdminIdentity)
            throw new Exception("Unauthorized: only the map admin may call this reducer.");
    }

    [Reducer]
    public static void UploadMapConfig(ReducerContext ctx, MapConfig config)
    {
        RequireAdmin(ctx);
        var existing = ctx.Db.MapConfig.MapName.Find(config.MapName);
        if (existing.HasValue)
            ctx.Db.MapConfig.MapName.Update(config);
        else
            ctx.Db.MapConfig.Insert(config);
    }

    [Reducer]
    public static void UploadMapTriangleBatch(ReducerContext ctx, string mapName, List<MapTriangleCell> cells)
    {
        RequireAdmin(ctx);
        foreach (var cell in cells)
        {
            var row = cell;
            row.MapName = mapName;
            ctx.Db.MapTriangleCell.Insert(row);
        }
    }

    [Reducer]
    public static void UploadMapHeightmapBatch(ReducerContext ctx, string mapName, List<MapHeightmapPatch> patches)
    {
        RequireAdmin(ctx);
        foreach (var patch in patches)
        {
            var row = patch;
            row.MapName = mapName;
            ctx.Db.MapHeightmapPatch.Insert(row);
        }
    }

    [Reducer]
    public static void ClearMapData(ReducerContext ctx, string mapName)
    {
        RequireAdmin(ctx);
        foreach (var cell in ctx.Db.MapTriangleCell.Iter().Where(c => c.MapName == mapName).ToArray())
            ctx.Db.MapTriangleCell.Id.Delete(cell.Id);
        foreach (var patch in ctx.Db.MapHeightmapPatch.Iter().Where(p => p.MapName == mapName).ToArray())
            ctx.Db.MapHeightmapPatch.Id.Delete(patch.Id);
        // Reset server-side caches so next tick reloads from updated data
        _loadedMapName = null;
        _heightmap = null;
        _triangleCache.Clear();
    }

    [Reducer]
    public static void SpawnDefaultEntities(ReducerContext ctx)
    {
        RequireAdmin(ctx);
        var config = ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found");
        var mapCfg = ctx.Db.MapConfig.MapName.Find(config.MapName)
            ?? throw new Exception($"MapConfig not found for '{config.MapName}'");
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? throw new Exception("EntityUpdate not found");

        var spawnXz = new Vector2(mapCfg.SpawnX, mapCfg.SpawnZ);
        var offsets = new Vector2[] { new(10f, 0f), new(20f, 0f) };
        foreach (var offset in offsets)
        {
            var pos = spawnXz + offset;
            ctx.Db.Entity.Insert(new Entity
            {
                Position = new DbVector3(pos.X, mapCfg.SpawnY, pos.Y),
                Direction = new DbVector2(0, 0),
                SequenceId = entityUpdate.SequenceId,
                Speed = 7f,
                Allegiance = Faction.Ratmen,
                IsGrounded = true,
                VerticalVelocity = 0,
            });
        }
    }

    [Reducer]
    public static void SetMapAdmin(ReducerContext ctx, Identity newAdmin)
    {
        RequireAdmin(ctx);
        var row = ctx.Db.MapAdmin.Id.Find(0)!.Value;
        row.AdminIdentity = newAdmin;
        ctx.Db.MapAdmin.Id.Update(row);
    }
}
