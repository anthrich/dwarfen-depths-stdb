using SpacetimeDB;

public static partial class Module
{
    [Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        Log.Info($"Initializing...");
        var config = ctx.Db.Config.Id.Find(0) ?? ctx.Db.Config.Insert(new Config
        {
            RoomSize = 10,
            UpdateEntityInterval = 0.050f,
            MapName = "Forest"
        });
        config.RoomSize = 10;
        config.UpdateEntityInterval = 0.050f;
        config.MapName = "Forest";
        ctx.Db.Config.Id.Update(config);
        var entityUpdate = ctx.Db.EntityUpdate.Id.Find(0) ?? ctx.Db.EntityUpdate.Insert(new EntityUpdate());
        entityUpdate.LastTickedAt = ctx.Timestamp;
        ctx.Db.EntityUpdate.Id.Update(entityUpdate);
        ctx.Db.moveAllEntitiesTimer.ScheduledId.Delete(0);
        ctx.Db.moveAllEntitiesTimer.Insert(new MoveAllEntitiesTimer
        {
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(config.UpdateEntityInterval / 4))
        });
        // Entity spawning is deferred to SpawnDefaultEntities, called after map data is uploaded.
    }
}
