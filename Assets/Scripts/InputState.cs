using UnityEngine;

public struct InputState
{
    public static bool IsDefault(InputState @is) =>
        @is.Direction == Vector2.zero && @is.SequenceId == 0;
        
    public Vector2 Direction;
    public ulong SequenceId;
}