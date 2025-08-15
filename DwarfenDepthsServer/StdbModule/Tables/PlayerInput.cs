using SpacetimeDB;
using Index = SpacetimeDB.Index;

public static partial class Module
{
    [Table(Name = "PlayerInput", Public = false)]
    [Index.BTree(Name = "EntityId_SequenceId", Columns = [nameof(EntityId), nameof(SequenceId)])]
    public partial struct PlayerInput
    {
        [PrimaryKey, AutoInc]
        public ulong Id;
        [Index.BTree]
        public uint EntityId;
        [Index.BTree]
        public ulong SequenceId;

        public uint TargetEntityId;
        
        
        public DbVector2 Direction;
    }
}