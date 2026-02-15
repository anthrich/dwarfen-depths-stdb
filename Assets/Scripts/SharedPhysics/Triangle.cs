using System;

namespace SharedPhysics
{
    [Serializable]
    public struct Triangle
    {
        public Vector3 V0;
        public Vector3 V1;
        public Vector3 V2;

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            V0 = v0;
            V1 = v1;
            V2 = v2;
        }

        public static Vector3 GetNormal(Triangle tri)
        {
            var edge1 = tri.V1 - tri.V0;
            var edge2 = tri.V2 - tri.V0;
            return Vector3.Cross(edge1, edge2).Normalized();
        }

        public static float GetSlopeAngle(Triangle tri)
        {
            var normal = GetNormal(tri);
            var dot = Vector3.Dot(normal, Vector3.Up);
            dot = Math.Clamp(dot, -1f, 1f);
            return MathF.Acos(dot) * (180f / MathF.PI);
        }

        public static (float u, float v, float w) GetBarycentric(Triangle tri, Vector2 point)
        {
            var a = new Vector2(tri.V0.X, tri.V0.Z);
            var b = new Vector2(tri.V1.X, tri.V1.Z);
            var c = new Vector2(tri.V2.X, tri.V2.Z);

            var v0 = b - a;
            var v1 = c - a;
            var v2 = point - a;

            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;
            if (MathF.Abs(denom) < 1e-8f)
                return (-1, -1, -1);

            float baryV = (d11 * d20 - d01 * d21) / denom;
            float baryW = (d00 * d21 - d01 * d20) / denom;
            float baryU = 1f - baryV - baryW;

            return (baryU, baryV, baryW);
        }

        public static bool ContainsXZ(Triangle tri, Vector2 point)
        {
            var (u, v, w) = GetBarycentric(tri, point);
            const float epsilon = -0.001f;
            return u >= epsilon && v >= epsilon && w >= epsilon;
        }

        public static float InterpolateHeight(Triangle tri, Vector2 point)
        {
            var (u, v, w) = GetBarycentric(tri, point);
            return u * tri.V0.Y + v * tri.V1.Y + w * tri.V2.Y;
        }
    }
}
