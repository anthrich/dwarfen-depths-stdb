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
        SpawnPlayer(ctx, player.PlayerId);
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