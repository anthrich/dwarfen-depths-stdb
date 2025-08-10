
using SpacetimeDB;

public static partial class Module
{
    [Table(Name = "Line")]
    public partial struct Line
    {
        [Unique, AutoInc] public uint Id;
        public DbVector2 Start;
        public DbVector2 End;

        public Line(DbVector2 start, DbVector2 end)
        {
            Start = start;
            End = end;
        }

        public static SharedPhysics.Line ToPhysics(Line line)
        {
            return new SharedPhysics.Line(
                DbVector2.ToPhysics(line.Start),
                DbVector2.ToPhysics(line.End)
            );
        }
    }
}