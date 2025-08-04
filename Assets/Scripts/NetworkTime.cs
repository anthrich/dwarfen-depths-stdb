using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkTime : MonoBehaviour
{
    public float timeScale = 1f;

    [UsedImplicitly]
    public void OnPlayerUpdated(Player newPlayer)
    {
        switch (newPlayer.SimulationOffset)
        {
            case < 1:
                timeScale += 0.01f;
                break;
            case > 5:
                timeScale -= 0.01f;
                break;
            default:
                timeScale = 1f;
                break;
        }
        
        timeScale = Mathf.Clamp(timeScale, 0.8f, 1.2f);
        Time.timeScale = timeScale;
    }
}