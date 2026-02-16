namespace SharedPhysics
{
    public readonly struct MapDefinition
    {
        public readonly string Name;
        public readonly Line[] Lines;
        public readonly Triangle[] Triangles;
        public readonly Vector3 DefaultSpawnPosition;

        // Heightmap data (null for mesh-based maps)
        public readonly float[]? HeightmapData;
        public readonly int HeightmapResolution;
        public readonly float HeightmapOriginX;
        public readonly float HeightmapOriginZ;
        public readonly float HeightmapSizeX;
        public readonly float HeightmapSizeZ;

        public bool HasHeightmap => HeightmapData != null && HeightmapData.Length > 0;

        // Constructor for mesh-based maps (backward compatible)
        public MapDefinition(string name, Line[] lines, Triangle[] triangles, Vector3 defaultSpawnPosition)
        {
            Name = name;
            Lines = lines;
            Triangles = triangles;
            DefaultSpawnPosition = defaultSpawnPosition;
            HeightmapData = null;
            HeightmapResolution = 0;
            HeightmapOriginX = 0;
            HeightmapOriginZ = 0;
            HeightmapSizeX = 0;
            HeightmapSizeZ = 0;
        }

        // Constructor for heightmap-based maps
        public MapDefinition(string name, Line[] lines, Vector3 defaultSpawnPosition,
                             float[] heightmapData, int heightmapResolution,
                             float heightmapOriginX, float heightmapOriginZ,
                             float heightmapSizeX, float heightmapSizeZ)
        {
            Name = name;
            Lines = lines;
            Triangles = System.Array.Empty<Triangle>();
            DefaultSpawnPosition = defaultSpawnPosition;
            HeightmapData = heightmapData;
            HeightmapResolution = heightmapResolution;
            HeightmapOriginX = heightmapOriginX;
            HeightmapOriginZ = heightmapOriginZ;
            HeightmapSizeX = heightmapSizeX;
            HeightmapSizeZ = heightmapSizeZ;
        }
    }
}
