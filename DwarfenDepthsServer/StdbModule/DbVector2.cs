using SharedPhysics;

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

    public static Vector2 ToPhysics(DbVector2 dbVector2)
    {
        return new Vector2(dbVector2.X, dbVector2.Y);
    }
    
    public static DbVector2 ToDb(Vector2 dbVector2)
    {
        return new DbVector2(dbVector2.X, dbVector2.Y);
    }
}