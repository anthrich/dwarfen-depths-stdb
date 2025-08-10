// Auto-generated level data
// Created by Unity Level Editor

[Serializable]
public struct RoomData
{
    public int x;
    public int y;
    public int roomType;

    public RoomData(int x, int y, int roomType = 0)
    {
        this.x = x;
        this.y = y;
        this.roomType = roomType;
    }
}

public static class LevelData
{
    public const int GRID_WIDTH = 15;
    public const int GRID_HEIGHT = 15;
    public const int TOTAL_ROOMS = 71;

    public static readonly RoomData[] Rooms = new RoomData[]
    {
        new RoomData(2, 10, 0),
        new RoomData(2, 11, 0),
        new RoomData(2, 12, 0),
        new RoomData(2, 13, 0),
        new RoomData(2, 14, 0),
        new RoomData(3, 0, 0),
        new RoomData(3, 1, 0),
        new RoomData(3, 2, 0),
        new RoomData(3, 3, 0),
        new RoomData(3, 4, 0),
        new RoomData(3, 10, 0),
        new RoomData(3, 12, 0),
        new RoomData(3, 14, 0),
        new RoomData(4, 0, 0),
        new RoomData(4, 4, 0),
        new RoomData(4, 10, 0),
        new RoomData(4, 12, 0),
        new RoomData(4, 14, 0),
        new RoomData(5, 0, 0),
        new RoomData(5, 4, 0),
        new RoomData(5, 10, 0),
        new RoomData(5, 12, 0),
        new RoomData(5, 14, 0),
        new RoomData(6, 0, 0),
        new RoomData(6, 4, 0),
        new RoomData(6, 10, 0),
        new RoomData(6, 12, 0),
        new RoomData(6, 14, 0),
        new RoomData(7, 0, 0),
        new RoomData(7, 1, 0),
        new RoomData(7, 2, 0),
        new RoomData(7, 3, 0),
        new RoomData(7, 4, 0),
        new RoomData(7, 5, 0),
        new RoomData(7, 6, 0),
        new RoomData(7, 7, 0),
        new RoomData(7, 8, 0),
        new RoomData(7, 9, 0),
        new RoomData(7, 10, 0),
        new RoomData(7, 11, 0),
        new RoomData(7, 12, 0),
        new RoomData(7, 13, 0),
        new RoomData(7, 14, 0),
        new RoomData(8, 0, 0),
        new RoomData(8, 4, 0),
        new RoomData(8, 10, 0),
        new RoomData(8, 12, 0),
        new RoomData(8, 14, 0),
        new RoomData(9, 0, 0),
        new RoomData(9, 4, 0),
        new RoomData(9, 10, 0),
        new RoomData(9, 12, 0),
        new RoomData(9, 14, 0),
        new RoomData(10, 0, 0),
        new RoomData(10, 4, 0),
        new RoomData(10, 10, 0),
        new RoomData(10, 12, 0),
        new RoomData(10, 14, 0),
        new RoomData(11, 0, 0),
        new RoomData(11, 1, 0),
        new RoomData(11, 2, 0),
        new RoomData(11, 3, 0),
        new RoomData(11, 4, 0),
        new RoomData(11, 10, 0),
        new RoomData(11, 12, 0),
        new RoomData(11, 14, 0),
        new RoomData(12, 10, 0),
        new RoomData(12, 11, 0),
        new RoomData(12, 12, 0),
        new RoomData(12, 13, 0),
        new RoomData(12, 14, 0)
    };

    /// <summary>
    /// Check if a room exists at the given coordinates
    /// </summary>
    public static bool HasRoomAt(int x, int y)
    {
        foreach (var room in Rooms)
        {
            if (room.x == x && room.y == y)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get room data at specific coordinates, or null if no room exists
    /// </summary>
    public static RoomData? GetRoomAt(int x, int y)
    {
        foreach (var room in Rooms)
        {
            if (room.x == x && room.y == y)
                return room;
        }
        return null;
    }

}
