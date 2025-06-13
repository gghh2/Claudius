using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extension du FootstepSystem pour détecter les Terrain Layers en plus des Materials
/// </summary>
[RequireComponent(typeof(FootstepSystem))]
public class TerrainLayerDetector : MonoBehaviour
{
    [Header("Terrain Layer Detection")]
    [Tooltip("Active la détection des layers de terrain")]
    public bool enableTerrainDetection = true;
    
    [Header("Terrain Layer Mappings")]
    [Tooltip("Correspondance entre les indices de layers et les noms de surface")]
    public TerrainLayerMapping[] terrainLayerMappings = new TerrainLayerMapping[]
    {
        new TerrainLayerMapping(0, "grass", "Herbe"),
        new TerrainLayerMapping(1, "dirt", "Terre"),
        new TerrainLayerMapping(2, "stone", "Pierre"),
        new TerrainLayerMapping(3, "sand", "Sable"),
        new TerrainLayerMapping(4, "snow", "Neige"),
        new TerrainLayerMapping(5, "rock", "Rocher"),
        new TerrainLayerMapping(6, "mud", "Boue"),
        new TerrainLayerMapping(7, "gravel", "Gravier")
    };
    
    [Header("Blend Settings")]
    [Tooltip("Seuil minimum de blend pour considérer un layer (0-1)")]
    [Range(0.1f, 0.9f)]
    public float blendThreshold = 0.3f;
    
    [Tooltip("Utilise le layer dominant uniquement")]
    public bool useDominantLayerOnly = true;
    
    [Header("Debug")]
    public bool showTerrainDebug = false;
    
    // Références
    private FootstepSystem footstepSystem;
    private Terrain currentTerrain;
    private TerrainData terrainData;
    
    // Cache
    private Dictionary<int, string> layerIndexToSurface;
    private int alphamapWidth;
    private int alphamapHeight;
    private float[,,] alphamaps;
    
    void Start()
    {
        footstepSystem = GetComponent<FootstepSystem>();
        BuildLayerDictionary();
        FindTerrain();
        
        if (showTerrainDebug && currentTerrain != null)
        {
            Debug.Log($"🏔️ TerrainLayerDetector initialisé - {terrainData.terrainLayers.Length} layers trouvés");
        }
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
        // Trouve le terrain actif dans la scène
        currentTerrain = Terrain.activeTerrain;
        
        if (currentTerrain != null)
        {
            terrainData = currentTerrain.terrainData;
            
            // Cache les dimensions de l'alphamap
            alphamapWidth = terrainData.alphamapWidth;
            alphamapHeight = terrainData.alphamapHeight;
            
            if (showTerrainDebug)
            {
                Debug.Log($"🏔️ Terrain trouvé: {currentTerrain.name}");
                Debug.Log($"   Dimensions alphamap: {alphamapWidth}x{alphamapHeight}");
                Debug.Log($"   Nombre de layers: {terrainData.terrainLayers.Length}");
                
                for (int i = 0; i < terrainData.terrainLayers.Length; i++)
                {
                    if (terrainData.terrainLayers[i] != null)
                    {
                        Debug.Log($"   Layer {i}: {terrainData.terrainLayers[i].name}");
                    }
                }
            }
        }
        else if (showTerrainDebug)
        {
            Debug.LogWarning("🏔️ Aucun terrain actif trouvé dans la scène");
        }
    }
    
    /// <summary>
    /// Détecte le layer de terrain à une position donnée
    /// </summary>
    public string DetectTerrainSurface(Vector3 worldPosition, out float strength)
    {
        strength = 0f;
        
        if (!enableTerrainDetection || currentTerrain == null || terrainData == null)
            return "";
        
        // Convertit la position du monde en coordonnées de terrain
        Vector3 terrainPosition = worldPosition - currentTerrain.transform.position;
        Vector3 normalizedPos = new Vector3(
            terrainPosition.x / terrainData.size.x,
            0,
            terrainPosition.z / terrainData.size.z
        );
        
        // Vérifie que la position est dans les limites du terrain
        if (normalizedPos.x < 0 || normalizedPos.x > 1 || normalizedPos.z < 0 || normalizedPos.z > 1)
        {
            if (showTerrainDebug)
                Debug.Log("🏔️ Position hors du terrain");
            return "";
        }
        
        // Convertit en coordonnées d'alphamap
        int alphamapX = Mathf.FloorToInt(normalizedPos.x * (alphamapWidth - 1));
        int alphamapZ = Mathf.FloorToInt(normalizedPos.z * (alphamapHeight - 1));
        
        // Récupère les valeurs d'alphamap à cette position
        alphamaps = terrainData.GetAlphamaps(alphamapX, alphamapZ, 1, 1);
        
        if (useDominantLayerOnly)
        {
            // Trouve le layer dominant
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
                strength = maxBlend;
                string surfaceName = GetSurfaceNameForLayer(dominantLayer);
                
                if (showTerrainDebug)
                {
                    string layerName = terrainData.terrainLayers[dominantLayer]?.name ?? "Unknown";
                    Debug.Log($"🏔️ Layer dominant: {dominantLayer} ({layerName}) → '{surfaceName}' (force: {maxBlend:F2})");
                }
                
                return surfaceName;
            }
        }
        else
        {
            // Utilise le mélange de layers
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
            
            // Retourne la surface avec le blend le plus fort
            if (surfaceBlends.Count > 0)
            {
                var dominant = surfaceBlends.OrderByDescending(kvp => kvp.Value).First();
                strength = dominant.Value;
                
                if (showTerrainDebug)
                {
                    Debug.Log($"🏔️ Surface mélangée dominante: '{dominant.Key}' (force totale: {dominant.Value:F2})");
                }
                
                return dominant.Key;
            }
        }
        
        return "";
    }
    
    /// <summary>
    /// Obtient le nom de surface pour un index de layer
    /// </summary>
    string GetSurfaceNameForLayer(int layerIndex)
    {
        // Vérifie d'abord le mapping configuré
        if (layerIndexToSurface.TryGetValue(layerIndex, out string mappedName))
        {
            return mappedName;
        }
        
        // Sinon, essaie de déduire depuis le nom du TerrainLayer
        if (layerIndex < terrainData.terrainLayers.Length && 
            terrainData.terrainLayers[layerIndex] != null)
        {
            string layerName = terrainData.terrainLayers[layerIndex].name.ToLower();
            
            // Recherche par mots-clés
            if (layerName.Contains("grass") || layerName.Contains("herbe")) return "grass";
            if (layerName.Contains("dirt") || layerName.Contains("terre")) return "dirt";
            if (layerName.Contains("stone") || layerName.Contains("pierre")) return "stone";
            if (layerName.Contains("sand") || layerName.Contains("sable")) return "sand";
            if (layerName.Contains("rock") || layerName.Contains("rocher")) return "stone";
            if (layerName.Contains("snow") || layerName.Contains("neige")) return "snow";
            if (layerName.Contains("mud") || layerName.Contains("boue")) return "mud";
            if (layerName.Contains("water") || layerName.Contains("eau")) return "water";
            if (layerName.Contains("metal")) return "metal";
            if (layerName.Contains("wood") || layerName.Contains("bois")) return "wood";
        }
        
        return ""; // Pas de correspondance trouvée
    }
    
    /// <summary>
    /// Méthode appelée par FootstepSystem (ou autre) pour obtenir la surface actuelle
    /// </summary>
    public string GetCurrentTerrainSurface(Vector3 position)
    {
        float strength;
        return DetectTerrainSurface(position, out strength);
    }
    
    /// <summary>
    /// Affiche les informations de debug sur les layers à la position actuelle
    /// </summary>
    public void DebugLayersAtPosition(Vector3 worldPosition)
    {
        if (currentTerrain == null || terrainData == null) return;
        
        Vector3 terrainPosition = worldPosition - currentTerrain.transform.position;
        Vector3 normalizedPos = new Vector3(
            terrainPosition.x / terrainData.size.x,
            0,
            terrainPosition.z / terrainData.size.z
        );
        
        if (normalizedPos.x < 0 || normalizedPos.x > 1 || normalizedPos.z < 0 || normalizedPos.z > 1)
            return;
        
        int alphamapX = Mathf.FloorToInt(normalizedPos.x * (alphamapWidth - 1));
        int alphamapZ = Mathf.FloorToInt(normalizedPos.z * (alphamapHeight - 1));
        
        float[,,] debugAlphamaps = terrainData.GetAlphamaps(alphamapX, alphamapZ, 1, 1);
        
        Debug.Log($"🏔️ === Layers à la position {worldPosition} ===");
        for (int layer = 0; layer < terrainData.terrainLayers.Length; layer++)
        {
            float blend = debugAlphamaps[0, 0, layer];
            if (blend > 0.01f) // Affiche seulement les layers significatifs
            {
                string layerName = terrainData.terrainLayers[layer]?.name ?? "Unknown";
                string surfaceName = GetSurfaceNameForLayer(layer);
                Debug.Log($"   Layer {layer} ({layerName}): {blend:F2} → Surface: '{surfaceName}'");
            }
        }
    }
    
    void OnGUI()
    {
        if (!showTerrainDebug || !GlobalDebugManager.IsDebugEnabled(DebugSystem.Footstep)) return;
        
        if (currentTerrain == null) return;
        
        GUILayout.BeginArea(new Rect(320, 150, 350, 200));
        GUILayout.Label("=== TERRAIN LAYER DEBUG ===");
        
        if (terrainData != null)
        {
            GUILayout.Label($"Terrain: {currentTerrain.name}");
            GUILayout.Label($"Layers: {terrainData.terrainLayers.Length}");
            
            // Affiche les layers à la position actuelle
            float strength;
            string currentSurface = DetectTerrainSurface(transform.position, out strength);
            GUILayout.Label($"Surface actuelle: {currentSurface} ({strength:F2})");
            
            GUILayout.Space(10);
            
            // Affiche tous les layers avec leur blend
            if (alphamaps != null && GUILayout.Button("Debug layers ici"))
            {
                DebugLayersAtPosition(transform.position);
            }
        }
        else
        {
            GUILayout.Label("Aucun terrain trouvé");
        }
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Structure pour mapper les indices de layer aux noms de surface
    /// </summary>
    [System.Serializable]
    public class TerrainLayerMapping
    {
        [Tooltip("Index du layer dans le terrain (0, 1, 2, etc.)")]
        public int layerIndex;
        
        [Tooltip("Nom de la surface correspondante (grass, dirt, stone, etc.)")]
        public string surfaceName;
        
        [Tooltip("Description pour l'éditeur")]
        public string description;
        
        public TerrainLayerMapping(int index, string surface, string desc = "")
        {
            layerIndex = index;
            surfaceName = surface;
            description = desc;
        }
    }
}
