using SpacetimeDB;

public static partial class Module
{
    [Reducer]
    public static void UpdatePlayerInput(ReducerContext ctx, Input[] inputs)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        foreach (var input in inputs)
        {
            ctx.Db.PlayerInput.PlayerId_SequenceId.Delete((player.PlayerId, input.SequenceId));
            ctx.Db.PlayerInput.Insert(
                new PlayerInput
                {
                    PlayerId = player.PlayerId,
                    Direction = input.Direction,
                    SequenceId = input.SequenceId,
                }
            );
        }

        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? throw new Exception("Entity update not found");
        player.SimulationOffset =
            Convert.ToSByte((long)inputs.LastOrDefault().SequenceId - (long)entityUpdate.SequenceId);
        ctx.Db.Player.PlayerId.Update(player);
    }
}