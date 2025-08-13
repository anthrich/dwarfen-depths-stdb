using SpacetimeDB;

public static partial class Module
{
    [Reducer]
    public static void UpdatePlayerInput(ReducerContext ctx, Input[] inputs)
    {
        var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
        foreach (var input in inputs)
        {
            ctx.Db.PlayerInput.EntityId_SequenceId.Delete((player.EntityId, input.SequenceId));
            ctx.Db.PlayerInput.Insert(
                new PlayerInput
                {
                    EntityId = player.EntityId,
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