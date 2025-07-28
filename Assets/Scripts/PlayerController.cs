using SpacetimeDB.Types;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
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