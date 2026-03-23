using SpacetimeDB;
using Index = SpacetimeDB.Index;

public static partial class Module
{
    [Table(Name = "MapHeightmapPatch", Public = true)]
    [Index.BTree(Name = "MapName_PatchX_PatchZ", Columns = [nameof(MapName), nameof(PatchX), nameof(PatchZ)])]
    public partial struct MapHeightmapPatch
    {
        [PrimaryKey, AutoInc] public ulong Id;
        public string MapName;
        public int PatchX, PatchZ; // floor(sampleIndex / patchSize) in each axis
        public List<float> Heights; // patchSize * patchSize floats, row-major (Z outer, X inner)
    }
}
