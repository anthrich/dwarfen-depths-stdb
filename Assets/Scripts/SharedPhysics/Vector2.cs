using System;

namespace SharedPhysics
{
    [Serializable]
    public struct Vector2 : IEquatable<Vector2>
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
        public readonly Vector2 Normalized() => Normalize(this);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float b) => new(a.X * b, a.Y * b);
        public static Vector2 operator /(Vector2 a, float b) => new(a.X / b, a.Y / b);
        public static bool operator ==(Vector2 a, Vector2 b) =>
            Math.Abs(a.X - b.X) < 0.001 && Math.Abs(a.Y - b.Y) < 0.001;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);

        public static readonly Vector2 Zero = new(0, 0);

        public static Vector2 Normalize(Vector2 vector)
        {
            var magnitude = vector.GetMagnitude();
            return magnitude > 0 ? vector / magnitude : vector;
        }
        
        public static float Distance(Vector2 a, Vector2 b)
        {
            var num1 = a.X - b.X;
            var num2 = a.Y - b.Y;
            return (float) Math.Sqrt(num1 * (double) num1 + num2 * (double) num2);
        }
    
        public static float Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public override string ToString()
        {
            return $"{{{X}, {Y}}}";
        }

        public bool Equals(Vector2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}