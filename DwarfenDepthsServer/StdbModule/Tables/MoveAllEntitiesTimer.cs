using SpacetimeDB;

public static partial class Module
{
    [Table(
        Name = "moveAllEntitiesTimer",
        Public = false,
        Scheduled = nameof(MoveAllEntities),
        ScheduledAt = nameof(ScheduledAt)
    )]
    public partial struct MoveAllEntitiesTimer
    {
        [PrimaryKey, AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }
}