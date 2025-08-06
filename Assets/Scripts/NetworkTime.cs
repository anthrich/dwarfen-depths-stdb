using JetBrains.Annotations;
using SpacetimeDB.Types;
using UnityEngine;

public class NetworkTime : MonoBehaviour
{
    public float timeScale = 1f;
    public float adjustmentRate = 0.001f;
    public float targetTimeScale = 1f;

    [UsedImplicitly]
    public void OnPlayerUpdated(Player newPlayer)
    {
        timeScale = newPlayer.SimulationOffset switch
        {
            < 2 => Mathf.MoveTowards(timeScale, 1.2f, adjustmentRate),
            > 3 => Mathf.MoveTowards(timeScale, 0.8f, adjustmentRate),
            _ => Mathf.MoveTowards(timeScale, targetTimeScale, adjustmentRate * 2),
        };

        Time.timeScale = timeScale;
    }
}