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
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? throw new Exception("EntityUpdate not found");
        var mapTile = ctx.Db.MapTile.Iter().First();
        ctx.Db.Entity.Insert(new Entity
        {
            EntityId = playerId,
            Position = mapTile.Position,
            Direction = new DbVector2(0,0),
            SequenceId = entityUpdate.SequenceId,
            Speed = 10f
        });
    }
}