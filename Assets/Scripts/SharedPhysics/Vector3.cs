using System;

namespace SharedPhysics
{
    [Serializable]
    public struct Vector3 : IEquatable<Vector3>
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float SqrMagnitude => X * X + Y * Y + Z * Z;
        public float GetMagnitude() => MathF.Sqrt(SqrMagnitude);
        public readonly Vector3 Normalized() => Normalize(this);

        public Vector2 ToXZ() => new Vector2(X, Z);

        public static Vector3 FromXZ(Vector2 xz, float y) => new Vector3(xz.X, y, xz.Y);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
        public static Vector3 operator /(Vector3 a, float b) => new(a.X / b, a.Y / b, a.Z / b);

        public static bool operator ==(Vector3 a, Vector3 b) =>
            Math.Abs(a.X - b.X) < 0.001 && Math.Abs(a.Y - b.Y) < 0.001 && Math.Abs(a.Z - b.Z) < 0.001;

        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public static readonly Vector3 Zero = new(0, 0, 0);
        public static readonly Vector3 Up = new(0, 1, 0);

        public static Vector3 Normalize(Vector3 vector)
        {
            var magnitude = vector.GetMagnitude();
            return magnitude > 0 ? vector / magnitude : vector;
        }

        public static float Distance(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vector3 Cross(Vector3 a, Vector3 b) => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;

        public override string ToString()
        {
            return $"{{{X}, {Y}, {Z}}}";
        }

        public bool Equals(Vector3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }
}
