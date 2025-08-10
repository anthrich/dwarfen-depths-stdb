using System;

namespace SharedPhysics
{
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
    
        private bool PositionIsInside(Vector2 position)
        {
            return MinX <= position.X &&
                   MaxX > position.X &&
                   MinY <= position.Y &&
                   MaxY > position.Y;
        }
    
        public bool Overlaps(BoundingBox other)
        {
            return MinX <= other.MaxX && MaxX >= other.MinX &&
                   MinY <= other.MaxY && MaxY >= other.MinY;
        }
    }
}