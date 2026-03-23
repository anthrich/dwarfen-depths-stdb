using System;

namespace SharedPhysics
{
    public struct BoundingBox
    {
        public float MinX, MinY, MaxX, MaxY;
    
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