using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "MapAdmin", Public = false)]
    public partial struct MapAdmin
    {
        [PrimaryKey] public uint Id; // always 0, singleton row
        public Identity AdminIdentity;
    }
}
