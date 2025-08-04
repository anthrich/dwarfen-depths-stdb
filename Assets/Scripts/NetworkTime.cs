using JetBrains.Annotations;
using SpacetimeDB.Types;
using UnityEngine;

public class NetworkTime : MonoBehaviour
{
    public float timeScale = 1f;
    public float adjustmentRate = 0.0025f;

    [UsedImplicitly]
    public void OnPlayerUpdated(Player newPlayer)
    {
        timeScale = newPlayer.SimulationOffset switch
        {
            < 2 => Mathf.MoveTowards(timeScale, 1.2f, adjustmentRate),
            > 2 => Mathf.MoveTowards(timeScale, 0.8f, adjustmentRate),
            2 => 1f
        };

        Time.timeScale = timeScale;
    }
}