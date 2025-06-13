using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Detects terrain layers for footstep surface detection
/// </summary>
[RequireComponent(typeof(FootstepSystem))]
public class TerrainLayerDetector : MonoBehaviour
{
    [Header("Terrain Layer Detection")]
    [Tooltip("Enable terrain layer detection")]
    public bool enableTerrainDetection = true;
    
    [Header("Terrain Layer Mappings")]
    [Tooltip("Map terrain layer indices to surface names")]
    public TerrainLayerMapping[] terrainLayerMappings = new TerrainLayerMapping[]
    {
        new TerrainLayerMapping(0, "grass", "Grass/Herbe"),
        new TerrainLayerMapping(1, "dirt", "Dirt/Terre"),
        new TerrainLayerMapping(2, "stone", "Stone/Pierre"),
        new TerrainLayerMapping(3, "sand", "Sand/Sable")
    };
    
    [Header("Blend Settings")]
    [Tooltip("Minimum blend value to consider a layer (0-1)")]
    [Range(0.1f, 0.9f)]
    public float blendThreshold = 0.3f;
    
    [Tooltip("Use only the dominant layer")]
    public bool useDominantLayerOnly = true;
    
    // Private
    private Terrain currentTerrain;
    private TerrainData terrainData;
    private Dictionary<int, string> layerIndexToSurface;
    private int alphamapWidth;
    private int alphamapHeight;
    
    void Start()
    {
        BuildLayerDictionary();
        FindTerrain();
    }
    
    void BuildLayerDictionary()
    {
        layerIndexToSurface = new Dictionary<int, string>();
        
        foreach (var mapping in terrainLayerMappings)
        {
            if (!string.IsNullOrEmpty(mapping.surfaceName))
            {
                layerIndexToSurface[mapping.layerIndex] = mapping.surfaceName;
            }
        }
    }
    
    void FindTerrain()
    {
        currentTerrain = Terrain.activeTerrain;
        
        if (currentTerrain != null)
        {
            terrainData = currentTerrain.terrainData;
            alphamapWidth = terrainData.alphamapWidth;
            alphamapHeight = terrainData.alphamapHeight;
        }
    }
    
    public string GetCurrentTerrainSurface(Vector3 worldPosition)
    {
        if (!enableTerrainDetection || currentTerrain == null || terrainData == null)
            return "";
        
        // Convert world position to terrain coordinates
        Vector3 terrainPosition = worldPosition - currentTerrain.transform.position;
        Vector3 normalizedPos = new Vector3(
            terrainPosition.x / terrainData.size.x,
            0,
            terrainPosition.z / terrainData.size.z
        );
        
        // Check bounds
        if (normalizedPos.x < 0 || normalizedPos.x > 1 || normalizedPos.z < 0 || normalizedPos.z > 1)
            return "";
        
        // Convert to alphamap coordinates
        int alphamapX = Mathf.FloorToInt(normalizedPos.x * (alphamapWidth - 1));
        int alphamapZ = Mathf.FloorToInt(normalizedPos.z * (alphamapHeight - 1));
        
        // Get alphamap values at position
        float[,,] alphamaps = terrainData.GetAlphamaps(alphamapX, alphamapZ, 1, 1);
        
        if (useDominantLayerOnly)
        {
            // Find dominant layer
            int dominantLayer = -1;
            float maxBlend = 0f;
            
            for (int layer = 0; layer < terrainData.terrainLayers.Length; layer++)
            {
                float blend = alphamaps[0, 0, layer];
                if (blend > maxBlend)
                {
                    maxBlend = blend;
                    dominantLayer = layer;
                }
            }
            
            if (dominantLayer >= 0 && maxBlend >= blendThreshold)
            {
                return GetSurfaceNameForLayer(dominantLayer);
            }
        }
        else
        {
            // Use blended layers
            Dictionary<string, float> surfaceBlends = new Dictionary<string, float>();
            
            for (int layer = 0; layer < terrainData.terrainLayers.Length; layer++)
            {
                float blend = alphamaps[0, 0, layer];
                if (blend >= blendThreshold)
                {
                    string surfaceName = GetSurfaceNameForLayer(layer);
                    if (!string.IsNullOrEmpty(surfaceName))
                    {
                        if (surfaceBlends.ContainsKey(surfaceName))
                            surfaceBlends[surfaceName] += blend;
                        else
                            surfaceBlends[surfaceName] = blend;
                    }
                }
            }
            
            // Return surface with highest blend
            if (surfaceBlends.Count > 0)
            {
                var dominant = surfaceBlends.OrderByDescending(kvp => kvp.Value).First();
                return dominant.Key;
            }
        }
        
        return "";
    }
    
    string GetSurfaceNameForLayer(int layerIndex)
    {
        // Check configured mapping
        if (layerIndexToSurface.TryGetValue(layerIndex, out string mappedName))
        {
            return mappedName;
        }
        
        // Try to deduce from TerrainLayer name
        if (layerIndex < terrainData.terrainLayers.Length && 
            terrainData.terrainLayers[layerIndex] != null)
        {
            string layerName = terrainData.terrainLayers[layerIndex].name.ToLower();
            
            // Keyword search
            if (layerName.Contains("grass") || layerName.Contains("herbe")) return "grass";
            if (layerName.Contains("dirt") || layerName.Contains("terre")) return "dirt";
            if (layerName.Contains("stone") || layerName.Contains("pierre")) return "stone";
            if (layerName.Contains("sand") || layerName.Contains("sable")) return "sand";
            if (layerName.Contains("rock") || layerName.Contains("rocher")) return "stone";
            if (layerName.Contains("metal")) return "metal";
            if (layerName.Contains("wood") || layerName.Contains("bois")) return "wood";
        }
        
        return "";
    }
    
    [System.Serializable]
    public class TerrainLayerMapping
    {
        [Tooltip("Terrain layer index (0, 1, 2, etc.)")]
        public int layerIndex;
        
        [Tooltip("Surface name (grass, dirt, stone, etc.)")]
        public string surfaceName;
        
        [Tooltip("Description")]
        public string description;
        
        public TerrainLayerMapping(int index, string surface, string desc = "")
        {
            layerIndex = index;
            surfaceName = surface;
            description = desc;
        }
    }
}
