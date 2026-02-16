using System;
using System.Collections.Generic;

namespace SharedPhysics
{
    public class TerrainGrid : ITerrain
    {
        private readonly Triangle[] _triangles;
        private readonly float _cellSize;
        private readonly float _minX, _minZ;
        private readonly int _gridWidth, _gridDepth;
        private readonly List<int>[] _cells;

        public TerrainGrid(Triangle[] triangles, float cellSize = 10f)
        {
            _triangles = triangles;
            _cellSize = cellSize;

            if (triangles.Length == 0)
            {
                _minX = _minZ = 0;
                _gridWidth = _gridDepth = 0;
                _cells = Array.Empty<List<int>>();
                return;
            }

            float maxX = float.MinValue, maxZ = float.MinValue;
            _minX = float.MaxValue;
            _minZ = float.MaxValue;

            foreach (var tri in triangles)
            {
                _minX = MathF.Min(_minX, MathF.Min(tri.V0.X, MathF.Min(tri.V1.X, tri.V2.X)));
                _minZ = MathF.Min(_minZ, MathF.Min(tri.V0.Z, MathF.Min(tri.V1.Z, tri.V2.Z)));
                maxX = MathF.Max(maxX, MathF.Max(tri.V0.X, MathF.Max(tri.V1.X, tri.V2.X)));
                maxZ = MathF.Max(maxZ, MathF.Max(tri.V0.Z, MathF.Max(tri.V1.Z, tri.V2.Z)));
            }

            _gridWidth = (int)MathF.Ceiling((maxX - _minX) / _cellSize) + 1;
            _gridDepth = (int)MathF.Ceiling((maxZ - _minZ) / _cellSize) + 1;
            _cells = new List<int>[_gridWidth * _gridDepth];

            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = new List<int>();

            for (int i = 0; i < triangles.Length; i++)
            {
                var tri = triangles[i];
                float triMinX = MathF.Min(tri.V0.X, MathF.Min(tri.V1.X, tri.V2.X));
                float triMinZ = MathF.Min(tri.V0.Z, MathF.Min(tri.V1.Z, tri.V2.Z));
                float triMaxX = MathF.Max(tri.V0.X, MathF.Max(tri.V1.X, tri.V2.X));
                float triMaxZ = MathF.Max(tri.V0.Z, MathF.Max(tri.V1.Z, tri.V2.Z));

                int cellMinX = CellX(triMinX);
                int cellMaxX = CellX(triMaxX);
                int cellMinZ = CellZ(triMinZ);
                int cellMaxZ = CellZ(triMaxZ);

                for (int cx = cellMinX; cx <= cellMaxX; cx++)
                    for (int cz = cellMinZ; cz <= cellMaxZ; cz++)
                        _cells[cx + cz * _gridWidth].Add(i);
            }
        }

        private int CellX(float x) => Math.Clamp((int)((x - _minX) / _cellSize), 0, _gridWidth - 1);
        private int CellZ(float z) => Math.Clamp((int)((z - _minZ) / _cellSize), 0, _gridDepth - 1);

        public int FindTriangleIndex(Vector2 xzPoint)
        {
            if (_cells.Length == 0) return -1;
            int cx = CellX(xzPoint.X);
            int cz = CellZ(xzPoint.Y);
            var cell = _cells[cx + cz * _gridWidth];

            foreach (var idx in cell)
            {
                if (Triangle.ContainsXZ(_triangles[idx], xzPoint))
                    return idx;
            }
            return -1;
        }

        public float? GetGroundHeight(Vector2 xzPoint)
        {
            int idx = FindTriangleIndex(xzPoint);
            if (idx < 0) return null;
            return Triangle.InterpolateHeight(_triangles[idx], xzPoint);
        }

        public Triangle? GetTriangle(Vector2 xzPoint)
        {
            int idx = FindTriangleIndex(xzPoint);
            if (idx < 0) return null;
            return _triangles[idx];
        }
    }
}
