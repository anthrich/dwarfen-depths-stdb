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
    }
}