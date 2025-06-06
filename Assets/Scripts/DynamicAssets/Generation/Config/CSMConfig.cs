using UnityEngine;

namespace DynamicAssets.Generation.Config
{
    /// <summary>
    /// Configuration pour l'API CSM (Common Sense Machines)
    /// Stocke tous les paramètres nécessaires pour la génération 3D
    /// </summary>
    [CreateAssetMenu(fileName = "CSMConfig", menuName = "Dynamic Assets/CSM Configuration")]
    public class CSMConfig : ScriptableObject
    {
        [Header("API Authentication")]
        [Tooltip("Clé API CSM - À obtenir sur le site de CSM")]
        public string apiKey = "";
        
        [Tooltip("URL de l'API CSM")]
        public string apiUrl = "https://api.csm.ai/generate"; // URL hypothétique
        
        [Header("Generation Settings")]
        [Tooltip("Style par défaut pour tous les modèles")]
        public string defaultStyle = "low-poly game asset, clean textures, optimized for real-time rendering";
        
        [Range(100, 5000)]
        [Tooltip("Nombre maximum de triangles par modèle")]
        public int maxTriangles = 1000;
        
        [Range(0.1f, 2f)]
        [Tooltip("Facteur d'échelle des modèles générés")]
        public float modelScale = 1f;
        
        [Header("Quality Settings")]
        [Tooltip("Qualité par défaut des modèles")]
        public ModelQuality defaultQuality = ModelQuality.Medium;
        
        [Range(256, 2048)]
        [Tooltip("Résolution des textures")]
        public int textureResolution = 512;
        
        [Tooltip("Génère automatiquement les textures")]
        public bool generateTextures = true;
        
        [Header("Output Settings")]
        [Tooltip("Format de sortie préféré")]
        public ModelFormat outputFormat = ModelFormat.FBX;
        
        [Tooltip("Génère automatiquement les LODs")]
        public bool generateLODs = false;
        
        [Header("Timeout Settings")]
        [Range(30f, 600f)]
        [Tooltip("Temps maximum d'attente pour la génération (secondes)")]
        public float generationTimeout = 300f; // 5 minutes
        
        [Range(10f, 120f)]
        [Tooltip("Temps maximum d'attente pour le téléchargement (secondes)")]
        public float downloadTimeout = 60f;
        
        [Header("Advanced Settings")]
        [Tooltip("Nombre de tentatives en cas d'échec")]
        [Range(1, 5)]
        public int retryAttempts = 2;
        
        [Tooltip("Délai entre les tentatives (secondes)")]
        [Range(5f, 30f)]
        public float retryDelay = 10f;
        
        [Tooltip("Mode debug pour l'API")]
        public bool debugMode = true;
        
        [Header("Prompts Enhancement")]
        [Tooltip("Amélioration automatique des prompts")]
        public bool enhancePrompts = true;
        
        [TextArea(2, 4)]
        [Tooltip("Suffixe ajouté à tous les prompts")]
        public string promptSuffix = ", highly detailed, professional quality, game ready";
        
        [Header("Cost Management")]
        [Tooltip("Coût estimé par génération (pour suivi)")]
        public float estimatedCostPerGeneration = 0.50f;
        
        [Tooltip("Budget maximum par jour")]
        public float dailyBudgetLimit = 20f;
        
        /// <summary>
        /// Valide que la configuration est complète
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("❌ Clé API CSM manquante");
                return false;
            }
            
            if (string.IsNullOrEmpty(apiUrl))
            {
                Debug.LogWarning("❌ URL API CSM manquante");
                return false;
            }
            
            if (maxTriangles <= 0)
            {
                Debug.LogWarning("❌ Nombre de triangles invalide");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Génère les paramètres complets pour une requête CSM
        /// </summary>
        public CSMRequestParams GetRequestParams(string prompt)
        {
            return new CSMRequestParams
            {
                prompt = enhancePrompts ? $"{prompt}{promptSuffix}" : prompt,
                style = defaultStyle,
                maxTriangles = maxTriangles,
                textureResolution = textureResolution,
                generateTextures = generateTextures,
                outputFormat = outputFormat,
                quality = defaultQuality,
                scale = modelScale,
                generateLODs = generateLODs
            };
        }
        
        /// <summary>
        /// Affiche un résumé de la configuration
        /// </summary>
        [ContextMenu("Show Config Summary")]
        public void ShowConfigSummary()
        {
            Debug.Log($@"📋 CONFIGURATION CSM
API Key: {(string.IsNullOrEmpty(apiKey) ? "❌ Non configurée" : "✅ Configurée")}
API URL: {apiUrl}
Style: {defaultStyle}
Triangles Max: {maxTriangles}
Texture Res: {textureResolution}px
Timeout: {generationTimeout}s
Format: {outputFormat}
Qualité: {defaultQuality}
Budget/jour: ${dailyBudgetLimit}
Debug: {(debugMode ? "✅" : "❌")}
Valid: {(IsValid() ? "✅" : "❌")}");
        }
        
        /// <summary>
        /// Réinitialise aux valeurs par défaut
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            apiKey = "";
            apiUrl = "https://api.csm.ai/generate";
            defaultStyle = "low-poly game asset, clean textures, optimized for real-time rendering";
            maxTriangles = 1000;
            modelScale = 1f;
            defaultQuality = ModelQuality.Medium;
            textureResolution = 512;
            generateTextures = true;
            outputFormat = ModelFormat.FBX;
            generateLODs = false;
            generationTimeout = 300f;
            downloadTimeout = 60f;
            retryAttempts = 2;
            retryDelay = 10f;
            debugMode = true;
            enhancePrompts = true;
            promptSuffix = ", highly detailed, professional quality, game ready";
            estimatedCostPerGeneration = 0.50f;
            dailyBudgetLimit = 20f;
            
            Debug.Log("✅ Configuration réinitialisée aux valeurs par défaut");
        }
    }
    
    /// <summary>
    /// Niveaux de qualité des modèles
    /// </summary>
    [System.Serializable]
    public enum ModelQuality
    {
        Low,        // Rapide, moins détaillé
        Medium,     // Équilibre qualité/vitesse
        High,       // Haute qualité, plus lent
        Ultra       // Qualité maximale
    }
    
    /// <summary>
    /// Formats de sortie supportés
    /// </summary>
    [System.Serializable]
    public enum ModelFormat
    {
        FBX,        // Format le plus compatible
        OBJ,        // Format simple
        GLB,        // Format web/mobile
        USD         // Format haute gamme
    }
    
    /// <summary>
    /// Paramètres compilés pour une requête CSM
    /// </summary>
    [System.Serializable]
    public class CSMRequestParams
    {
        public string prompt;
        public string style;
        public int maxTriangles;
        public int textureResolution;
        public bool generateTextures;
        public ModelFormat outputFormat;
        public ModelQuality quality;
        public float scale;
        public bool generateLODs;
        
        /// <summary>
        /// Convertit en JSON pour l'API
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
    }
}