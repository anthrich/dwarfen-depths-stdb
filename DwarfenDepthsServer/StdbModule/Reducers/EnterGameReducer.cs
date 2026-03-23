using SpacetimeDB;

public static partial class Module
{
    [Reducer]
    public static void EnterGame(ReducerContext ctx, string name)
    {
        Log.Info($"Creating player with name {name}");
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        player.Name = name;
        ctx.Db.Player.Identity.Update(player);
        SpawnPlayerEntity(ctx, player);
    }

    private static void SpawnPlayerEntity(ReducerContext ctx, Player player)
    {
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? throw new Exception("EntityUpdate not found");
        var config = ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found");
        var mapCfg = ctx.Db.MapConfig.MapName.Find(config.MapName) ?? throw new Exception($"MapConfig not found for '{config.MapName}'");
        var playerEntity = ctx.Db.Entity.Insert(new Entity
        {
            Position = new DbVector3(mapCfg.SpawnX, mapCfg.SpawnY, mapCfg.SpawnZ),
            Direction = new DbVector2(0, 0),
            SequenceId = entityUpdate.SequenceId,
            Speed = 7f,
            IsGrounded = true,
            VerticalVelocity = 0,
        });
        player.EntityId = playerEntity.EntityId;
        ctx.Db.Player.Identity.Update(player);
    }
}
