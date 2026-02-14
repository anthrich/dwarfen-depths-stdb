namespace SharedPhysics
{
    public readonly struct MapDefinition
    {
        public readonly Line[] Lines;
        public readonly Vector2 DefaultSpawnPosition;

        public MapDefinition(Line[] lines, Vector2 defaultSpawnPosition)
        {
            Lines = lines;
            DefaultSpawnPosition = defaultSpawnPosition;
        }
    }
}
