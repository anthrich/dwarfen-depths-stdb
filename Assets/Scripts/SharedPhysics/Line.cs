namespace SharedPhysics
{
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
            return Vector2.Normalized(new Vector2(-direction.Y, direction.X));
        }

        public static Vector2 GlideAlong(Line line, Vector2 direction)
        {
            var normal = GetNormal(line);
            var dotProduct = Vector2.Dot(direction, normal);
            return direction - normal * dotProduct;
        }
    }
}