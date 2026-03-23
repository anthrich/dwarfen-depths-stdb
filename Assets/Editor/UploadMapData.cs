using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpacetimeDB;
using SpacetimeDB.Types;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class UploadMapData : EditorWindow
{
    private const float WalkableNormalThreshold = 0.5f;
    private const float TriangleCellSize = 8f;
    private const int TriBatchSize = 500;

    private static readonly Dictionary<string, string> ServerChoices = new()
    {
        { "Local",     "http://127.0.0.1:3000" },
        { "Maincloud", "https://maincloud.spacetimedb.com" },
    };
    private static readonly string[] ServerKeys = { "Local", "Maincloud", "Custom" };

    private int _serverIndex;
    private string _customUrl = "";
    private string _moduleName = "dwarfen-depths";
    private string _statusText = "";
    private bool _busy;

    private DbConnection _conn;

    [MenuItem("Tools/Upload Map Data")]
    public static void ShowWindow() => GetWindow<UploadMapData>("Upload Map Data");

    private void OnEnable()  => EditorApplication.update += EditorTick;
    private void OnDisable() => EditorApplication.update -= EditorTick;
    private void EditorTick() => _conn?.FrameTick();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("SpacetimeDB Map Upload", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _serverIndex = EditorGUILayout.Popup("Server", _serverIndex, ServerKeys);
        if (_serverIndex == 2)
            _customUrl = EditorGUILayout.TextField("Custom URL", _customUrl);

        _moduleName = EditorGUILayout.TextField("Module Name", _moduleName);

        EditorGUILayout.Space();
        GUI.enabled = !_busy;
        if (GUILayout.Button("Export & Upload")) ExportAndUpload();
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(_statusText))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_statusText, MessageType.Info);
        }
    }

    private string ResolvedServerUrl() =>
        _serverIndex == 2 ? _customUrl : ServerChoices[ServerKeys[_serverIndex]];

    // ── Entry point ───────────────────────────────────────────────────────────

    private async void ExportAndUpload()
    {
        var raw = GatherRawSources();
        if (raw == null) return;

        _busy = true;
        SetStatus("Processing geometry…");

        ProcessedMap map;
        try
        {
            map = await Task.Run(() => ProcessGeometry(raw.Value));
        }
        catch (Exception e)
        {
            _busy = false;
            SetStatus($"Geometry error: {e.Message}");
            return;
        }

        try
        {
            SetStatus("Connecting…");
            await ConnectAsync();

            SetStatus($"Clearing existing map data for '{map.MapName}'…");
            var clearTcs = MakeTcs();
            _conn.Reducers.OnClearMapData += OnClear;
            _conn.Reducers.ClearMapData(map.MapName);
            await clearTcs.Task;
            _conn.Reducers.OnClearMapData -= OnClear;
            void OnClear(ReducerEventContext ctx, string _) => ResolveReducer(clearTcs, ctx, "ClearMapData");

            SetStatus($"Uploading {map.Tris.Count} triangles…");
            await UploadTriangleBatches(map);

            SetStatus("Uploading MapConfig…");
            var cfgTcs = MakeTcs();
            _conn.Reducers.OnUploadMapConfig += OnCfg;
            _conn.Reducers.UploadMapConfig(new MapConfig
            {
                MapName          = map.MapName,
                SpawnX           = map.SpawnPos.x,
                SpawnY           = map.SpawnPos.y,
                SpawnZ           = map.SpawnPos.z,
                TriangleCellSize = TriangleCellSize,
            });
            await cfgTcs.Task;
            _conn.Reducers.OnUploadMapConfig -= OnCfg;
            void OnCfg(ReducerEventContext ctx, MapConfig _) => ResolveReducer(cfgTcs, ctx, "UploadMapConfig");

            SetStatus("Spawning default entities…");
            var spawnTcs = MakeTcs();
            _conn.Reducers.OnSpawnDefaultEntities += OnSpawn;
            _conn.Reducers.SpawnDefaultEntities();
            await spawnTcs.Task;
            _conn.Reducers.OnSpawnDefaultEntities -= OnSpawn;
            void OnSpawn(ReducerEventContext ctx) => ResolveReducer(spawnTcs, ctx, "SpawnDefaultEntities");

            SetStatus($"Done! Uploaded '{map.MapName}': {map.Tris.Count} triangles.");
        }
        catch (Exception e)
        {
            SetStatus($"Upload failed: {e.Message}");
            Debug.LogError($"[UploadMapData] {e}");
        }
        finally
        {
            _conn?.Disconnect();
            _conn = null;
            _busy = false;
            Repaint();
        }
    }

    // ── SDK helpers ───────────────────────────────────────────────────────────

    private Task ConnectAsync()
    {
        var tcs = MakeTcs();
        _conn = DbConnection.Builder()
            .WithUri(ResolvedServerUrl())
            .WithModuleName(_moduleName)
            .WithToken(AuthToken.Token)
            .OnConnect((_, _, token) => { AuthToken.SaveToken(token); tcs.TrySetResult(true); })
            .OnConnectError(ex => tcs.TrySetException(ex ?? new Exception("Connection failed")))
            .Build();
        return tcs.Task;
    }

    private static TaskCompletionSource<bool> MakeTcs() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void ResolveReducer(TaskCompletionSource<bool> tcs, ReducerEventContext ctx, string name)
    {
        if (ctx.Event.Status is Status.Committed)
            tcs.TrySetResult(true);
        else
            tcs.TrySetException(new Exception($"{name} failed: {ctx.Event.Status}"));
    }

    private void SetStatus(string msg) { _statusText = msg; Repaint(); }

    // ── Batch upload ──────────────────────────────────────────────────────────

    private async Task UploadTriangleBatches(ProcessedMap map)
    {
        for (int i = 0; i < map.Tris.Count; i += TriBatchSize)
        {
            int end = Math.Min(i + TriBatchSize, map.Tris.Count);
            var batch = new List<MapTriangleCell>(end - i);
            for (int j = i; j < end; j++)
            {
                var t = map.Tris[j];
                batch.Add(new MapTriangleCell
                {
                    Id = 0, MapName = map.MapName,
                    CellX = t.CellX, CellZ = t.CellZ,
                    V0X = t.V0.x, V0Y = t.V0.y, V0Z = t.V0.z,
                    V1X = t.V1.x, V1Y = t.V1.y, V1Z = t.V1.z,
                    V2X = t.V2.x, V2Y = t.V2.y, V2Z = t.V2.z,
                });
            }
            var tcs = MakeTcs();
            _conn.Reducers.OnUploadMapTriangleBatch += OnBatch;
            _conn.Reducers.UploadMapTriangleBatch(map.MapName, batch);
            await tcs.Task;
            _conn.Reducers.OnUploadMapTriangleBatch -= OnBatch;
            void OnBatch(ReducerEventContext ctx, string _, List<MapTriangleCell> __) =>
                ResolveReducer(tcs, ctx, "UploadMapTriangleBatch");

            SetStatus($"Uploading triangles… {end}/{map.Tris.Count}");
        }
    }

    // ── Raw data gathered on the Unity main thread ────────────────────────────

    private struct RawTri { public Vector3 V0, V1, V2; }

    private struct RawSource
    {
        public List<RawTri> TerrainTris;
        public List<List<RawTri>> MeshTris;
        public Vector3 SpawnPos;
        public string MapName;
    }

    private struct CellTri
    {
        public int CellX, CellZ;
        public Vector3 V0, V1, V2;
    }

    private struct ProcessedMap
    {
        public string MapName;
        public Vector3 SpawnPos;
        public List<CellTri> Tris;
    }

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
            MeshTris    = new List<List<RawTri>>(),
            SpawnPos    = spawnPoints[0].transform.position,
            MapName     = EditorSceneManager.GetActiveScene().name,
        };

        foreach (var tm in terrainMeshes)
        {
            var terrain    = tm.GetComponent<Terrain>();
            var meshFilter = tm.GetComponent<MeshFilter>();

            if (terrain != null && terrain.terrainData != null)
                GatherTerrainTris(terrain, tm.resolution, raw.TerrainTris);
            else if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var walkable = GatherMeshWalkableTris(meshFilter.sharedMesh, tm.transform);
                if (walkable.Count > 0)
                    raw.MeshTris.Add(walkable);
                else
                    Debug.LogWarning($"TerrainMesh '{tm.name}': no walkable (upward-facing) faces found.");
            }
            else
                Debug.LogWarning($"TerrainMesh '{tm.name}': no Terrain or MeshFilter component. Skipping.");
        }

        return raw;
    }

    private static void GatherTerrainTris(Terrain terrain, int resolution, List<RawTri> result)
    {
        var data  = terrain.terrainData;
        var hmRes = data.heightmapResolution;
        var size  = data.size;
        var pos   = terrain.transform.position;

        resolution = Mathf.Clamp(resolution, 2, hmRes);
        int step      = Mathf.Max(1, (hmRes - 1) / (resolution - 1));
        int actualRes = (hmRes - 1) / step + 1;

        var unityHeights = data.GetHeights(0, 0, hmRes, hmRes);
        float cellX = size.x / (actualRes - 1);
        float cellZ = size.z / (actualRes - 1);

        var verts = new Vector3[actualRes * actualRes];
        for (int z = 0; z < actualRes; z++)
        for (int x = 0; x < actualRes; x++)
        {
            int hmX   = Mathf.Min(x * step, hmRes - 1);
            int hmZ   = Mathf.Min(z * step, hmRes - 1);
            float worldY = unityHeights[hmZ, hmX] * size.y + pos.y;
            verts[z * actualRes + x] = new Vector3(pos.x + x * cellX, worldY, pos.z + z * cellZ);
        }

        for (int gz = 0; gz < actualRes - 1; gz++)
        for (int gx = 0; gx < actualRes - 1; gx++)
        {
            var tl = verts[gz * actualRes + gx];
            var bl = verts[(gz + 1) * actualRes + gx];
            var tr = verts[gz * actualRes + gx + 1];
            var br = verts[(gz + 1) * actualRes + gx + 1];
            result.Add(new RawTri { V0 = tl, V1 = bl, V2 = tr });
            result.Add(new RawTri { V0 = tr, V1 = bl, V2 = br });
        }
    }

    private static List<RawTri> GatherMeshWalkableTris(Mesh mesh, Transform transform)
    {
        var tris   = mesh.triangles;
        var verts  = mesh.vertices;
        var result = new List<RawTri>();
        for (int i = 0; i < tris.Length; i += 3)
        {
            var v0 = transform.TransformPoint(verts[tris[i]]);
            var v1 = transform.TransformPoint(verts[tris[i + 1]]);
            var v2 = transform.TransformPoint(verts[tris[i + 2]]);
            if (Vector3.Cross(v1 - v0, v2 - v0).normalized.y > WalkableNormalThreshold)
                result.Add(new RawTri { V0 = v0, V1 = v1, V2 = v2 });
        }
        return result;
    }

    // ── Background geometry processing ────────────────────────────────────────

    private static ProcessedMap ProcessGeometry(RawSource raw)
    {
        var allRaw = new List<RawTri>(raw.TerrainTris.Count + raw.MeshTris.Sum(l => l.Count));
        allRaw.AddRange(raw.TerrainTris);
        foreach (var obj in raw.MeshTris) allRaw.AddRange(obj);
        var deduped = DeduplicateTris(allRaw);

        // Highest centroid-Y first so TerrainGrid returns the uppermost surface.
        deduped.Sort((a, b) =>
            ((b.V0.y + b.V1.y + b.V2.y) / 3f).CompareTo((a.V0.y + a.V1.y + a.V2.y) / 3f));

        var cellTris = new List<CellTri>(deduped.Count);
        foreach (var t in deduped)
        {
            cellTris.Add(new CellTri
            {
                CellX = (int)MathF.Floor((t.V0.x + t.V1.x + t.V2.x) / 3f / TriangleCellSize),
                CellZ = (int)MathF.Floor((t.V0.z + t.V1.z + t.V2.z) / 3f / TriangleCellSize),
                V0 = t.V0, V1 = t.V1, V2 = t.V2,
            });
        }

        return new ProcessedMap { MapName = raw.MapName, SpawnPos = raw.SpawnPos, Tris = cellTris };
    }

    private static long PackVert(Vector3 v)
    {
        long x = (long)Math.Round(v.x * 100) & 0xFFFFF;
        long y = (long)Math.Round(v.y * 100) & 0xFFFFF;
        long z = (long)Math.Round(v.z * 100) & 0xFFFFF;
        return x | (y << 20) | (z << 40);
    }

    private static List<RawTri> DeduplicateTris(List<RawTri> tris)
    {
        var seen   = new HashSet<(long, long, long)>();
        var result = new List<RawTri>(tris.Count);
        foreach (var tri in tris)
        {
            var k0 = PackVert(tri.V0);
            var k1 = PackVert(tri.V1);
            var k2 = PackVert(tri.V2);
            if (k0 > k1) (k0, k1) = (k1, k0);
            if (k1 > k2) (k1, k2) = (k2, k1);
            if (k0 > k1) (k0, k1) = (k1, k0);
            if (seen.Add((k0, k1, k2))) result.Add(tri);
        }
        return result;
    }
}
