using SharedPhysics;
using SpacetimeDB;

public static partial class Module
{
    [Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        Log.Info($"Initializing...");
        var config = ctx.Db.Config.Id.Find(0) ?? ctx.Db.Config.Insert(new Config());
        config.RoomSize = 10;
        config.UpdateEntityInterval = 0.050f;
        ctx.Db.Config.Id.Update(config);
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? ctx.Db.EntityUpdate.Insert(new EntityUpdate());
        entityUpdate.LastTickedAt = ctx.Timestamp;
        ctx.Db.EntityUpdate.Id.Update(entityUpdate);
        ctx.Db.moveAllEntitiesTimer.ScheduledId.Delete(0);
        ctx.Db.moveAllEntitiesTimer.Insert(new MoveAllEntitiesTimer
        {
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(config.UpdateEntityInterval / 4))
        });
        InsertRatmen(ctx, entityUpdate);
    }

    private static void InsertRatmen(ReducerContext ctx, EntityUpdate entityUpdate)
    {
        var spawn = MapData.DefaultSpawnPosition;
        var offsets = new Vector2[] { new(10f, 0f), new(20f, 0f) };
        foreach (var offset in offsets)
        {
            var pos = spawn + offset;
            ctx.Db.Entity.Insert(new Entity()
            {
                Position = new DbVector2(pos.X, pos.Y),
                Direction = new DbVector2(0, 0),
                SequenceId = entityUpdate.SequenceId,
                Speed = 7f,
                Allegiance = Faction.Ratmen
            });
        }
    }
}
