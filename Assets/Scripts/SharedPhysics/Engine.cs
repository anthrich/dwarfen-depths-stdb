using System;
using System.Linq;

namespace SharedPhysics
{
    public class Engine
    {
        public static Vector2? GetIntersection(Line line1, Line line2)
        {
            var p1 = line1.Start;
            var p2 = line1.End;
            var p3 = line2.Start;
            var p4 = line2.End;
    
            var d1 = p2 - p1; // Direction of line 1
            var d2 = p4 - p3; // Direction of line 2
    
            var cross = d1.X * d2.Y - d1.Y * d2.X;
    
            if (Math.Abs(cross) < 0.0001f)
                return default; // Parallel lines
    
            var t = ((p3.X - p1.X) * d2.Y - (p3.Y - p1.Y) * d2.X) / cross;
            var u = ((p3.X - p1.X) * d1.Y - (p3.Y - p1.Y) * d1.X) / cross;
    
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                return p1 + d1 * t;
            }
    
            return default;
        }

        public static Line[] GetNearbyLines(Line line1, Line[] allLines)
        {
            var movementBounds = BoundingBox.FromLine(line1);
            return allLines.Where(line => BoundingBox.FromLine(line).Overlaps(movementBounds)).ToArray();
        }
    
        public struct BoundingBox
        {
            public float MinX, MinY, MaxX, MaxY;
    
            public static BoundingBox FromLine(Line line)
            {
                return new BoundingBox
                {
                    MinX = Math.Min(line.Start.X, line.End.X),
                    MinY = Math.Min(line.Start.Y, line.End.Y),
                    MaxX = Math.Max(line.Start.X, line.End.X),
                    MaxY = Math.Max(line.Start.Y, line.End.Y)
                };
            }
    
            public bool Overlaps(BoundingBox other)
            {
                return MinX <= other.MaxX && MaxX >= other.MinX &&
                       MinY <= other.MaxY && MaxY >= other.MinY;
            }
        }
    }
}