using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	const int SendUpdatesPerSec = 20;
	const float SendUpdatesFrequency = 1f / SendUpdatesPerSec;
	public uint playerId;
    private float _lastMovementSendTimestamp;
    private Vector2? _lockInputPosition;
	public string username = "Unknown";
	public bool isLocalPlayer;

	public void Initialize(Player player)
	{
		username = player.Name;
        playerId = player.PlayerId;
        if (player.Identity == GameManager.LocalIdentity)
        {
	        isLocalPlayer = true;
        }
	}
}