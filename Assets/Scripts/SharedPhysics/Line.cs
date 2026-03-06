using System;

namespace SharedPhysics
{
    [Serializable]
    public struct Line
    {
        public Vector2 Start;
        public Vector2 End;
        // Height above which this line does not block.
        // 0 (default) = always block. Positive = only block when entity.Y < SurfaceY.
        public float SurfaceY;

        public Line(Vector2 start, Vector2 end, float surfaceY = 0f)
        {
            Start = start;
            End = end;
            SurfaceY = surfaceY;
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