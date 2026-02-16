using System;

namespace SharedPhysics
{
    public class Heightmap : ITerrain
    {
        private readonly float[] _heights;
        private readonly int _resolution;
        private readonly float _originX;
        private readonly float _originZ;
        private readonly float _sizeX;
        private readonly float _sizeZ;
        private readonly float _cellSizeX;
        private readonly float _cellSizeZ;

        public Heightmap(float[] heights, int resolution,
                         float originX, float originZ,
                         float sizeX, float sizeZ)
        {
            if (heights.Length != resolution * resolution)
                throw new ArgumentException(
                    $"Expected {resolution * resolution} heights, got {heights.Length}");

            _heights = heights;
            _resolution = resolution;
            _originX = originX;
            _originZ = originZ;
            _sizeX = sizeX;
            _sizeZ = sizeZ;
            _cellSizeX = _sizeX / (_resolution - 1);
            _cellSizeZ = _sizeZ / (_resolution - 1);
        }

        private bool TryGetCell(Vector2 xzPoint, out int gridX, out int gridZ, out float fracX, out float fracZ)
        {
            float localX = (xzPoint.X - _originX) / _cellSizeX;
            float localZ = (xzPoint.Y - _originZ) / _cellSizeZ;

            if (localX < 0 || localZ < 0 ||
                localX > _resolution - 1 || localZ > _resolution - 1)
            {
                gridX = gridZ = 0;
                fracX = fracZ = 0;
                return false;
            }

            gridX = Math.Min((int)localX, _resolution - 2);
            gridZ = Math.Min((int)localZ, _resolution - 2);
            fracX = localX - gridX;
            fracZ = localZ - gridZ;
            return true;
        }

        public float? GetGroundHeight(Vector2 xzPoint)
        {
            if (!TryGetCell(xzPoint, out int gx, out int gz, out float fx, out float fz))
                return null;

            float topLeft = _heights[gz * _resolution + gx];
            float topRight = _heights[gz * _resolution + gx + 1];
            float bottomLeft = _heights[(gz + 1) * _resolution + gx];
            float bottomRight = _heights[(gz + 1) * _resolution + gx + 1];

            // Triangle split: diagonal from bottomLeft to topRight
            // Matches MapExporter's quad triangulation:
            //   Triangle 1: topLeft, bottomLeft, topRight     (fx + fz <= 1)
            //   Triangle 2: topRight, bottomLeft, bottomRight (fx + fz > 1)
            if (fx + fz <= 1.0f)
            {
                return topLeft + fx * (topRight - topLeft) + fz * (bottomLeft - topLeft);
            }
            else
            {
                return bottomRight + (1f - fx) * (bottomLeft - bottomRight)
                                   + (1f - fz) * (topRight - bottomRight);
            }
        }

        public Triangle? GetTriangle(Vector2 xzPoint)
        {
            if (!TryGetCell(xzPoint, out int gx, out int gz, out float fx, out float fz))
                return null;

            float wx0 = _originX + gx * _cellSizeX;
            float wx1 = _originX + (gx + 1) * _cellSizeX;
            float wz0 = _originZ + gz * _cellSizeZ;
            float wz1 = _originZ + (gz + 1) * _cellSizeZ;

            float topLeft = _heights[gz * _resolution + gx];
            float topRight = _heights[gz * _resolution + gx + 1];
            float bottomLeft = _heights[(gz + 1) * _resolution + gx];
            float bottomRight = _heights[(gz + 1) * _resolution + gx + 1];

            if (fx + fz <= 1.0f)
            {
                return new Triangle(
                    new Vector3(wx0, topLeft, wz0),
                    new Vector3(wx0, bottomLeft, wz1),
                    new Vector3(wx1, topRight, wz0)
                );
            }
            else
            {
                return new Triangle(
                    new Vector3(wx1, topRight, wz0),
                    new Vector3(wx0, bottomLeft, wz1),
                    new Vector3(wx1, bottomRight, wz1)
                );
            }
        }
    }
}
