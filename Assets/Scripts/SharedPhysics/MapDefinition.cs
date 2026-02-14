namespace SharedPhysics
{
    public readonly struct MapDefinition
    {
        public readonly string Name;
        public readonly Line[] Lines;
        public readonly Vector2 DefaultSpawnPosition;

        public MapDefinition(string name, Line[] lines, Vector2 defaultSpawnPosition)
        {
            Name = name;
            Lines = lines;
            DefaultSpawnPosition = defaultSpawnPosition;
        }
    }
}
