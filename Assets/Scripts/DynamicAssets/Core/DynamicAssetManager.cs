using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using DynamicAssets.Core;

namespace DynamicAssets.Core
{
    /// <summary>
    /// Gestionnaire principal pour les assets dynamiques
    /// Gère le cache, le chargement et la génération d'assets 3D
    /// </summary>
    public class DynamicAssetManager : MonoBehaviour
    {
        public static DynamicAssetManager Instance { get; private set; }
        
        [Header("Cache Configuration")]
        public string cacheFolder = "GeneratedAssets/Cache/";
        public string prefabFolder = "GeneratedAssets/Prefabs/";
        public string modelFolder = "GeneratedAssets/Models/";
        public string cacheFileName = "asset_cache.json";
        
        [Header("Fallback Assets")]
        public GameObject defaultItemPrefab;       // Prefab par défaut pour objets
        public GameObject defaultNPCPrefab;        // Prefab par défaut pour NPCs
        public GameObject defaultTerminalPrefab;   // Prefab par défaut pour terminaux
        public GameObject defaultMarkerPrefab;     // Prefab par défaut pour marqueurs
        
        [Header("Generation Settings")]
        public bool enableAutoGeneration = false;  // Désactivé pour Phase 1
        public float maxLoadTimeSeconds = 30f;
        public int maxCacheSize = 100;
        
        [Header("Debug")]
        public bool showDetailedLogs = false;
        // Debug principal est maintenant géré par GlobalDebugManager
        
        // Cache en mémoire
        private Dictionary<string, GameObject> loadedPrefabs = new Dictionary<string, GameObject>();
        private AssetCacheData cacheData;
        private string fullCachePath;
        
        // États
        private bool isInitialized = false;
        private List<string> currentlyLoading = new List<string>();
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Initialise le gestionnaire
        /// </summary>
        void InitializeManager()
        {
            try
            {
                // Prépare les chemins
                SetupPaths();
                
                // Charge le cache existant
                LoadCacheFromDisk();
                
                // Valide les assets en cache
                ValidateCache();
                
                isInitialized = true;
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log($"✅ DynamicAssetManager initialisé - {cacheData.totalAssets} assets en cache");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Erreur initialisation DynamicAssetManager: {e.Message}");
                isInitialized = false;
            }
        }
        
        /// <summary>
        /// Configure les chemins de fichiers - VERSION CORRIGÉE
        /// </summary>
        void SetupPaths()
        {
            // CORRECTION : Utilise des chemins relatifs au projet Unity
            string projectPath = Application.dataPath.Replace("/Assets", "");
            
            // Assure-toi que les dossiers existent
            string fullCacheFolder = Path.Combine(projectPath, "Assets", cacheFolder);
            string fullPrefabFolder = Path.Combine(projectPath, "Assets", prefabFolder);
            string fullModelFolder = Path.Combine(projectPath, "Assets", modelFolder);
            
            try
            {
                Directory.CreateDirectory(fullCacheFolder);
                Directory.CreateDirectory(fullPrefabFolder);
                Directory.CreateDirectory(fullModelFolder);
                
                fullCachePath = Path.Combine(fullCacheFolder, cacheFileName);
                
                // Vérification du chemin
                if (string.IsNullOrEmpty(fullCachePath))
                {
                    Debug.LogError("❌ Chemin de cache vide !");
                    fullCachePath = Path.Combine(Application.persistentDataPath, cacheFileName);
                    Debug.Log($"🔧 Utilisation chemin alternatif: {fullCachePath}");
                }
                
                if (showDetailedLogs)
                {
                    Debug.Log($"📁 Chemins configurés:");
                    Debug.Log($"  Cache: {fullCachePath}");
                    Debug.Log($"  Prefabs: {fullPrefabFolder}");
                    Debug.Log($"  Models: {fullModelFolder}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erreur configuration chemins: {e.Message}");
                
                // Fallback vers persistentDataPath
                fullCachePath = Path.Combine(Application.persistentDataPath, cacheFileName);
                Debug.Log($"🔧 Utilisation chemin de secours: {fullCachePath}");
            }
        }
        
        /// <summary>
        /// Charge le cache depuis le disque
        /// </summary>
        void LoadCacheFromDisk()
        {
            if (File.Exists(fullCachePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(fullCachePath);
                    cacheData = JsonUtility.FromJson<AssetCacheData>(jsonContent);
                    
                    if (cacheData == null)
                        cacheData = new AssetCacheData();
                    
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                        Debug.Log($"📂 Cache chargé: {cacheData.totalAssets} assets");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Erreur lecture cache: {e.Message}. Création nouveau cache.");
                    cacheData = new AssetCacheData();
                }
            }
            else
            {
                cacheData = new AssetCacheData();
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log("📂 Nouveau cache créé");
            }
        }
        
        /// <summary>
        /// Valide que les assets en cache existent toujours
        /// </summary>
        void ValidateCache()
        {
            if (cacheData == null) return;
            
            int removedCount = 0;
            for (int i = cacheData.cachedAssets.Count - 1; i >= 0; i--)
            {
                CachedAsset asset = cacheData.cachedAssets[i];
                
                // Vérifie si le prefab existe
                if (!string.IsNullOrEmpty(asset.prefabPath))
                {
                    string fullPath = Path.Combine(Application.dataPath, asset.prefabPath.Replace("Assets/", ""));
                    if (!File.Exists(fullPath))
                    {
                        asset.status = AssetStatus.Missing;
                        if (showDetailedLogs)
                            Debug.LogWarning($"⚠️ Asset manquant: {asset.itemName} ({asset.prefabPath})");
                    }
                }
            }
            
            // Nettoie les assets invalides
            cacheData.cachedAssets.RemoveAll(a => a.status == AssetStatus.Missing);
            
            if (removedCount > 0)
            {
                cacheData.UpdateStatistics();
                SaveCacheToDisk();
                Debug.Log($"🧹 {removedCount} assets invalides supprimés du cache");
            }
        }
        
        /// <summary>
        /// MÉTHODE PRINCIPALE - Récupère un prefab d'objet de quête
        /// </summary>
        public async Task<GameObject> GetQuestItemPrefab(string itemName, QuestObjectType objectType = QuestObjectType.Item)
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ DynamicAssetManager non initialisé !");
                return GetFallbackPrefab(objectType);
            }
            
            if (showDetailedLogs)
                Debug.Log($"🔍 Recherche prefab pour: {itemName} ({objectType})");
            
            try
            {
                // 1. Check cache mémoire
                if (loadedPrefabs.ContainsKey(itemName))
                {
                    if (showDetailedLogs)
                        Debug.Log($"✅ Trouvé en mémoire: {itemName}");
                    return loadedPrefabs[itemName];
                }
                
                // 2. Check cache disque
                CachedAsset cachedAsset = cacheData?.FindAsset(itemName);
                if (cachedAsset != null && cachedAsset.IsValid())
                {
                    GameObject prefab = await LoadPrefabFromCache(cachedAsset);
                    if (prefab != null)
                    {
                        loadedPrefabs[itemName] = prefab;
                        cachedAsset.RecordUsage();
                        
                        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                            Debug.Log($"✅ Chargé depuis cache: {itemName}");
                        return prefab;
                    }
                }
                
                // 3. TODO: Génération via CSM (Phase 2)
                if (enableAutoGeneration)
                {
                    return await GenerateNewAsset(itemName, objectType);
                }
                
                // 4. Fallback vers prefab par défaut
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log($"⚠️ Utilisation fallback pour: {itemName}");
                return GetFallbackPrefab(objectType);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Erreur récupération asset {itemName}: {e.Message}");
                return GetFallbackPrefab(objectType);
            }
        }
        
        /// <summary>
        /// Charge un prefab depuis le cache
        /// </summary>
        async Task<GameObject> LoadPrefabFromCache(CachedAsset asset)
        {
            if (currentlyLoading.Contains(asset.itemName))
            {
                // Évite les chargements multiples simultanés
                int waitCount = 0;
                while (currentlyLoading.Contains(asset.itemName) && waitCount < 100)
                {
                    await Task.Delay(100);
                    waitCount++;
                }
            }
            
            currentlyLoading.Add(asset.itemName);
            
            try
            {
                // Simule un délai de chargement asynchrone
                await Task.Delay(50);
                
                // En Phase 1, on utilise Resources.Load pour simuler
                // En Phase 2+, on utilisera AssetDatabase.LoadAssetAtPath
                GameObject prefab = null;
                
                if (!string.IsNullOrEmpty(asset.prefabPath))
                {
                    // TODO: Chargement réel du prefab
                    // prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset.prefabPath);
                    
                    if (showDetailedLogs)
                        Debug.Log($"📁 Chargement simulé: {asset.prefabPath}");
                }
                
                return prefab;
            }
            finally
            {
                currentlyLoading.Remove(asset.itemName);
            }
        }
        
        /// <summary>
        /// Génère un nouvel asset (Phase 2+)
        /// </summary>
        async Task<GameObject> GenerateNewAsset(string itemName, QuestObjectType objectType)
        {
            if (showDetailedLogs)
                Debug.Log($"🎨 Génération de {itemName} demandée (non implémenté en Phase 1)");
            
            // TODO: Phase 2 - Intégration CSM
            await Task.Delay(100);
            
            return GetFallbackPrefab(objectType);
        }
        
        /// <summary>
        /// Retourne le prefab de fallback approprié
        /// </summary>
        GameObject GetFallbackPrefab(QuestObjectType objectType)
        {
            return objectType switch
            {
                QuestObjectType.Item => defaultItemPrefab,
                QuestObjectType.NPC => defaultNPCPrefab,
                QuestObjectType.InteractableObject => defaultTerminalPrefab,
                QuestObjectType.Marker => defaultMarkerPrefab,
                _ => defaultItemPrefab
            };
        }
        
        /// <summary>
        /// Ajoute manuellement un asset au cache - VERSION CORRIGÉE
        /// </summary>
        public void AddAssetToCache(string itemName, GameObject prefab, string modelPath = "")
        {
            if (string.IsNullOrEmpty(itemName) || prefab == null)
            {
                Debug.LogWarning("⚠️ Tentative d'ajout d'asset invalide au cache");
                return;
            }
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
            {
                Debug.Log($"💾 === AJOUT CACHE DÉTAILLÉ ===");
                Debug.Log($"Item Name: {itemName}");
                Debug.Log($"Prefab: {prefab.name}");
                Debug.Log($"Model Path: {modelPath}");
                Debug.Log($"Cache Data avant: {cacheData?.totalAssets ?? 0} assets");
            }
            
            // Crée l'entrée de cache CORRECTEMENT
            CachedAsset asset = CachedAsset.CreateManual(itemName, "", modelPath);
            asset.status = AssetStatus.Ready;
            asset.displayName = FormatDisplayName(itemName);
            
            // CORRECTION : Ajoute au cache data ET en mémoire
            if (cacheData != null)
            {
                cacheData.AddAsset(asset);
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log($"✅ Asset ajouté à cacheData: {itemName}");
            }
            else
            {
                Debug.LogError("❌ cacheData est null !");
                return;
            }
            
            // Ajoute en mémoire
            loadedPrefabs[itemName] = prefab;
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"✅ Asset ajouté en mémoire: {itemName}");
            
            // FORCE la sauvegarde immédiate
            SaveCacheToDisk();
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"Cache Data après: {cacheData.totalAssets} assets");
            
            // Vérification immédiate
            if (cacheData.FindAsset(itemName) != null)
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log($"✅ VÉRIFICATION : {itemName} trouvé dans cacheData");
            }
            else
            {
                Debug.LogError($"❌ ÉCHEC VÉRIFICATION : {itemName} non trouvé dans cacheData");
            }
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"✅ Asset ajouté au cache avec succès: {itemName}");
        }
        
        /// <summary>
        /// Formate un nom technique en nom d'affichage
        /// </summary>
        private string FormatDisplayName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return "Asset Inconnu";
            
            // cristal_energie → Cristal Énergie
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
        /// Sauvegarde le cache sur disque - VERSION SÉCURISÉE
        /// </summary>
        public void SaveCacheToDisk()
        {
            try
            {
                if (string.IsNullOrEmpty(fullCachePath))
                {
                    Debug.LogError("❌ Chemin de cache non configuré - initialisation...");
                    SetupPaths();
                }
                
                if (string.IsNullOrEmpty(fullCachePath))
                {
                    Debug.LogError("❌ Impossible de configurer le chemin de cache");
                    return;
                }
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                {
                    Debug.Log($"💾 Sauvegarde cache vers: {fullCachePath}");
                }
                
                cacheData.UpdateStatistics();
                string jsonContent = JsonUtility.ToJson(cacheData, true);
                
                // Assure-toi que le dossier parent existe
                string directory = Path.GetDirectoryName(fullCachePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(fullCachePath, jsonContent);
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log($"✅ Cache sauvegardé avec succès: {cacheData.totalAssets} assets");
                
                if (showDetailedLogs)
                    Debug.Log($"💾 Contenu: {jsonContent}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erreur sauvegarde cache: {e.Message}");
                Debug.LogError($"📁 Chemin problématique: '{fullCachePath}'");
            }
        }
        
        /// <summary>
        /// Nettoie le cache (supprime les assets anciens/inutilisés)
        /// </summary>
        [ContextMenu("Cleanup Cache")]
        public void CleanupCache()
        {
            if (cacheData == null) return;
            
            int removed = cacheData.CleanupOldAssets(30, 1);
            
            if (removed > 0)
            {
                SaveCacheToDisk();
                Debug.Log($"🧹 Cache nettoyé: {removed} assets supprimés");
            }
            else
            {
                Debug.Log("✅ Cache déjà propre");
            }
        }
        
        /// <summary>
        /// Affiche les statistiques du cache
        /// </summary>
        [ContextMenu("Show Cache Stats")]
        public void ShowCacheStats()
        {
            if (cacheData == null)
            {
                Debug.Log("❌ Pas de données de cache");
                return;
            }
            
            Debug.Log($@"📊 STATISTIQUES DU CACHE
Total Assets: {cacheData.totalAssets}
Générés: {cacheData.generatedAssetsCount}
Manuels: {cacheData.manualAssetsCount}
En mémoire: {loadedPrefabs.Count}
Dernière MAJ: {cacheData.lastUpdated:yyyy-MM-dd HH:mm:ss}
Dernier nettoyage: {cacheData.lastCleanup:yyyy-MM-dd HH:mm:ss}");
            
            // Affiche les assets les plus utilisés
            var mostUsed = cacheData.GetMostUsedAssets(5);
            if (mostUsed.Count > 0)
            {
                Debug.Log("🏆 Top 5 Assets:");
                foreach (var asset in mostUsed)
                {
                    Debug.Log($"  • {asset.displayName}: {asset.usageCount} utilisations");
                }
            }
        }
        
        /// <summary>
        /// Précharge des assets populaires
        /// </summary>
        [ContextMenu("Preload Popular Assets")]
        public async void PreloadPopularAssets()
        {
            if (cacheData == null) return;
            
            var popular = cacheData.GetMostUsedAssets(10);
            Debug.Log($"🚀 Préchargement de {popular.Count} assets populaires...");
            
            foreach (var asset in popular)
            {
                if (!loadedPrefabs.ContainsKey(asset.itemName))
                {
                    await GetQuestItemPrefab(asset.itemName);
                }
            }
            
            Debug.Log("✅ Préchargement terminé");
        }
        
        /// <summary>
        /// NOUVELLE MÉTHODE : Test d'ajout manuel au cache
        /// </summary>
        [ContextMenu("Test Add To Cache")]
        public void TestAddToCache()
        {
            Debug.Log("🧪 Test d'ajout manuel au cache...");
            
            // Crée un objet de test
            GameObject testObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            testObject.name = "TestCristal_Generated";
            
            // Ajoute au cache
            AddAssetToCache("cristal_energie", testObject, "test/path/cristal.fbx");
            
            // Affiche le cache
            ShowCacheStats();
            
            Debug.Log("✅ Test d'ajout terminé");
        }
        
        /// <summary>
        /// Vide complètement le cache mémoire
        /// </summary>
        [ContextMenu("Clear Memory Cache")]
        public void ClearMemoryCache()
        {
            loadedPrefabs.Clear();
            Debug.Log("🧹 Cache mémoire vidé");
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveCacheToDisk();
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SaveCacheToDisk();
        }
        
        void OnDestroy()
        {
            SaveCacheToDisk();
        }
    }
}