using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "Entity", Public = true)]
    public partial struct Entity
    {
        [PrimaryKey, AutoInc]
        public uint EntityId;
        public float Speed;
        public DbVector2 Position;
        public DbVector2 Direction;
        public ulong SequenceId;
        public Faction Allegiance;
        public uint TargetEntityId;

        public static SharedPhysics.Entity ToPhysics(Entity entity)
        {
            return new SharedPhysics.Entity
            {
                Id = entity.EntityId,
                Position = DbVector2.ToPhysics(entity.Position),
                Direction = DbVector2.ToPhysics(entity.Direction),
                Speed = entity.Speed,
                SequenceId = entity.SequenceId
            };
        }

        public static Entity FromPhysics(SharedPhysics.Entity entity)
        {
            return new Entity
            {
                EntityId = entity.Id,
                Position = DbVector2.ToDb(entity.Position),
                Direction = DbVector2.ToDb(entity.Direction),
                Speed = entity.Speed,
                SequenceId = entity.SequenceId
            };
        }
    }
}