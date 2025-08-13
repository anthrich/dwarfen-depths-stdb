using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
	[FormerlySerializedAs("playerId")] public uint EntityId;
	public string username = "Unknown";
	public bool isLocalPlayer;

	public void Initialize(Player player)
	{
		username = player.Name;
        EntityId = player.EntityId;
        if (player.Identity == GameManager.LocalIdentity)
        {
	        isLocalPlayer = true;
        }
	}
}