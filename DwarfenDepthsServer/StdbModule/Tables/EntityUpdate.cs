using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "EntityUpdate", Public = true)]
    public partial struct EntityUpdate
    {
        [PrimaryKey]
        public uint Id;
        public ulong SequenceId;
        public Timestamp LastTickedAt;
        public float DeltaTime;
    }
}