using SharedPhysics;

[SpacetimeDB.Type]
public partial struct DbVector3
{
    public float X;
    public float Y;
    public float Z;

    public DbVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 ToPhysics(DbVector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    public static DbVector3 ToDb(Vector3 v)
    {
        return new DbVector3(v.X, v.Y, v.Z);
    }
}
