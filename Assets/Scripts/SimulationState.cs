using UnityEngine;

public struct SimulationState
{
    public static bool IsDefault(SimulationState simState) =>
        simState.Position == Vector2.zero && simState.SequenceId == 0;
        
    public Vector2 Position;
    public ulong SequenceId;
}