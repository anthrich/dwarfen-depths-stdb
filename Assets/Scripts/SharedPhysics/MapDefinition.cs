namespace SharedPhysics
{
    public readonly struct MapDefinition
    {
        public readonly string Name;
        public readonly Line[] Lines;
        public readonly Triangle[] Triangles;
        public readonly Vector3 DefaultSpawnPosition;

        public MapDefinition(string name, Line[] lines, Triangle[] triangles, Vector3 defaultSpawnPosition)
        {
            Name = name;
            Lines = lines;
            Triangles = triangles;
            DefaultSpawnPosition = defaultSpawnPosition;
        }
    }
}
