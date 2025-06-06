using UnityEngine;
using DynamicAssets.Generation.Config;

namespace DynamicAssets.Generation.API
{
    /// <summary>
    /// Structure de données pour les requêtes vers l'API CSM
    /// Correspond au format JSON attendu par l'API
    /// </summary>
    [System.Serializable]
    public class CSMRequest
    {
        [Header("Core Parameters")]
        [Tooltip("Description textuelle de l'objet à générer")]
        public string prompt;
        
        [Tooltip("Style artistique du modèle")]
        public string style;
        
        [Header("Technical Specifications")]
        [Tooltip("Nombre maximum de triangles")]
        public int max_triangles = 1000;
        
        [Tooltip("Résolution des textures")]
        public int texture_resolution = 512;
        
        [Tooltip("Format de sortie du modèle")]
        public string output_format = "fbx";
        
        [Header("Generation Options")]
        [Tooltip("Génère automatiquement les textures")]
        public bool generate_textures = true;
        
        [Tooltip("Génère plusieurs niveaux de détail")]
        public bool generate_lods = false;
        
        [Tooltip("Optimise pour le temps réel")]
        public bool optimize_for_realtime = true;
        
        [Header("Quality Settings")]
        [Tooltip("Niveau de qualité (low, medium, high, ultra)")]
        public string quality = "medium";
        
        [Tooltip("Niveau de détail (0.1 à 2.0)")]
        [Range(0.1f, 2f)]
        public float detail_level = 1f;
        
        [Header("Metadata")]
        [Tooltip("ID unique pour cette requête")]
        public string request_id;
        
        [Tooltip("Timestamp de la requête")]
        public string timestamp;
        
        [Tooltip("Nom de l'objet pour référence")]
        public string object_name;
        
        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public CSMRequest()
        {
            request_id = System.Guid.NewGuid().ToString();
            timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        
        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public CSMRequest(string prompt, string objectName, CSMConfig config)
        {
            this.prompt = prompt;
            this.object_name = objectName;
            this.style = config.defaultStyle;
            this.max_triangles = config.maxTriangles;
            this.texture_resolution = config.textureResolution;
            this.output_format = config.outputFormat.ToString().ToLower();
            this.generate_textures = config.generateTextures;
            this.generate_lods = config.generateLODs;
            this.quality = config.defaultQuality.ToString().ToLower();
            
            // Métadonnées
            this.request_id = System.Guid.NewGuid().ToString();
            this.timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        
        /// <summary>
        /// Convertit la requête en JSON pour l'API
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
        
        /// <summary>
        /// Crée une requête depuis un CSMConfig
        /// </summary>
        public static CSMRequest FromConfig(string prompt, string objectName, CSMConfig config)
        {
            if (config == null)
            {
                Debug.LogError("❌ CSMConfig est null !");
                return null;
            }
            
            return new CSMRequest(prompt, objectName, config);
        }
        
        /// <summary>
        /// Valide que la requête est complète
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(prompt))
            {
                Debug.LogWarning("❌ Prompt manquant dans la requête CSM");
                return false;
            }
            
            if (string.IsNullOrEmpty(object_name))
            {
                Debug.LogWarning("❌ Nom d'objet manquant dans la requête CSM");
                return false;
            }
            
            if (max_triangles <= 0)
            {
                Debug.LogWarning("❌ Nombre de triangles invalide");
                return false;
            }
            
            if (texture_resolution < 256 || texture_resolution > 2048)
            {
                Debug.LogWarning("❌ Résolution de texture invalide");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Estime la taille du modèle généré (pour planification)
        /// </summary>
        public float EstimateFileSizeMB()
        {
            // Estimation très approximative basée sur les paramètres
            float baseSize = max_triangles * 0.0001f; // ~0.1 MB par 1000 triangles
            
            if (generate_textures)
            {
                float textureSize = (texture_resolution * texture_resolution * 4) / (1024f * 1024f); // RGBA
                baseSize += textureSize * 2; // Diffuse + Normal approximativement
            }
            
            if (generate_lods)
                baseSize *= 1.5f; // LODs ajoutent ~50%
                
            return baseSize;
        }
        
        /// <summary>
        /// Estime le temps de génération (pour interface utilisateur)
        /// </summary>
        public float EstimateGenerationTimeSeconds()
        {
            // Estimation basée sur la complexité
            float baseTime = 60f; // 1 minute de base
            
            // Plus de triangles = plus de temps
            if (max_triangles > 1500)
                baseTime += 30f;
            else if (max_triangles < 500)
                baseTime -= 15f;
            
            // Textures haute résolution = plus de temps
            if (texture_resolution > 1024)
                baseTime += 45f;
            else if (texture_resolution < 512)
                baseTime -= 15f;
            
            // Qualité = temps
            switch (quality.ToLower())
            {
                case "low": baseTime *= 0.7f; break;
                case "medium": baseTime *= 1f; break;
                case "high": baseTime *= 1.4f; break;
                case "ultra": baseTime *= 2f; break;
            }
            
            if (generate_lods)
                baseTime += 30f;
                
            return baseTime;
        }
        
        /// <summary>
        /// Génère un résumé lisible de la requête
        /// </summary>
        public override string ToString()
        {
            return $@"CSM Request [{request_id.Substring(0, 8)}]
Object: {object_name}
Prompt: {prompt}
Triangles: {max_triangles}
Texture: {texture_resolution}px
Quality: {quality}
Est. Time: {EstimateGenerationTimeSeconds():F0}s
Est. Size: {EstimateFileSizeMB():F1}MB";
        }
    }
}