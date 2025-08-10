[SpacetimeDB.Type]
public partial struct DbVector2
{
    public float X;
    public float Y;
    
    public DbVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float SqrMagnitude => X * X + Y * Y;
    public float GetMagnitude() => MathF.Sqrt(SqrMagnitude);

    public static DbVector2 operator +(DbVector2 a, DbVector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static DbVector2 operator -(DbVector2 a, DbVector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static DbVector2 operator *(DbVector2 a, float b) => new(a.X * b, a.Y * b);
    public static DbVector2 operator /(DbVector2 a, float b) => new(a.X / b, a.Y / b);
    
    public static DbVector2 Normalized(DbVector2 vector)
    {
        var magnitude = vector.GetMagnitude();
        return magnitude > 0 ? vector / magnitude : vector;
    }
    
    public static float Dot(DbVector2 a, DbVector2 b)
    {
        return a.X * b.X + a.Y * b.Y;
    }
}