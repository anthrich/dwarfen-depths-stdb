using UnityEngine;
using UnityEditor;

public static class MapMigration
{
    private const float RoomSize = 10f;

    private static readonly (int x, int y)[] Rooms =
    {
        (2, 10), (2, 11), (2, 12), (2, 13), (2, 14),
        (3, 0), (3, 1), (3, 2), (3, 3), (3, 4), (3, 10), (3, 12), (3, 14),
        (4, 0), (4, 4), (4, 10), (4, 12), (4, 14),
        (5, 0), (5, 4), (5, 10), (5, 12), (5, 14),
        (6, 0), (6, 4), (6, 10), (6, 12), (6, 14),
        (7, 0), (7, 1), (7, 2), (7, 3), (7, 4), (7, 5), (7, 6), (7, 7), (7, 8), (7, 9), (7, 10), (7, 11), (7, 12), (7, 13), (7, 14),
        (8, 0), (8, 4), (8, 10), (8, 12), (8, 14),
        (9, 0), (9, 4), (9, 10), (9, 12), (9, 14),
        (10, 0), (10, 4), (10, 10), (10, 12), (10, 14),
        (11, 0), (11, 1), (11, 2), (11, 3), (11, 4), (11, 10), (11, 12), (11, 14),
        (12, 10), (12, 11), (12, 12), (12, 13), (12, 14)
    };

    [MenuItem("Tools/Migrate Level Data to Scene")]
    public static void Migrate()
    {
        var mapParent = new GameObject("Map").transform;

        foreach (var room in Rooms)
        {
            var roomObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            roomObj.name = $"Floor_{room.x}_{room.y}";
            roomObj.transform.SetParent(mapParent);
            roomObj.transform.position = new Vector3(room.x * RoomSize, 0f, room.y * RoomSize);
            roomObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            roomObj.transform.localScale = new Vector3(RoomSize, RoomSize, 1f);

            // Remove the default collider (we use SharedPhysics for collision)
            var collider = roomObj.GetComponent<Collider>();
            if (collider) Object.DestroyImmediate(collider);

            roomObj.AddComponent<TerrainMesh>();
        }

        // Add spawn point at first room
        var spawnObj = new GameObject("SpawnPoint");
        spawnObj.transform.SetParent(mapParent);
        spawnObj.transform.position = new Vector3(Rooms[0].x * RoomSize, 0f, Rooms[0].y * RoomSize);
        spawnObj.AddComponent<SpawnPoint>();

        Debug.Log($"Migrated {Rooms.Length} rooms to scene. SpawnPoint at ({Rooms[0].x * RoomSize}, 0, {Rooms[0].y * RoomSize})");
        EditorUtility.DisplayDialog("Migration Complete", $"Created {Rooms.Length} floor tiles with TerrainMesh components.\nUse Tools > Export Map Data to generate MapData.cs.", "OK");
    }
}
