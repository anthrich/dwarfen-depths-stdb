using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "MapTile", Public = true)]
    public partial struct MapTile
    {
        [PrimaryKey, AutoInc]
        public uint Id;
        public DbVector2 Position;
        public float Width;
        public float Height;

        public static DbVector2 GetBottomLeftCorner(MapTile mapTile)
        {
            return new DbVector2(
                mapTile.Position.X - mapTile.Width / 2,
                mapTile.Position.Y - mapTile.Height / 2
            );
        }
        
        public static DbVector2 GetBottomRightCorner(MapTile mapTile)
        {
            return new DbVector2(
                mapTile.Position.X + mapTile.Width / 2,
                mapTile.Position.Y - mapTile.Height / 2
            );
        }
        
        public static DbVector2 GetTopLeftCorner(MapTile mapTile)
        {
            return new DbVector2(
                mapTile.Position.X - mapTile.Width / 2,
                mapTile.Position.Y + mapTile.Height / 2
            );
        }
        
        public static DbVector2 GetTopRightCorner(MapTile mapTile)
        {
            return new DbVector2(
                mapTile.Position.X + mapTile.Width / 2,
                mapTile.Position.Y + mapTile.Height / 2
            );
        }

        public static Line GetLeftWall(MapTile mapTile)
        {
            return new Line(
                GetBottomLeftCorner(mapTile),
                GetTopLeftCorner(mapTile)
            );
        }
        
        public static Line GetRightWall(MapTile mapTile)
        {
            return new Line(
                GetBottomRightCorner(mapTile),
                GetTopRightCorner(mapTile)
            );
        }
        
        public static Line GetTopWall(MapTile mapTile)
        {
            return new Line(
                GetTopLeftCorner(mapTile),
                GetTopRightCorner(mapTile)
            );
        }
        
        public static Line GetBottomWall(MapTile mapTile)
        {
            return new Line(
                GetBottomLeftCorner(mapTile),
                GetBottomRightCorner(mapTile)
            );
        }
    }
}