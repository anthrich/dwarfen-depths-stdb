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
        InsertMap(ctx, config);
    }

    private static void InsertMap(ReducerContext ctx, Config config)
    {
        var walls = new List<Line>();
        
        foreach (var room in LevelData.Rooms)
        {
            var mapTile = new MapTile
            {
                Position = new DbVector2((float)room.x * config.RoomSize, (float)room.y * config.RoomSize),
                Width = config.RoomSize,
                Height = config.RoomSize
            };
            ctx.Db.MapTile.Insert(mapTile);
            
            Log.Info($"MapTile inserted with id {mapTile.Id}");
            
            if (!LevelData.HasRoomAt(room.x - 1, room.y))
            {
                ctx.Db.Line.Insert(MapTile.GetLeftWall(mapTile));
            }
            if (!LevelData.HasRoomAt(room.x + 1, room.y))
            {
                ctx.Db.Line.Insert(MapTile.GetRightWall(mapTile));
            }
            if (!LevelData.HasRoomAt(room.x, room.y + 1))
            {
                ctx.Db.Line.Insert(MapTile.GetTopWall(mapTile));
            }
            if (!LevelData.HasRoomAt(room.x, room.y - 1))
            {
                ctx.Db.Line.Insert(MapTile.GetBottomWall(mapTile));
            }
        }
    }
}