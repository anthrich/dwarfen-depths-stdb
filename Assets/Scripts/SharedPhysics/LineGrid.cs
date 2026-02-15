using System;
using System.Collections.Generic;

namespace SharedPhysics
{
    public class LineGrid
    {
        private readonly Line[] _lines;
        private readonly float _cellSize;
        private readonly float _minX, _minZ;
        private readonly int _gridWidth, _gridDepth;
        private readonly List<int>[] _cells;
        private readonly HashSet<int> _seenIndices = new();

        public Line[] Lines => _lines;

        public LineGrid(Line[] lines, float cellSize = 10f)
        {
            _lines = lines;
            _cellSize = cellSize;

            if (lines.Length == 0)
            {
                _minX = _minZ = 0;
                _gridWidth = _gridDepth = 0;
                _cells = Array.Empty<List<int>>();
                return;
            }

            float maxX = float.MinValue, maxZ = float.MinValue;
            _minX = float.MaxValue;
            _minZ = float.MaxValue;

            foreach (var line in lines)
            {
                _minX = MathF.Min(_minX, MathF.Min(line.Start.X, line.End.X));
                _minZ = MathF.Min(_minZ, MathF.Min(line.Start.Y, line.End.Y));
                maxX = MathF.Max(maxX, MathF.Max(line.Start.X, line.End.X));
                maxZ = MathF.Max(maxZ, MathF.Max(line.Start.Y, line.End.Y));
            }

            _gridWidth = (int)MathF.Ceiling((maxX - _minX) / _cellSize) + 1;
            _gridDepth = (int)MathF.Ceiling((maxZ - _minZ) / _cellSize) + 1;
            _cells = new List<int>[_gridWidth * _gridDepth];

            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = new List<int>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                int cellMinX = CellX(MathF.Min(line.Start.X, line.End.X));
                int cellMaxX = CellX(MathF.Max(line.Start.X, line.End.X));
                int cellMinZ = CellZ(MathF.Min(line.Start.Y, line.End.Y));
                int cellMaxZ = CellZ(MathF.Max(line.Start.Y, line.End.Y));

                for (int cx = cellMinX; cx <= cellMaxX; cx++)
                    for (int cz = cellMinZ; cz <= cellMaxZ; cz++)
                        _cells[cx + cz * _gridWidth].Add(i);
            }
        }

        private int CellX(float x) => Math.Clamp((int)((x - _minX) / _cellSize), 0, Math.Max(0, _gridWidth - 1));
        private int CellZ(float z) => Math.Clamp((int)((z - _minZ) / _cellSize), 0, Math.Max(0, _gridDepth - 1));

        public void GetNearbyLines(BoundingBox movementBounds, List<Line> result)
        {
            result.Clear();
            _seenIndices.Clear();
            if (_cells.Length == 0) return;

            int cellMinX = CellX(movementBounds.MinX);
            int cellMaxX = CellX(movementBounds.MaxX);
            int cellMinZ = CellZ(movementBounds.MinY);
            int cellMaxZ = CellZ(movementBounds.MaxY);

            for (int cx = cellMinX; cx <= cellMaxX; cx++)
            {
                for (int cz = cellMinZ; cz <= cellMaxZ; cz++)
                {
                    var cell = _cells[cx + cz * _gridWidth];
                    foreach (var idx in cell)
                    {
                        if (!_seenIndices.Add(idx)) continue;
                        var line = _lines[idx];
                        var lineBounds = BoundingBox.FromLine(line);
                        if (lineBounds.Overlaps(movementBounds))
                        {
                            result.Add(line);
                        }
                    }
                }
            }
        }
    }
}
