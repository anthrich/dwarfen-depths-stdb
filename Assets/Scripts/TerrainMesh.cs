using UnityEngine;

public class TerrainMesh : MonoBehaviour
{
    [Tooltip("Resolution for terrain heightmap sampling. Only used with Unity Terrain components. Higher = more detailed but larger export.")]
    public int resolution = 64;
}
