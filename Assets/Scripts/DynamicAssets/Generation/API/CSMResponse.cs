using UnityEngine;

namespace DynamicAssets.Generation.API
{
    /// <summary>
    /// Structure de données pour les réponses de l'API CSM
    /// Correspond au format JSON renvoyé par l'API
    /// </summary>
    [System.Serializable]
    public class CSMResponse
    {
        [Header("Response Status")]
        [Tooltip("Statut de la génération")]
        public string status; // "success", "processing", "failed", "queued"
        
        [Tooltip("Message détaillé du statut")]
        public string message;
        
        [Tooltip("Code d'erreur si applicable")]
        public int error_code = 0;
        
        [Header("Generation Results")]
        [Tooltip("ID unique de la génération")]
        public string generation_id;
        
        [Tooltip("URL de téléchargement du modèle")]
        public string download_url;
        
        [Tooltip("URL de prévisualisation (image)")]
        public string preview_url;
        
        [Header("Model Information")]
        [Tooltip("Nombre réel de triangles générés")]
        public int actual_triangles;
        
        [Tooltip("Taille du fichier en bytes")]
        public long file_size_bytes;
        
        [Tooltip("Format réel du fichier")]
        public string file_format;
        
        [Header("Timing")]
        [Tooltip("Temps de génération en secondes")]
        public float generation_time_seconds;
        
        [Tooltip("Timestamp de début")]
        public string started_at;
        
        [Tooltip("Timestamp de fin")]
        public string completed_at;
        
        [Header("Quality Metrics")]
        [Tooltip("Score de qualité (0-100)")]
        [Range(0, 100)]
        public int quality_score = 0;
        
        [Tooltip("Niveau de correspondance au prompt (0-100)")]
        [Range(0, 100)]
        public int prompt_match_score = 0;
        
        [Header("Additional Data")]
        [Tooltip("URLs des textures générées")]
        public string[] texture_urls;
        
        [Tooltip("URLs des fichiers LOD")]
        public string[] lod_urls;
        
        [Tooltip("Métadonnées supplémentaires")]
        public string metadata_json;
        
        /// <summary>
        /// Vérifie si la génération est terminée avec succès
        /// </summary>
        public bool IsSuccess()
        {
            return status?.ToLower() == "success" || status?.ToLower() == "completed";
        }
        
        /// <summary>
        /// Vérifie si la génération est encore en cours
        /// </summary>
        public bool IsProcessing()
        {
            return status?.ToLower() == "processing" || 
                   status?.ToLower() == "queued" || 
                   status?.ToLower() == "generating";
        }
        
        /// <summary>
        /// Vérifie si la génération a échoué
        /// </summary>
        public bool IsFailed()
        {
            return status?.ToLower() == "failed" || 
                   status?.ToLower() == "error" || 
                   error_code > 0;
        }
        
        /// <summary>
        /// Vérifie si le téléchargement est possible
        /// </summary>
        public bool CanDownload()
        {
            return IsSuccess() && !string.IsNullOrEmpty(download_url);
        }
        
        /// <summary>
        /// Convertit la taille de fichier en format lisible
        /// </summary>
        public string GetFormattedFileSize()
        {
            if (file_size_bytes < 1024)
                return $"{file_size_bytes} B";
            else if (file_size_bytes < 1024 * 1024)
                return $"{file_size_bytes / 1024f:F1} KB";
            else
                return $"{file_size_bytes / (1024f * 1024f):F1} MB";
        }
        
        /// <summary>
        /// Retourne un message d'état lisible
        /// </summary>
        public string GetStatusMessage()
        {
            switch (status?.ToLower())
            {
                case "success":
                case "completed":
                    return $"✅ Génération terminée ({generation_time_seconds:F1}s)";
                    
                case "processing":
                case "generating":
                    return "🎨 Génération en cours...";
                    
                case "queued":
                    return "⏳ En file d'attente...";
                    
                case "failed":
                case "error":
                    return $"❌ Échec: {message ?? "Erreur inconnue"}";
                    
                default:
                    return $"❓ Statut inconnu: {status}";
            }
        }
        
        /// <summary>
        /// Crée une réponse depuis du JSON
        /// </summary>
        public static CSMResponse FromJson(string jsonString)
        {
            try
            {
                return JsonUtility.FromJson<CSMResponse>(jsonString);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erreur parsing CSMResponse: {e.Message}");
                return CreateErrorResponse($"Erreur parsing JSON: {e.Message}");
            }
        }
        
        /// <summary>
        /// Crée une réponse d'erreur
        /// </summary>
        public static CSMResponse CreateErrorResponse(string errorMessage, int errorCode = -1)
        {
            return new CSMResponse
            {
                status = "failed",
                message = errorMessage,
                error_code = errorCode,
                generation_id = System.Guid.NewGuid().ToString(),
                started_at = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                completed_at = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }
        
        /// <summary>
        /// Crée une réponse de succès de test
        /// </summary>
        public static CSMResponse CreateTestSuccessResponse(string testObjectName)
        {
            return new CSMResponse
            {
                status = "success",
                message = "Test generation completed",
                generation_id = System.Guid.NewGuid().ToString(),
                download_url = $"https://fake-csm-api.com/download/{testObjectName}.fbx",
                preview_url = $"https://fake-csm-api.com/preview/{testObjectName}.jpg",
                actual_triangles = Random.Range(800, 1200),
                file_size_bytes = Random.Range(500000, 2000000), // 0.5-2MB
                file_format = "fbx",
                generation_time_seconds = Random.Range(45f, 180f),
                started_at = System.DateTime.UtcNow.AddSeconds(-Random.Range(45, 180)).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                completed_at = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                quality_score = Random.Range(75, 95),
                prompt_match_score = Random.Range(80, 98)
            };
        }
        
        /// <summary>
        /// Valide que la réponse est cohérente
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(status))
            {
                Debug.LogWarning("❌ CSMResponse sans statut");
                return false;
            }
            
            if (IsSuccess() && string.IsNullOrEmpty(download_url))
            {
                Debug.LogWarning("❌ Réponse 'success' sans URL de téléchargement");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Génère un résumé de la réponse
        /// </summary>
        public override string ToString()
        {
            return $@"CSM Response [{generation_id?.Substring(0, 8) ?? "null"}]
Status: {GetStatusMessage()}
Download: {(string.IsNullOrEmpty(download_url) ? "❌" : "✅")}
Triangles: {actual_triangles:N0}
Size: {GetFormattedFileSize()}
Quality: {quality_score}/100
Match: {prompt_match_score}/100";
        }
    }
}