using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "Entity", Public = true)]
    public partial struct Entity
    {
        [PrimaryKey, AutoInc]
        public uint EntityId;
        public float Speed;
        public DbVector3 Position;
        public DbVector2 Direction;
        public float Rotation;
        public ulong SequenceId;
        public Faction Allegiance;
        public uint TargetEntityId;
        public float VerticalVelocity;
        public bool IsGrounded;

        public static SharedPhysics.Entity ToPhysics(Entity entity)
        {
            return new SharedPhysics.Entity
            {
                Id = entity.EntityId,
                Position = DbVector3.ToPhysics(entity.Position),
                Direction = DbVector2.ToPhysics(entity.Direction),
                Speed = entity.Speed,
                SequenceId = entity.SequenceId,
                Rotation = entity.Rotation,
                VerticalVelocity = entity.VerticalVelocity,
                IsGrounded = entity.IsGrounded,
            };
        }

        public static Entity FromPhysics(SharedPhysics.Entity entity)
        {
            return new Entity
            {
                EntityId = entity.Id,
                Position = DbVector3.ToDb(entity.Position),
                Direction = DbVector2.ToDb(entity.Direction),
                Speed = entity.Speed,
                SequenceId = entity.SequenceId,
                Rotation = entity.Rotation,
                VerticalVelocity = entity.VerticalVelocity,
                IsGrounded = entity.IsGrounded,
            };
        }
    }
}
