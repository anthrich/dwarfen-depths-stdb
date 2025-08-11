using System;

namespace SharedPhysics
{
    [Serializable]
    public struct Line
    {
        public Vector2 Start;
        public Vector2 End;

        public Line(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
        
        public static Vector2 GetNormal(Line wall)
        {
            var direction = wall.End - wall.Start;
            return new Vector2(-direction.Y, direction.X).Normalized();
        }

        public static Vector2 GlideAlong(Line line, Vector2 direction)
        {
            var normal = GetNormal(line);
            var dotProduct = Vector2.Dot(direction, normal);
            return direction - normal * dotProduct;
        }
    }
}