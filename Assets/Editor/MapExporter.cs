using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapExporter
{
    // Minimum dot product with world-up for a face to be considered walkable.
    private const float WalkableNormalThreshold = 0.5f;
    private static readonly string OutputDir = Path.Combine(Application.dataPath, "Scripts", "SharedPhysics");

    // ── Raw data gathered on the Unity main thread ───────────────────────────

    private struct RawTri { public Vector3 V0, V1, V2; }

    private struct RawSource
    {
        public List<RawTri> TerrainTris;        // sampled from Unity Terrain
        public List<List<RawTri>> MeshTris;     // per-object walkable (upward) tris
        public Vector3 SpawnPos;
        public string MapName;
    }

    // ── Processed output ─────────────────────────────────────────────────────

    private struct WallLine { public Vector2 Start, End; public float SurfaceY; }
    private struct Tri3 { public Vector3 V0, V1, V2; }

    private struct ProcessedMap
    {
        public List<WallLine> Walls;
        public List<Tri3> Tris;
        public Vector3 SpawnPos;
        public string MapName;
    }

    // ── Entry point ──────────────────────────────────────────────────────────

    [MenuItem("Tools/Export Map Data")]
    public static async void ExportMapData()
    {
        var raw = GatherRawSources();
        if (raw == null) return;

        EditorUtility.DisplayProgressBar("Exporting Map", "Processing geometry…", 0.2f);
        var progress = new Progress<string>(msg =>
            EditorUtility.DisplayProgressBar("Exporting Map", msg, 0.6f));

        ProcessedMap result;
        try
        {
            result = await Task.Run(() => ProcessGeometry(raw.Value, progress));
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Export Failed", e.Message, "OK");
            return;
        }

        WriteFiles(result);
        EditorUtility.ClearProgressBar();
    }

    // ── Main-thread data gathering ───────────────────────────────────────────

    private static RawSource? GatherRawSources()
    {
        var terrainMeshes = UnityEngine.Object.FindObjectsByType<TerrainMesh>(FindObjectsSortMode.None);
        if (terrainMeshes.Length == 0)
        {
            EditorUtility.DisplayDialog("Export Failed", "No TerrainMesh components found in the scene.", "OK");
            return null;
        }

        var spawnPoints = UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        if (spawnPoints.Length == 0)
        {
            EditorUtility.DisplayDialog("Export Failed", "No SpawnPoint components found in the scene.", "OK");
            return null;
        }

        var raw = new RawSource
        {
            TerrainTris = new List<RawTri>(),
            MeshTris = new List<List<RawTri>>(),
            SpawnPos = spawnPoints[0].transform.position,
            MapName = EditorSceneManager.GetActiveScene().name
        };

        foreach (var tm in terrainMeshes)
        {
            var terrain = tm.GetComponent<Terrain>();
            var meshFilter = tm.GetComponent<MeshFilter>();

            if (terrain != null && terrain.terrainData != null)
            {
                GatherTerrainTris(terrain, tm.resolution, raw.TerrainTris);
            }
            else if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var walkable = GatherMeshWalkableTris(meshFilter.sharedMesh, tm.transform);
                if (walkable.Count > 0)
                    raw.MeshTris.Add(walkable);
                else
                    Debug.LogWarning($"TerrainMesh '{tm.name}': no walkable (upward-facing) faces found.");
            }
            else
            {
                Debug.LogWarning($"TerrainMesh '{tm.name}': no Terrain or MeshFilter component. Skipping.");
            }
        }

        return raw;
    }

    // Samples Unity Terrain into a triangulated mesh matching Heightmap.cs quad split.
    private static void GatherTerrainTris(Terrain terrain, int resolution, List<RawTri> result)
    {
        var data = terrain.terrainData;
        var hmRes = data.heightmapResolution;
        var size = data.size;
        var pos = terrain.transform.position;

        resolution = Mathf.Clamp(resolution, 2, hmRes);
        int step = Mathf.Max(1, (hmRes - 1) / (resolution - 1));
        int actualRes = (hmRes - 1) / step + 1;

        var unityHeights = data.GetHeights(0, 0, hmRes, hmRes);
        float cellX = size.x / (actualRes - 1);
        float cellZ = size.z / (actualRes - 1);

        var verts = new Vector3[actualRes * actualRes];
        for (int z = 0; z < actualRes; z++)
        {
            for (int x = 0; x < actualRes; x++)
            {
                int hmX = Mathf.Min(x * step, hmRes - 1);
                int hmZ = Mathf.Min(z * step, hmRes - 1);
                float worldY = unityHeights[hmZ, hmX] * size.y + pos.y;
                verts[z * actualRes + x] = new Vector3(pos.x + x * cellX, worldY, pos.z + z * cellZ);
            }
        }

        // Diagonal split: topLeft–bottomLeft–topRight (fx+fz≤1) and topRight–bottomLeft–bottomRight (fx+fz>1).
        // Matches Heightmap.cs so runtime queries are consistent.
        for (int gz = 0; gz < actualRes - 1; gz++)
        {
            for (int gx = 0; gx < actualRes - 1; gx++)
            {
                var topLeft     = verts[gz * actualRes + gx];
                var bottomLeft  = verts[(gz + 1) * actualRes + gx];
                var topRight    = verts[gz * actualRes + gx + 1];
                var bottomRight = verts[(gz + 1) * actualRes + gx + 1];

                result.Add(new RawTri { V0 = topLeft,  V1 = bottomLeft, V2 = topRight });
                result.Add(new RawTri { V0 = topRight, V1 = bottomLeft, V2 = bottomRight });
            }
        }

        Debug.Log($"Gathered {(actualRes - 1) * (actualRes - 1) * 2} terrain triangles from '{terrain.name}' at {actualRes}x{actualRes}.");
    }

    // Extracts only upward-facing (walkable) triangles from a mesh object.
    private static List<RawTri> GatherMeshWalkableTris(Mesh mesh, Transform transform)
    {
        var tris = mesh.triangles;
        var verts = mesh.vertices;
        var result = new List<RawTri>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            var v0 = transform.TransformPoint(verts[tris[i]]);
            var v1 = transform.TransformPoint(verts[tris[i + 1]]);
            var v2 = transform.TransformPoint(verts[tris[i + 2]]);

            var faceNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            if (faceNormal.y > WalkableNormalThreshold)
                result.Add(new RawTri { V0 = v0, V1 = v1, V2 = v2 });
        }

        return result;
    }

    // ── Background geometry processing ───────────────────────────────────────

    private static ProcessedMap ProcessGeometry(RawSource raw, IProgress<string> progress)
    {
        progress.Report("Computing terrain boundary…");
        var terrainWalls = ComputeTerrainBoundary(raw.TerrainTris);

        progress.Report("Computing mesh perimeters…");
        var meshWalls = ComputeMeshPerimeters(raw.MeshTris);

        progress.Report("Deduplicating triangles…");
        // Terrain is NOT carved under mesh objects. The highest-Y-first sort below
        // ensures FindTriangleIndex returns the uppermost surface, while the terrain
        // underneath acts as a safety net for entities falling through gaps between
        // adjacent mesh objects.
        var allTris = new List<Tri3>(raw.TerrainTris.Count + raw.MeshTris.Sum(l => l.Count));
        foreach (var t in raw.TerrainTris)
            allTris.Add(new Tri3 { V0 = t.V0, V1 = t.V1, V2 = t.V2 });
        foreach (var obj in raw.MeshTris)
            foreach (var t in obj)
                allTris.Add(new Tri3 { V0 = t.V0, V1 = t.V1, V2 = t.V2 });
        var dedupedTris = DeduplicateTris(allTris);
        // Sort highest-Y centroid first so TerrainGrid's first-match returns the
        // uppermost walkable surface when multiple surfaces share an XZ footprint
        // (e.g. a box top at Y=3 sitting above a floor at Y=0).
        dedupedTris.Sort((x, y) =>
        {
            float xc = (x.V0.y + x.V1.y + x.V2.y) / 3f;
            float yc = (y.V0.y + y.V1.y + y.V2.y) / 3f;
            return yc.CompareTo(xc); // descending
        });

        var allWalls = new List<WallLine>(terrainWalls.Count + meshWalls.Count);
        allWalls.AddRange(terrainWalls);
        allWalls.AddRange(meshWalls);

        return new ProcessedMap
        {
            Walls = allWalls,
            Tris = dedupedTris,
            SpawnPos = raw.SpawnPos,
            MapName = raw.MapName
        };
    }

    // Perimeter edges of ALL terrain triangles (before XZ carving) = map boundary.
    // SurfaceY = float.MaxValue so they always block.
    private static List<WallLine> ComputeTerrainBoundary(List<RawTri> terrainTris)
    {
        var counts = new Dictionary<(long, long), int>();
        var edgeVerts = new Dictionary<(long, long), (Vector3, Vector3)>();

        foreach (var tri in terrainTris)
        {
            AccumulateEdge(counts, edgeVerts, tri.V0, tri.V1);
            AccumulateEdge(counts, edgeVerts, tri.V1, tri.V2);
            AccumulateEdge(counts, edgeVerts, tri.V2, tri.V0);
        }

        var result = new List<WallLine>();
        foreach (var (key, count) in counts)
        {
            if (count != 1) continue;
            var (a, b) = edgeVerts[key];
            result.Add(new WallLine
            {
                Start = new Vector2(a.x, a.z),
                End = new Vector2(b.x, b.z),
                SurfaceY = 0f   // always block: map boundary
            });
        }
        return result;
    }

    // Perimeter of the combined walkable top-faces across all mesh objects.
    // Edges shared between two adjacent mesh objects cancel out (interior edges).
    // SurfaceY = max Y of the edge's two world-space endpoints.
    private static List<WallLine> ComputeMeshPerimeters(List<List<RawTri>> meshTrisPerObj)
    {
        var counts = new Dictionary<(long, long), int>();
        var edgeVerts = new Dictionary<(long, long), (Vector3, Vector3)>();

        foreach (var obj in meshTrisPerObj)
        {
            foreach (var tri in obj)
            {
                AccumulateEdge(counts, edgeVerts, tri.V0, tri.V1);
                AccumulateEdge(counts, edgeVerts, tri.V1, tri.V2);
                AccumulateEdge(counts, edgeVerts, tri.V2, tri.V0);
            }
        }

        var result = new List<WallLine>();
        foreach (var (key, count) in counts)
        {
            if (count != 1) continue;
            var (a, b) = edgeVerts[key];
            float edgeMaxY = Mathf.Max(a.y, b.y);
            result.Add(new WallLine
            {
                Start = new Vector2(a.x, a.z),
                End = new Vector2(b.x, b.z),
                // Snap near-zero values to 0 (always-block). Float noise on ground-level
                // mesh vertices (~4.7e-7) would otherwise make dungeon walls permeable.
                SurfaceY = edgeMaxY > 0.1f ? edgeMaxY : 0f
            });
        }
        return result;
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    // Accumulates an edge (A,B) into a canonical shared-edge count dictionary.
    private static void AccumulateEdge(
        Dictionary<(long, long), int> counts,
        Dictionary<(long, long), (Vector3, Vector3)> data,
        Vector3 a, Vector3 b)
    {
        long ka = PackVert(a);
        long kb = PackVert(b);
        var key = ka <= kb ? (ka, kb) : (kb, ka);

        if (!counts.ContainsKey(key))
        {
            counts[key] = 0;
            data[key] = (a, b);
        }
        counts[key]++;
    }

    // Pack a world-space vertex into a single long for edge/triangle dedup.
    // Quantises to ~0.01 world units; supports coords in ±1,000,000 range.
    private static long PackVert(Vector3 v)
    {
        long x = (long)Math.Round(v.x * 100) & 0xFFFFF;
        long y = (long)Math.Round(v.y * 100) & 0xFFFFF;
        long z = (long)Math.Round(v.z * 100) & 0xFFFFF;
        return x | (y << 20) | (z << 40);
    }

    // Test whether XZ point is inside a triangle projected onto the XZ plane.
    private static bool TriContainsXz(RawTri tri, Vector2 p)
    {
        float d1 = XzSign(p, new Vector2(tri.V0.x, tri.V0.z), new Vector2(tri.V1.x, tri.V1.z));
        float d2 = XzSign(p, new Vector2(tri.V1.x, tri.V1.z), new Vector2(tri.V2.x, tri.V2.z));
        float d3 = XzSign(p, new Vector2(tri.V2.x, tri.V2.z), new Vector2(tri.V0.x, tri.V0.z));
        bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNeg && hasPos);
    }

    private static float XzSign(Vector2 p, Vector2 a, Vector2 b) =>
        (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

    // O(n) triangle deduplication via canonical packed-vertex hash.
    private static List<Tri3> DeduplicateTris(List<Tri3> tris)
    {
        var seen = new HashSet<(long, long, long)>();
        var result = new List<Tri3>(tris.Count);
        foreach (var tri in tris)
        {
            var key = CanonicalTriKey(tri);
            if (seen.Add(key)) result.Add(tri);
        }
        return result;
    }

    private static (long, long, long) CanonicalTriKey(Tri3 tri)
    {
        var k0 = PackVert(tri.V0);
        var k1 = PackVert(tri.V1);
        var k2 = PackVert(tri.V2);
        // Sort three keys (3-element sort network)
        if (k0 > k1) (k0, k1) = (k1, k0);
        if (k1 > k2) (k1, k2) = (k2, k1);
        if (k0 > k1) (k0, k1) = (k1, k0);
        return (k0, k1, k2);
    }

    // ── File writing ─────────────────────────────────────────────────────────

    private static void WriteFiles(ProcessedMap result)
    {
        var mapFilePath = Path.Combine(OutputDir, $"MapData.{result.MapName}.cs");
        File.WriteAllText(mapFilePath, GenerateMapFile(result));

        var allMapNames = DiscoverMapNames();
        File.WriteAllText(Path.Combine(OutputDir, "MapData.cs"), GenerateIndexFile(allMapNames));

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Export Complete",
            $"Exported '{result.MapName}': {result.Walls.Count} collision lines, {result.Tris.Count} triangles.\n" +
            $"{allMapNames.Count} total map(s) registered.", "OK");
    }

    private static List<string> DiscoverMapNames()
    {
        var names = new List<string>();
        foreach (var file in Directory.GetFiles(OutputDir, "MapData.*.cs"))
        {
            var baseName = Path.GetFileNameWithoutExtension(file);
            names.Add(baseName.Substring("MapData.".Length));
        }
        names.Sort();
        return names;
    }

    private static string GenerateMapFile(ProcessedMap map)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by MapExporter. Do not edit.");
        sb.AppendLine("namespace SharedPhysics");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial class MapData");
        sb.AppendLine("    {");

        // Line data: 5 floats per line (startX, startZ, endX, endZ, surfaceY)
        sb.AppendLine($"        private static readonly float[] _{map.MapName}LineData = new float[]");
        sb.AppendLine("        {");
        foreach (var w in map.Walls)
            sb.AppendLine($"            {F(w.Start.x)},{F(w.Start.y)},{F(w.End.x)},{F(w.End.y)},{F(w.SurfaceY)},");
        sb.AppendLine("        };");
        sb.AppendLine();

        // Triangle data: 9 floats per triangle
        sb.AppendLine($"        private static readonly float[] _{map.MapName}TriangleData = new float[]");
        sb.AppendLine("        {");
        foreach (var t in map.Tris)
        {
            sb.AppendLine($"            {F(t.V0.x)},{F(t.V0.y)},{F(t.V0.z)},{F(t.V1.x)},{F(t.V1.y)},{F(t.V1.z)},{F(t.V2.x)},{F(t.V2.y)},{F(t.V2.z)},");
        }
        sb.AppendLine("        };");
        sb.AppendLine();

        sb.AppendLine($"        private static readonly System.Lazy<MapDefinition> _{map.MapName}Lazy = new System.Lazy<MapDefinition>(() => new MapDefinition(");
        sb.AppendLine($"            \"{map.MapName}\",");
        sb.AppendLine($"            BuildLines(_{map.MapName}LineData),");
        sb.AppendLine($"            BuildTriangles(_{map.MapName}TriangleData),");
        sb.AppendLine($"            new Vector3({F(map.SpawnPos.x)}, {F(map.SpawnPos.y)}, {F(map.SpawnPos.z)})");
        sb.AppendLine("        ));");
        sb.AppendLine($"        public static MapDefinition {map.MapName} => _{map.MapName}Lazy.Value;");

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateIndexFile(List<string> mapNames)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by MapExporter. Do not edit.");
        sb.AppendLine("namespace SharedPhysics");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial class MapData");
        sb.AppendLine("    {");

        sb.AppendLine("        public static MapDefinition GetMap(string name) => name switch");
        sb.AppendLine("        {");
        foreach (var name in mapNames)
            sb.AppendLine($"            \"{name}\" => {name},");
        sb.AppendLine("            _ => throw new System.ArgumentException($\"Unknown map: {name}\")");
        sb.AppendLine("        };");
        sb.AppendLine();

        // 5-float line format: startX, startZ, endX, endZ, surfaceY
        sb.AppendLine("        private static Line[] BuildLines(float[] data)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = new Line[data.Length / 5];");
        sb.AppendLine("            for (int i = 0; i < result.Length; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                int j = i * 5;");
        sb.AppendLine("                result[i] = new Line(new Vector2(data[j], data[j + 1]), new Vector2(data[j + 2], data[j + 3]), data[j + 4]);");
        sb.AppendLine("            }");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine("        private static Triangle[] BuildTriangles(float[] data)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = new Triangle[data.Length / 9];");
        sb.AppendLine("            for (int i = 0; i < result.Length; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                int j = i * 9;");
        sb.AppendLine("                result[i] = new Triangle(");
        sb.AppendLine("                    new Vector3(data[j], data[j + 1], data[j + 2]),");
        sb.AppendLine("                    new Vector3(data[j + 3], data[j + 4], data[j + 5]),");
        sb.AppendLine("                    new Vector3(data[j + 6], data[j + 7], data[j + 8]));");
        sb.AppendLine("            }");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string F(float value) =>
        value.ToString("G", CultureInfo.InvariantCulture) + "f";
}
