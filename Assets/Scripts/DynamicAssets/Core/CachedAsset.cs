using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicAssets.Core
{
    /// <summary>
    /// Représente un asset 3D mis en cache (généré ou manuel)
    /// </summary>
    [System.Serializable]
    public class CachedAsset
    {
        [Header("Asset Identity")]
        public string itemName;           // Nom technique (ex: "cristal_energie")
        public string displayName;        // Nom affiché (ex: "Cristal d'Énergie")
        public string assetId;            // ID unique pour éviter les doublons
        
        [Header("File Paths")]
        public string prefabPath;         // Chemin vers le .prefab Unity
        public string modelPath;          // Chemin vers le .fbx/.obj source
        public string texturesFolder;     // Dossier des textures associées
        
        [Header("Generation Info")]
        public bool isGenerated;          // true = généré par IA, false = créé manuellement
        public string originalPrompt;     // Prompt utilisé pour la génération
        public string generationMethod;   // "CSM", "Manual", "Meshy", etc.
        public DateTime createdDate;
        public DateTime lastUsed;         // Pour cleanup automatique
        
        [Header("Technical Info")]
        public int triangleCount;         // Nombre de triangles du modèle
        public string modelHash;          // Hash MD5 du fichier pour détecter les changements
        public Vector3 modelBounds;       // Taille approximative du modèle
        public AssetQuality quality;      // Qualité du modèle
        
        [Header("Usage Stats")]
        public int usageCount;            // Combien de fois utilisé
        public float averageLoadTime;     // Temps de chargement moyen
        public AssetStatus status;        // État actuel de l'asset
        
        /// <summary>
        /// Constructeur pour assets générés
        /// </summary>
        public static CachedAsset CreateGenerated(string itemName, string prompt, string method = "CSM")
        {
            var asset = new CachedAsset();
            asset.itemName = itemName;
            asset.displayName = asset.FormatDisplayName(itemName);
            asset.assetId = asset.GenerateAssetId(itemName);
            asset.originalPrompt = prompt;
            asset.generationMethod = method;
            asset.isGenerated = true;
            asset.createdDate = DateTime.Now;
            asset.lastUsed = DateTime.Now;
            asset.status = AssetStatus.Generating;
            asset.quality = AssetQuality.Medium;
            asset.usageCount = 0;
            return asset;
        }
        
        /// <summary>
        /// Constructeur pour assets manuels
        /// </summary>
        public static CachedAsset CreateManual(string itemName, string prefabPath, string modelPath)
        {
            var asset = new CachedAsset();
            asset.itemName = itemName;
            asset.displayName = asset.FormatDisplayName(itemName);
            asset.assetId = asset.GenerateAssetId(itemName);
            asset.prefabPath = prefabPath;
            asset.modelPath = modelPath;
            asset.isGenerated = false;
            asset.generationMethod = "Manual";
            asset.createdDate = DateTime.Now;
            asset.lastUsed = DateTime.Now;
            asset.status = AssetStatus.Ready;
            asset.quality = AssetQuality.High;
            asset.usageCount = 0;
            return asset;
        }
        
        /// <summary>
        /// Constructeur vide pour désérialisation JSON
        /// </summary>
        public CachedAsset() { }
        
        // ========== MÉTHODES UTILITAIRES ==========
        
        /// <summary>
        /// Génère un ID unique basé sur le nom de l'item
        /// </summary>
        string GenerateAssetId(string itemName)
        {
            string timestamp = DateTime.Now.Ticks.ToString();
            return $"{itemName}_{timestamp.Substring(timestamp.Length - 8)}";
        }
        
        /// <summary>
        /// Convertit un nom technique en nom d'affichage
        /// </summary>
        string FormatDisplayName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return "Asset Inconnu";
            
            // cristal_energie → Cristal d'Énergie
            string formatted = itemName.Replace('_', ' ');
            
            // Capitalize each word
            string[] words = formatted.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            
            return string.Join(" ", words);
        }
        
        /// <summary>
        /// Met à jour les statistiques d'usage
        /// </summary>
        public void RecordUsage(float loadTime = 0f)
        {
            usageCount++;
            lastUsed = DateTime.Now;
            
            if (loadTime > 0f)
            {
                // Calcul de la moyenne mobile
                averageLoadTime = (averageLoadTime * (usageCount - 1) + loadTime) / usageCount;
            }
        }
        
        /// <summary>
        /// Vérifie si l'asset est valide et utilisable
        /// </summary>
        public bool IsValid()
        {
            return status == AssetStatus.Ready && 
                   !string.IsNullOrEmpty(prefabPath) && 
                   !string.IsNullOrEmpty(itemName);
        }
        
        /// <summary>
        /// Calcule l'âge de l'asset en jours
        /// </summary>
        public int GetAgeInDays()
        {
            return (DateTime.Now - createdDate).Days;
        }
        
        /// <summary>
        /// Détermine si l'asset devrait être nettoyé (vieux et peu utilisé)
        /// </summary>
        public bool ShouldCleanup(int maxAgeInDays = 30, int minUsageCount = 1)
        {
            return GetAgeInDays() > maxAgeInDays && usageCount < minUsageCount;
        }
        
        /// <summary>
        /// Retourne un résumé de l'asset pour debug
        /// </summary>
        public override string ToString()
        {
            return $"{displayName} [{assetId}] - {status} ({(isGenerated ? "Generated" : "Manual")})";
        }
    }
    
    /// <summary>
    /// États possibles d'un asset
    /// </summary>
    [System.Serializable]
    public enum AssetStatus
    {
        Generating,      // En cours de génération
        Downloading,     // En cours de téléchargement
        Importing,       // En cours d'import dans Unity
        Ready,           // Prêt à utiliser
        Error,           // Erreur lors de la génération/import
        Outdated,        // Version obsolète disponible
        Missing          // Fichiers manquants sur disque
    }
    
    /// <summary>
    /// Niveaux de qualité des assets
    /// </summary>
    [System.Serializable]
    public enum AssetQuality
    {
        Low,             // < 500 triangles, textures 256x256
        Medium,          // 500-1500 triangles, textures 512x512
        High,            // 1500-3000 triangles, textures 1024x1024
        Ultra            // > 3000 triangles, textures 2048x2048+
    }
    
    /// <summary>
    /// Container principal pour sauvegarder tous les assets en cache
    /// </summary>
    [System.Serializable]
    public class AssetCacheData
    {
        [Header("Cache Info")]
        public string version = "1.0";
        public DateTime lastUpdated;
        public int totalAssets;
        
        [Header("Assets")]
        public List<CachedAsset> cachedAssets = new List<CachedAsset>();
        
        [Header("Statistics")]
        public int generatedAssetsCount;
        public int manualAssetsCount;
        public float totalCacheSizeMB;
        public DateTime lastCleanup;
        
        /// <summary>
        /// Constructeur
        /// </summary>
        public AssetCacheData()
        {
            lastUpdated = DateTime.Now;
            lastCleanup = DateTime.Now;
        }
        
        /// <summary>
        /// Ajoute un asset au cache
        /// </summary>
        public void AddAsset(CachedAsset asset)
        {
            if (asset == null) return;
            
            // Évite les doublons
            RemoveAsset(asset.itemName);
            
            cachedAssets.Add(asset);
            UpdateStatistics();
        }
        
        /// <summary>
        /// Retire un asset du cache
        /// </summary>
        public bool RemoveAsset(string itemName)
        {
            for (int i = cachedAssets.Count - 1; i >= 0; i--)
            {
                if (cachedAssets[i].itemName == itemName)
                {
                    cachedAssets.RemoveAt(i);
                    UpdateStatistics();
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Trouve un asset par nom
        /// </summary>
        public CachedAsset FindAsset(string itemName)
        {
            return cachedAssets.Find(a => a.itemName == itemName);
        }
        
        /// <summary>
        /// Met à jour les statistiques du cache
        /// </summary>
        public void UpdateStatistics()
        {
            totalAssets = cachedAssets.Count;
            generatedAssetsCount = cachedAssets.FindAll(a => a.isGenerated).Count;
            manualAssetsCount = totalAssets - generatedAssetsCount;
            lastUpdated = DateTime.Now;
        }
        
        /// <summary>
        /// Nettoie les assets obsolètes
        /// </summary>
        public int CleanupOldAssets(int maxAgeInDays = 30, int minUsageCount = 1)
        {
            int removed = 0;
            for (int i = cachedAssets.Count - 1; i >= 0; i--)
            {
                if (cachedAssets[i].ShouldCleanup(maxAgeInDays, minUsageCount))
                {
                    cachedAssets.RemoveAt(i);
                    removed++;
                }
            }
            
            if (removed > 0)
            {
                lastCleanup = DateTime.Now;
                UpdateStatistics();
            }
            
            return removed;
        }
        
        /// <summary>
        /// Retourne les assets les plus utilisés
        /// </summary>
        public List<CachedAsset> GetMostUsedAssets(int count = 10)
        {
            List<CachedAsset> sorted = new List<CachedAsset>(cachedAssets);
            sorted.Sort((a, b) => b.usageCount.CompareTo(a.usageCount));
            
            if (sorted.Count > count)
                sorted.RemoveRange(count, sorted.Count - count);
                
            return sorted;
        }
    }
}