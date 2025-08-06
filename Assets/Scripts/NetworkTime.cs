using System;
using JetBrains.Annotations;
using SpacetimeDB.Types;
using UnityEngine;

public class NetworkTime : MonoBehaviour
{
    public float timeScale = 1f;
    public float adjustmentRate = 0.005f;
    public float targetTimeScale = 1f;

    [UsedImplicitly]
    public void OnPlayerUpdated(Player newPlayer)
    {
        if (newPlayer.PlayerId != GameManager.LocalPlayer.playerId) return;
        timeScale = newPlayer.SimulationOffset switch
        {
            < 2 => 1.2f,
            > 3 => 0.8f,
            _ => targetTimeScale,
        };
    }

    private void FixedUpdate()
    {
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, timeScale, adjustmentRate);
    }
}