using SpacetimeDB;
using Index = SpacetimeDB.Index;

public static partial class Module
{
    [Table(Name = "MapTriangleCell", Public = true)]
    [Index.BTree(Name = "MapName_CellX_CellZ", Columns = [nameof(MapName), nameof(CellX), nameof(CellZ)])]
    public partial struct MapTriangleCell
    {
        [PrimaryKey, AutoInc] public ulong Id;
        public string MapName;
        public int CellX, CellZ; // floor(centroid.X / cellSize), floor(centroid.Z / cellSize)
        // Triangle vertices — rows inserted in descending centroid-Y order within each cell
        public float V0X, V0Y, V0Z;
        public float V1X, V1Y, V1Z;
        public float V2X, V2Y, V2Z;
    }
}
