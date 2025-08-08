using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "Entity", Public = true)]
    public partial struct Entity
    {
        [PrimaryKey] 
        public uint EntityId;
        public float Speed;
        public DbVector2 Position;
        public DbVector2 Direction;
        public ulong SequenceId;
    }
}