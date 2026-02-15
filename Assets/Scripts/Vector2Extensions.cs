using SpacetimeDB.Types;
using UnityEngine;

public static class Vector2Extensions
{
    public static Vector2 ToUnityVector2(this DbVector2 vec)
    {
        return new Vector2(vec.X, vec.Y);
    }

    public static SharedPhysics.Vector2 ToSharedPhysicsV2(this Vector3 vec)
    {
        return new SharedPhysics.Vector2(vec.x, vec.z);
    }

    public static SharedPhysics.Vector2 ToSharedPhysicsV2(this Vector2 vec)
    {
        return new SharedPhysics.Vector2(vec.x, vec.y);
    }

    public static SharedPhysics.Vector3 ToSharedPhysicsV3(this Vector3 vec)
    {
        return new SharedPhysics.Vector3(vec.x, vec.y, vec.z);
    }

    public static Vector3 ToGamePosition(this SharedPhysics.Vector3 vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Vector3 ToGamePosition(this DbVector3 vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Vector3 ToGameDirection(this DbVector2 vec)
    {
        return new Vector3(vec.X, 0, vec.Y);
    }

    public static bool ApproximatesTo(this Vector2 vec, Vector2 target, float precision = 0.1f)
    {
        return Mathf.Abs(vec.x - target.x) < precision && Mathf.Abs(vec.y - target.y) < precision;
    }

    public static DbVector2 ToDbVector2(this Vector2 vec)
    {
        return new DbVector2(vec.x, vec.y);
    }
}
