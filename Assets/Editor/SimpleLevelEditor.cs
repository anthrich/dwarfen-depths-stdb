using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class SimpleLevelEditor : EditorWindow
{
    private const int GRID_WIDTH = 15;
    private const int GRID_HEIGHT = 15;
    private const int CELL_SIZE = 30;
    
    private bool[,] roomGrid = new bool[GRID_WIDTH, GRID_HEIGHT];
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<SimpleLevelEditor>("Level Editor");
        window.minSize = new Vector2(500, 400);
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Simple Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Toolbar
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All", GUILayout.Width(80)))
        {
            ClearAllRooms();
        }
        if (GUILayout.Button("Export C# Level Data", GUILayout.Width(150)))
        {
            ExportToClass();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Grid
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawGrid();
        EditorGUILayout.EndScrollView();
        
        // Info
        EditorGUILayout.Space();
        int roomCount = CountRooms();
        EditorGUILayout.LabelField($"Rooms placed: {roomCount}");
        EditorGUILayout.HelpBox("Click squares to place/remove rooms. Green = room, Gray = empty.", MessageType.Info);
    }
    
    private void DrawGrid()
    {
        // Draw grid from top to bottom (Unity UI convention)
        for (int y = GRID_HEIGHT - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                // Set button color based on room state
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = roomGrid[x, y] ? Color.green : Color.gray;
                
                // Create clickable button for each cell
                if (GUILayout.Button("", GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE)))
                {
                    roomGrid[x, y] = !roomGrid[x, y]; // Toggle room
                }
                
                GUI.backgroundColor = originalColor;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void ClearAllRooms()
    {
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                roomGrid[x, y] = false;
            }
        }
        Repaint();
    }
    
    private int CountRooms()
    {
        int count = 0;
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                if (roomGrid[x, y]) count++;
            }
        }
        return count;
    }
    
    private void ExportToClass()
    {
        var csharpCode = GenerateCSharpClass();
        var path = EditorUtility.SaveFilePanel("Export Level Data", Application.dataPath, "LevelData", "cs");
        
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, csharpCode);
            Debug.Log($"Exported level to: {path}");
            AssetDatabase.Refresh(); // Refresh Unity's asset database
            EditorUtility.DisplayDialog("Export Complete", $"Level exported to:\n{path}\n\nRooms: {CountRooms()}", "OK");
        }
    }
    
    private string GenerateCSharpClass()
    {
        var code = new StringBuilder();
        var rooms = GetRoomData();
        
        // File header
        code.AppendLine("// Auto-generated level data");
        code.AppendLine("// Created by Unity Level Editor");
        code.AppendLine("using System.Collections.Generic;");
        code.AppendLine();
        
        // Room data structure
        code.AppendLine("[System.Serializable]");
        code.AppendLine("public struct RoomData");
        code.AppendLine("{");
        code.AppendLine("    public int x;");
        code.AppendLine("    public int y;");
        code.AppendLine("    public int roomType;");
        code.AppendLine();
        code.AppendLine("    public RoomData(int x, int y, int roomType = 0)");
        code.AppendLine("    {");
        code.AppendLine("        this.x = x;");
        code.AppendLine("        this.y = y;");
        code.AppendLine("        this.roomType = roomType;");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();
        
        // Main static class
        code.AppendLine("public static class LevelData");
        code.AppendLine("{");
        code.AppendLine($"    public const int GRID_WIDTH = {GRID_WIDTH};");
        code.AppendLine($"    public const int GRID_HEIGHT = {GRID_HEIGHT};");
        code.AppendLine($"    public const int TOTAL_ROOMS = {rooms.Count};");
        code.AppendLine();
        
        // Room array
        code.AppendLine("    public static readonly RoomData[] Rooms = new RoomData[]");
        code.AppendLine("    {");
        
        if (rooms.Count > 0)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                string comma = (i < rooms.Count - 1) ? "," : "";
                code.AppendLine($"        new RoomData({room.x}, {room.y}, 0){comma}");
            }
        }
        
        code.AppendLine("    };");
        code.AppendLine();
        
        code.AppendLine("    public static bool HasRoomAt(int x, int y)");
        code.AppendLine("    {");
        code.AppendLine("        foreach (var room in Rooms)");
        code.AppendLine("        {");
        code.AppendLine("            if (room.x == x && room.y == y)");
        code.AppendLine("                return true;");
        code.AppendLine("        }");
        code.AppendLine("        return false;");
        code.AppendLine("    }");
        code.AppendLine();
        
        code.AppendLine("    public static RoomData? GetRoomAt(int x, int y)");
        code.AppendLine("    {");
        code.AppendLine("        foreach (var room in Rooms)");
        code.AppendLine("        {");
        code.AppendLine("            if (room.x == x && room.y == y)");
        code.AppendLine("                return room;");
        code.AppendLine("        }");
        code.AppendLine("        return null;");
        code.AppendLine("    }");
        code.AppendLine();
        code.AppendLine("}");
        
        return code.ToString();
    }
    
    private List<RoomPosition> GetRoomData()
    {
        var rooms = new List<RoomPosition>();
        
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                if (roomGrid[x, y])
                {
                    rooms.Add(new RoomPosition { x = x, y = y });
                }
            }
        }
        
        return rooms;
    }
    
    private struct RoomPosition
    {
        public int x;
        public int y;
    }
}