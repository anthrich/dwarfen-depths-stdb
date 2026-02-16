namespace SharedPhysics
{
    public interface ITerrain
    {
        float? GetGroundHeight(Vector2 xzPoint);
        Triangle? GetTriangle(Vector2 xzPoint);
    }
}
