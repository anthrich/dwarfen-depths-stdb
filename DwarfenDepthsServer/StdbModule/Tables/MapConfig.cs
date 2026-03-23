using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "MapConfig", Public = true)]
    public partial struct MapConfig
    {
        [PrimaryKey] public string MapName;
        public float SpawnX, SpawnY, SpawnZ;
        // Heightmap metadata (all zero for triangle-based maps)
        public float HeightmapOriginX, HeightmapOriginZ;
        public float HeightmapSizeX, HeightmapSizeZ;
        public int HeightmapResolution;
        public int HeightmapPatchSize; // samples per patch side, e.g. 16
        // Triangle cell size in world units (e.g. 8f); zero for heightmap maps
        public float TriangleCellSize;
    }
}
