using SpacetimeDB;
using Index = SpacetimeDB.Index;

public static partial class Module
{
    [Table(Name = "PlayerInput", Public = false)]
    [Index.BTree(Name = "PlayerId_SequenceId", Columns = [nameof(PlayerId), nameof(SequenceId)])]
    public partial struct PlayerInput
    {
        [PrimaryKey, AutoInc]
        public ulong Id;
        
        [Index.BTree]
        public uint PlayerId;
        
        [Index.BTree]
        public ulong SequenceId;
        
        public DbVector2 Direction;
    }
}