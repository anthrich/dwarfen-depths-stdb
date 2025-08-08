using SpacetimeDB;

public static partial class Module
{
    [Reducer(ReducerKind.ClientConnected)]
    public static void Connect(ReducerContext ctx)
    {
        var playerResult = ctx.Db.Player.Identity.Find(ctx.Sender);
        
        if(playerResult.HasValue) throw new Exception($"Player {ctx.Sender} is already connected");
        
        ctx.Db.Player.Insert(new Player
        {
            Identity = ctx.Sender,
            Name = "",
        });
    }
}