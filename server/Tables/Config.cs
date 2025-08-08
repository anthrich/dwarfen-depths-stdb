using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "Config", Public = true)]
    public partial struct Config
    {
        [PrimaryKey]
        public uint Id;
        public ulong WorldSize;
        public float UpdateEntityInterval;
    }
}