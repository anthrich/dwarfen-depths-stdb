using SpacetimeDB.Types;
using UnityEngine;

public static class Vector2Extensions
{
    public static Vector2 ToUnityVector2(this DbVector2 vec)
    {
        return new Vector2(vec.X, vec.Y);
    }
    
    public static Vector3 ToGamePosition(this DbVector2 vec, float yPos)
    {
        return new Vector3(vec.X, yPos, vec.Y);
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