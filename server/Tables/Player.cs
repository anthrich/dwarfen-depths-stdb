using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "Player", Public = true)]
    public partial struct Player
    {
        [PrimaryKey]
        public Identity Identity;
        [Unique, AutoInc]
        public uint PlayerId;
        public string Name;
        public DbVector2 Position;
        public sbyte SimulationOffset;
    }
}