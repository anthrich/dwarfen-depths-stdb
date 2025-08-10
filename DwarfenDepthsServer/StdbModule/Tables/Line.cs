
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
        
        public static DbVector2 GetNormal(Line wall)
        {
            var direction = wall.End - wall.Start;
            return DbVector2.Normalized(new DbVector2(-direction.Y, direction.X));
        }

        public static DbVector2 GlideAlong(Line line, DbVector2 direction)
        {
            var normal = GetNormal(line);
            var dotProduct = DbVector2.Dot(direction, normal);
            return direction - normal * dotProduct;
        }
    }
}