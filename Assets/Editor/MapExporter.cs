using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class MapExporter
{
    private const float EdgeEpsilon = 0.01f;
    private const float MaxSlopeAngle = 45f;
    private static readonly string OutputPath = Path.Combine(Application.dataPath, "Scripts", "SharedPhysics", "MapData.cs");

    [MenuItem("Tools/Export Map Data")]
    public static void ExportMapData()
    {
        var terrainMeshes = Object.FindObjectsByType<TerrainMesh>(FindObjectsSortMode.None);
        if (terrainMeshes.Length == 0)
        {
            EditorUtility.DisplayDialog("Export Failed", "No TerrainMesh components found in the scene.", "OK");
            return;
        }

        var spawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        if (spawnPoints.Length == 0)
        {
            EditorUtility.DisplayDialog("Export Failed", "No SpawnPoint components found in the scene.", "OK");
            return;
        }

        var allEdges = new List<Edge2D>();
        var allTriangles = new List<Triangle3D>();

        foreach (var terrain in terrainMeshes)
        {
            var meshFilter = terrain.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"TerrainMesh on '{terrain.name}' has no MeshFilter or mesh. Skipping.");
                continue;
            }

            var boundaryEdges = ExtractBoundaryEdges(meshFilter.sharedMesh, terrain.transform);
            allEdges.AddRange(boundaryEdges);

            var triangles = ExtractTriangles(meshFilter.sharedMesh, terrain.transform);
            allTriangles.AddRange(triangles);

            var slopeEdges = ExtractSlopeTransitionEdges(meshFilter.sharedMesh, terrain.transform, MaxSlopeAngle);
            allEdges.AddRange(slopeEdges);
        }

        var deduplicatedEdges = DeduplicateSharedEdges(allEdges);
        var deduplicatedTriangles = DeduplicateSharedTriangles(allTriangles);
        Debug.Log($"Extracted {allEdges.Count} boundary edges, {deduplicatedEdges.Count} after deduplication.");
        Debug.Log($"Extracted {allTriangles.Count} triangles, {deduplicatedTriangles.Count} after deduplication.");

        var spawnPos = spawnPoints[0].transform.position;

        var mapName = EditorSceneManager.GetActiveScene().name;

        var code = GenerateMapDataClass(mapName, deduplicatedEdges, deduplicatedTriangles, spawnPos);
        File.WriteAllText(OutputPath, code);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Export Complete",
            $"Exported map '{mapName}' ({deduplicatedEdges.Count} collision lines, {deduplicatedTriangles.Count} triangles) to:\n{OutputPath}",
            "OK"
        );
    }

    private static List<Triangle3D> ExtractTriangles(Mesh mesh, Transform transform)
    {
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        var result = new List<Triangle3D>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            var worldV0 = transform.TransformPoint(vertices[triangles[i]]);
            var worldV1 = transform.TransformPoint(vertices[triangles[i + 1]]);
            var worldV2 = transform.TransformPoint(vertices[triangles[i + 2]]);

            result.Add(new Triangle3D(worldV0, worldV1, worldV2));
        }

        return result;
    }

    private static List<Edge2D> ExtractBoundaryEdges(Mesh mesh, Transform transform)
    {
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        var edgeCounts = new Dictionary<(int, int), int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            var v0 = triangles[i];
            var v1 = triangles[i + 1];
            var v2 = triangles[i + 2];

            CountEdge(edgeCounts, v0, v1);
            CountEdge(edgeCounts, v1, v2);
            CountEdge(edgeCounts, v2, v0);
        }

        var boundaryEdges = new List<Edge2D>();
        foreach (var kvp in edgeCounts)
        {
            if (kvp.Value != 1) continue;

            var worldA = transform.TransformPoint(vertices[kvp.Key.Item1]);
            var worldB = transform.TransformPoint(vertices[kvp.Key.Item2]);

            boundaryEdges.Add(new Edge2D(
                new Vector2(worldA.x, worldA.z),
                new Vector2(worldB.x, worldB.z)
            ));
        }

        return boundaryEdges;
    }

    private static List<Edge2D> ExtractSlopeTransitionEdges(Mesh mesh, Transform transform, float maxSlopeAngle)
    {
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        var edgeTriangles = new Dictionary<(int, int), List<int>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int triIdx = i / 3;
            AddEdgeTri(edgeTriangles, triangles[i], triangles[i + 1], triIdx);
            AddEdgeTri(edgeTriangles, triangles[i + 1], triangles[i + 2], triIdx);
            AddEdgeTri(edgeTriangles, triangles[i + 2], triangles[i], triIdx);
        }

        var result = new List<Edge2D>();
        foreach (var kvp in edgeTriangles)
        {
            if (kvp.Value.Count != 2) continue;

            bool walkableA = IsTriangleWalkable(kvp.Value[0], triangles, vertices, transform, maxSlopeAngle);
            bool walkableB = IsTriangleWalkable(kvp.Value[1], triangles, vertices, transform, maxSlopeAngle);

            if (walkableA != walkableB)
            {
                var worldA = transform.TransformPoint(vertices[kvp.Key.Item1]);
                var worldB = transform.TransformPoint(vertices[kvp.Key.Item2]);
                result.Add(new Edge2D(
                    new Vector2(worldA.x, worldA.z),
                    new Vector2(worldB.x, worldB.z)
                ));
            }
        }
        return result;
    }

    private static void AddEdgeTri(Dictionary<(int, int), List<int>> edgeTriangles, int a, int b, int triIdx)
    {
        var key = a < b ? (a, b) : (b, a);
        if (!edgeTriangles.TryGetValue(key, out var list))
        {
            list = new List<int>();
            edgeTriangles[key] = list;
        }
        list.Add(triIdx);
    }

    private static bool IsTriangleWalkable(int triIdx, int[] triangles, Vector3[] vertices, Transform transform, float maxSlopeAngle)
    {
        int i = triIdx * 3;
        var worldV0 = transform.TransformPoint(vertices[triangles[i]]);
        var worldV1 = transform.TransformPoint(vertices[triangles[i + 1]]);
        var worldV2 = transform.TransformPoint(vertices[triangles[i + 2]]);

        var edge1 = worldV1 - worldV0;
        var edge2 = worldV2 - worldV0;
        var normal = Vector3.Cross(edge1, edge2).normalized;

        var angle = Vector3.Angle(normal, Vector3.up);
        // Check both orientations (normal could point up or down depending on winding)
        if (angle > 90f) angle = 180f - angle;
        return angle <= maxSlopeAngle;
    }

    private static void CountEdge(Dictionary<(int, int), int> edgeCounts, int a, int b)
    {
        var key = a < b ? (a, b) : (b, a);
        edgeCounts.TryGetValue(key, out var count);
        edgeCounts[key] = count + 1;
    }

    private static List<Edge2D> DeduplicateSharedEdges(List<Edge2D> edges)
    {
        var result = new List<Edge2D>();
        var removed = new bool[edges.Count];

        for (int i = 0; i < edges.Count; i++)
        {
            if (removed[i]) continue;

            bool isDuplicate = false;
            for (int j = i + 1; j < edges.Count; j++)
            {
                if (removed[j]) continue;
                if (!EdgesMatch(edges[i], edges[j])) continue;

                removed[i] = true;
                removed[j] = true;
                isDuplicate = true;
                break;
            }

            if (!isDuplicate)
            {
                result.Add(edges[i]);
            }
        }

        return result;
    }

    private static List<Triangle3D> DeduplicateSharedTriangles(List<Triangle3D> triangles)
    {
        var result = new List<Triangle3D>();
        var removed = new bool[triangles.Count];

        for (int i = 0; i < triangles.Count; i++)
        {
            if (removed[i]) continue;

            bool isDuplicate = false;
            for (int j = i + 1; j < triangles.Count; j++)
            {
                if (removed[j]) continue;
                if (!TrianglesMatch(triangles[i], triangles[j])) continue;

                removed[i] = true;
                removed[j] = true;
                isDuplicate = true;
                break;
            }

            if (!isDuplicate)
            {
                result.Add(triangles[i]);
            }
        }

        return result;
    }

    private static bool TrianglesMatch(Triangle3D a, Triangle3D b)
    {
        var aVerts = new[] { a.V0, a.V1, a.V2 };
        var bVerts = new[] { b.V0, b.V1, b.V2 };

        // Check if all vertices of a match some vertex in b
        foreach (var av in aVerts)
        {
            bool found = false;
            foreach (var bv in bVerts)
            {
                if (ApproxEqual3D(av, bv))
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }

    private static bool EdgesMatch(Edge2D a, Edge2D b)
    {
        return (ApproxEqual(a.Start, b.Start) && ApproxEqual(a.End, b.End)) ||
               (ApproxEqual(a.Start, b.End) && ApproxEqual(a.End, b.Start));
    }

    private static bool ApproxEqual(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) < EdgeEpsilon && Mathf.Abs(a.y - b.y) < EdgeEpsilon;
    }

    private static bool ApproxEqual3D(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) < EdgeEpsilon &&
               Mathf.Abs(a.y - b.y) < EdgeEpsilon &&
               Mathf.Abs(a.z - b.z) < EdgeEpsilon;
    }

    private static string GenerateMapDataClass(string mapName, List<Edge2D> lines, List<Triangle3D> triangles, Vector3 spawnPosition)
    {
        var code = new StringBuilder();
        code.AppendLine("// Auto-generated by MapExporter. Do not edit.");
        code.AppendLine("namespace SharedPhysics");
        code.AppendLine("{");
        code.AppendLine("    public static class MapData");
        code.AppendLine("    {");

        code.AppendLine("        public static MapDefinition GetMap(string name) => name switch");
        code.AppendLine("        {");
        code.AppendLine($"            \"{mapName}\" => {mapName},");
        code.AppendLine("            _ => throw new System.ArgumentException($\"Unknown map: {name}\")");
        code.AppendLine("        };");

        code.AppendLine();
        code.AppendLine($"        private static readonly MapDefinition {mapName} = new MapDefinition(");
        code.AppendLine($"            \"{mapName}\",");

        // Lines array
        code.AppendLine("            new Line[]");
        code.AppendLine("            {");
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var comma = i < lines.Count - 1 ? "," : "";
            code.AppendLine($"                new Line(new Vector2({F(line.Start.x)}, {F(line.Start.y)}), new Vector2({F(line.End.x)}, {F(line.End.y)})){comma}");
        }
        code.AppendLine("            },");

        // Triangles array
        code.AppendLine("            new Triangle[]");
        code.AppendLine("            {");
        for (int i = 0; i < triangles.Count; i++)
        {
            var tri = triangles[i];
            var comma = i < triangles.Count - 1 ? "," : "";
            code.AppendLine($"                new Triangle(new Vector3({F(tri.V0.x)}, {F(tri.V0.y)}, {F(tri.V0.z)}), new Vector3({F(tri.V1.x)}, {F(tri.V1.y)}, {F(tri.V1.z)}), new Vector3({F(tri.V2.x)}, {F(tri.V2.y)}, {F(tri.V2.z)})){comma}");
        }
        code.AppendLine("            },");

        // Spawn position as Vector3
        code.AppendLine($"            new Vector3({F(spawnPosition.x)}, {F(spawnPosition.y)}, {F(spawnPosition.z)})");
        code.AppendLine("        );");

        code.AppendLine("    }");
        code.AppendLine("}");

        return code.ToString();
    }

    private static string F(float value)
    {
        return value.ToString("G", CultureInfo.InvariantCulture) + "f";
    }

    private struct Edge2D
    {
        public Vector2 Start;
        public Vector2 End;

        public Edge2D(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
    }

    private struct Triangle3D
    {
        public Vector3 V0;
        public Vector3 V1;
        public Vector3 V2;

        public Triangle3D(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            V0 = v0;
            V1 = v1;
            V2 = v2;
        }
    }
}
