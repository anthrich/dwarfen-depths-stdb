using SpacetimeDB;

public static partial class Module
{
    [Reducer(ReducerKind.ClientDisconnected)]
    public static void Disconnect(ReducerContext ctx)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        ctx.Db.Player.Identity.Delete(player.Identity);
        ctx.Db.Entity.EntityId.Delete(player.PlayerId);
        ctx.Db.PlayerInput.PlayerId.Delete(player.PlayerId);
    }
}