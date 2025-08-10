using System;

namespace SharedPhysics
{
    public partial struct Vector2
    {
        public float X;
        public float Y;
    
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float SqrMagnitude => X * X + Y * Y;
        public float GetMagnitude() => MathF.Sqrt(SqrMagnitude);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float b) => new(a.X * b, a.Y * b);
        public static Vector2 operator /(Vector2 a, float b) => new(a.X / b, a.Y / b);
    
        public static Vector2 Normalized(Vector2 vector)
        {
            var magnitude = vector.GetMagnitude();
            return magnitude > 0 ? vector / magnitude : vector;
        }
    
        public static float Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }
    }
}