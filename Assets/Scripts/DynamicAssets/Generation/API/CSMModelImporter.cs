using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using DynamicAssets.Generation.API;
using DynamicAssets.Generation.Config;
using DynamicAssets.Core;

namespace DynamicAssets.Generation.API
{
    /// <summary>
    /// Gère le téléchargement et l'import automatique des modèles CSM dans Unity
    /// </summary>
    public class CSMModelImporter : MonoBehaviour
    {
        public static CSMModelImporter Instance { get; private set; }
        
        [Header("Import Settings")]
        [SerializeField] private string downloadFolder = "GeneratedAssets/Downloads/";
        [SerializeField] private string modelsFolder = "GeneratedAssets/Models/";
        [SerializeField] private string prefabsFolder = "GeneratedAssets/Prefabs/";
        
        [Header("Import Configuration")]
        [SerializeField] private bool autoOptimizeMesh = true;
        [SerializeField] private bool generateColliders = true;
        [SerializeField] private bool addQuestObjectComponent = true;
        
        [Header("Status")]
        [SerializeField] private int activeDownloads = 0;
        [SerializeField] private int totalDownloads = 0;
        [SerializeField] private int successfulImports = 0;
        [SerializeField] private int failedImports = 0;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        
        // Paths complets
        private string fullDownloadPath;
        private string fullModelsPath;
        private string fullPrefabsPath;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePaths();
                Debug.Log("✅ CSMModelImporter Instance créée et initialisée");
            }
            else
            {
                Debug.LogWarning("⚠️ CSMModelImporter Instance déjà existante - Destruction de ce doublon");
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Initialise les chemins de dossiers
        /// </summary>
        void InitializePaths()
        {
            fullDownloadPath = Path.Combine(Application.dataPath, downloadFolder);
            fullModelsPath = Path.Combine(Application.dataPath, modelsFolder);
            fullPrefabsPath = Path.Combine(Application.dataPath, prefabsFolder);
            
            // Crée les dossiers s'ils n'existent pas
            Directory.CreateDirectory(fullDownloadPath);
            Directory.CreateDirectory(fullModelsPath);
            Directory.CreateDirectory(fullPrefabsPath);
            
            if (debugMode)
            {
                Debug.Log($"📁 Chemins d'import initialisés:");
                Debug.Log($"  Downloads: {fullDownloadPath}");
                Debug.Log($"  Models: {fullModelsPath}");
                Debug.Log($"  Prefabs: {fullPrefabsPath}");
            }
        }
        
        /// <summary>
        /// MÉTHODE PRINCIPALE : Import complet depuis une réponse CSM
        /// </summary>
        public async Task<GameObject> ImportModelFromCSMResponse(CSMResponse response, string itemName)
        {
            if (response == null || !response.IsSuccess())
            {
                Debug.LogError("❌ Réponse CSM invalide pour import");
                return null;
            }
            
            if (debugMode)
                Debug.Log($"🚀 === DÉBUT IMPORT : {itemName} ===");
            
            totalDownloads++;
            activeDownloads++;
            
            try
            {
                // Étape 1: Téléchargement du modèle
                string localFilePath = await DownloadModel(response, itemName);
                if (string.IsNullOrEmpty(localFilePath))
                {
                    failedImports++;
                    return null;
                }
                
                // Étape 2: Import dans Unity
                GameObject prefab = await ImportModelToUnity(localFilePath, itemName, response);
                if (prefab != null)
                {
                    successfulImports++;
                    
                    // Étape 3: Mise à jour du cache
                    UpdateAssetCache(itemName, prefab, localFilePath, response);
                    
                    if (debugMode)
                        Debug.Log($"✅ === IMPORT TERMINÉ : {itemName} ===");
                }
                else
                {
                    failedImports++;
                }
                
                return prefab;
            }
            catch (System.Exception e)
            {
                failedImports++;
                Debug.LogError($"❌ Erreur import {itemName}: {e.Message}");
                return null;
            }
            finally
            {
                activeDownloads--;
            }
        }
        
        /// <summary>
        /// Télécharge le modèle depuis l'URL CSM
        /// </summary>
        async Task<string> DownloadModel(CSMResponse response, string itemName)
        {
            if (string.IsNullOrEmpty(response.download_url))
            {
                Debug.LogError("❌ URL de téléchargement manquante");
                return null;
            }
            
            // CORRECTION : Vérification des chemins
            if (string.IsNullOrEmpty(fullDownloadPath))
            {
                Debug.LogError("❌ fullDownloadPath est null - Réinitialisation...");
                InitializePaths();
                
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    Debug.LogError("❌ Impossible d'initialiser fullDownloadPath");
                    return null;
                }
            }
            
            // Détermine le nom de fichier et l'extension
            string fileExtension = GetFileExtension(response.file_format, response.download_url);
            string fileName = $"{itemName}_{response.generation_id}{fileExtension}";
            
            // CORRECTION : Validation avant Path.Combine
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("❌ fileName est null ou vide");
                return null;
            }
            
            Debug.Log($"📁 Paths debug:");
            Debug.Log($"  fullDownloadPath: '{fullDownloadPath}'");
            Debug.Log($"  fileName: '{fileName}'");
            
            string localPath = Path.Combine(fullDownloadPath, fileName);
            
            if (debugMode)
                Debug.Log($"📥 Téléchargement RÉEL: {response.download_url} → {fileName}");
            
            // Téléchargement réel via coroutine
            bool downloadSuccess = false;
            bool downloadCompleted = false;
            
            StartCoroutine(DownloadFileCoroutine(response.download_url, localPath, (success) => {
                downloadSuccess = success;
                downloadCompleted = true;
            }));
            
            // Attend la fin du téléchargement
            while (!downloadCompleted)
            {
                await Task.Delay(100);
            }
            
            if (downloadSuccess && File.Exists(localPath))
            {
                Debug.Log($"✅ Téléchargement réel terminé: {fileName} ({new FileInfo(localPath).Length} bytes)");
                return localPath;
            }
            else
            {
                Debug.LogError($"❌ Échec téléchargement: {fileName}");
                return null;
            }
        }
        
        /// <summary>
        /// Coroutine pour télécharger le fichier
        /// </summary>
        IEnumerator DownloadFileCoroutine(string url, string localPath, System.Action<bool> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerFile(localPath);
                request.timeout = 60; // 1 minute timeout
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"✅ Fichier téléchargé: {Path.GetFileName(localPath)} ({request.downloadedBytes} bytes)");
                    callback(true);
                }
                else
                {
                    Debug.LogError($"❌ Erreur téléchargement: {request.error}");
                    callback(false);
                }
            }
        }
        
        /// <summary>
        /// Importe le modèle téléchargé dans Unity
        /// </summary>
        async Task<GameObject> ImportModelToUnity(string filePath, string itemName, CSMResponse response)
        {
            if (debugMode)
                Debug.Log($"🔧 Import Unity: {Path.GetFileName(filePath)}");
            
            // Vérifie que le fichier existe réellement
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"⚠️ Fichier non trouvé: {filePath} - Utilisation modèle simulé");
                return CreateSimulatedModel(itemName, response);
            }
            
            // PHASE 2B.3 : Pour l'instant, on simule l'import même avec un vrai fichier
            // En Phase 2C, on utilisera AssetDatabase pour l'import réel
            
            await Task.Delay(1000); // Simule le temps d'import
            
            Debug.Log($"📁 Fichier réel détecté: {new FileInfo(filePath).Length} bytes");
            Debug.Log($"🔧 Import Unity simulé pour fichier réel: {Path.GetFileName(filePath)}");
            
            // SIMULATION : Crée un GameObject de test mais avec métadonnées réelles
            GameObject simulatedModel = CreateSimulatedModel(itemName, response);
            
            if (simulatedModel != null)
            {
                // Configure le modèle avec infos réelles
                ConfigureImportedModel(simulatedModel, itemName, response);
                
                // Ajoute des métadonnées sur le fichier réel
                GeneratedAssetInfo assetInfo = simulatedModel.GetComponent<GeneratedAssetInfo>();
                if (assetInfo != null)
                {
                    assetInfo.realFilePath = filePath;
                    assetInfo.realFileSize = new FileInfo(filePath).Length;
                    assetInfo.wasReallyDownloaded = true;
                }
                
                // Crée un prefab simulé
                GameObject prefab = CreatePrefabFromModel(simulatedModel, itemName);
                
                if (debugMode)
                    Debug.Log($"✅ Modèle importé (simulé avec fichier réel): {itemName}");
                
                return prefab;
            }
            
            return null;
        }
        
        /// <summary>
        /// SIMULATION : Crée un modèle de test (en attendant l'import réel)
        /// </summary>
        GameObject CreateSimulatedModel(string itemName, CSMResponse response)
        {
            // Crée un objet basique selon le type d'item
            GameObject model;
            
            if (itemName.Contains("cristal") || itemName.Contains("crystal"))
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Cube);
                model.name = $"Generated_{itemName}";
                
                // Style cristal
                Renderer renderer = model.GetComponent<Renderer>();
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.3f, 0.7f, 1f, 0.8f); // Bleu cristallin
                material.SetFloat("_Metallic", 0.8f);
                material.SetFloat("_Smoothness", 0.9f);
                renderer.material = material;
                
                // Rotation pour look plus intéressant
                model.transform.rotation = Quaternion.Euler(45, 45, 0);
                model.transform.localScale = Vector3.one * 0.8f;
            }
            else if (itemName.Contains("terminal") || itemName.Contains("console"))
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Cube);
                model.name = $"Generated_{itemName}";
                
                // Style terminal
                Renderer renderer = model.GetComponent<Renderer>();
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.2f, 0.2f, 0.3f); // Gris technologique
                material.SetFloat("_Metallic", 0.9f);
                material.SetFloat("_Smoothness", 0.7f);
                renderer.material = material;
                
                model.transform.localScale = new Vector3(1.2f, 0.8f, 0.6f);
            }
            else
            {
                // Objet générique
                model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                model.name = $"Generated_{itemName}";
                
                Renderer renderer = model.GetComponent<Renderer>();
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(Random.Range(0.3f, 1f), Random.Range(0.3f, 1f), Random.Range(0.3f, 1f));
                renderer.material = material;
            }
            
            return model;
        }
        
        /// <summary>
        /// Configure le modèle importé avec les composants nécessaires
        /// </summary>
        void ConfigureImportedModel(GameObject model, string itemName, CSMResponse response)
        {
            // Supprime le collider par défaut si nécessaire
            Collider defaultCollider = model.GetComponent<Collider>();
            if (defaultCollider != null && !generateColliders)
            {
                DestroyImmediate(defaultCollider);
            }
            
            // Ajoute le composant QuestObject si nécessaire
            if (addQuestObjectComponent)
            {
                QuestObject questObj = model.GetComponent<QuestObject>();
                if (questObj == null)
                {
                    questObj = model.AddComponent<QuestObject>();
                }
                
                questObj.objectName = itemName;
                questObj.objectType = QuestObjectType.Item; // Par défaut
            }
            
            // Optimise le mesh si demandé
            if (autoOptimizeMesh)
            {
                OptimizeMeshForRuntime(model);
            }
            
            // Ajoute des métadonnées sur la génération
            GeneratedAssetInfo assetInfo = model.AddComponent<GeneratedAssetInfo>();
            assetInfo.itemName = itemName;
            assetInfo.generationId = response.generation_id;
            assetInfo.generatedDate = System.DateTime.Now;
            assetInfo.triangleCount = response.actual_triangles;
            assetInfo.fileSize = response.file_size_bytes;
            assetInfo.qualityScore = response.quality_score;
            
            if (debugMode)
                Debug.Log($"🔧 Modèle configuré: {itemName} ({response.actual_triangles} triangles)");
        }
        
        /// <summary>
        /// Optimise le mesh pour le runtime
        /// </summary>
        void OptimizeMeshForRuntime(GameObject model)
        {
            MeshFilter meshFilter = model.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                Mesh mesh = meshFilter.mesh;
                
                // Optimise le mesh
                mesh.Optimize();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                
                if (debugMode)
                    Debug.Log($"🔧 Mesh optimisé: {mesh.vertexCount} vertices, {mesh.triangles.Length/3} triangles");
            }
        }
        
        /// <summary>
        /// Crée un prefab depuis le modèle
        /// </summary>
        GameObject CreatePrefabFromModel(GameObject model, string itemName)
        {
            // En Phase 2B.3 : Simulation de création de prefab
            // En Phase 2C : Utilisation de PrefabUtility.SaveAsPrefabAsset
            
            if (debugMode)
                Debug.Log($"📦 Prefab créé (simulé): {itemName}");
            
            return model; // Pour l'instant, retourne le modèle directement
        }
        
        /// <summary>
        /// Met à jour le cache avec le nouvel asset
        /// </summary>
        void UpdateAssetCache(string itemName, GameObject prefab, string modelPath, CSMResponse response)
        {
            if (DynamicAssetManager.Instance != null)
            {
                Debug.Log($"💾 === AJOUT AU CACHE ===");
                Debug.Log($"Item: {itemName}");
                Debug.Log($"Prefab: {(prefab != null ? prefab.name : "null")}");
                Debug.Log($"Model Path: {modelPath}");
                
                // CORRECTION : Utilise la bonne méthode
                DynamicAssetManager.Instance.AddAssetToCache(itemName, prefab, modelPath);
                
                // Vérification immédiate
                StartCoroutine(VerifyCacheAddition(itemName));
                
                if (debugMode)
                    Debug.Log($"✅ Asset ajouté au cache: {itemName}");
            }
            else
            {
                Debug.LogError("❌ DynamicAssetManager.Instance est null - Cache non mis à jour !");
            }
        }
        
        /// <summary>
        /// Vérifie que l'asset a bien été ajouté au cache
        /// </summary>
        System.Collections.IEnumerator VerifyCacheAddition(string itemName)
        {
            yield return new WaitForSeconds(1f); // Laisse le temps au cache de se mettre à jour
            
            if (DynamicAssetManager.Instance != null)
            {
                DynamicAssetManager.Instance.ShowCacheStats();
                
                // Test de récupération - SANS try-catch dans la coroutine
                Debug.Log($"🔍 Vérification cache pour: {itemName}");
                
                var task = DynamicAssetManager.Instance.GetQuestItemPrefab(itemName);
                
                // Attend que la tâche se termine
                while (!task.IsCompleted)
                {
                    yield return null;
                }
                
                // Vérifie le résultat après completion
                if (task.IsCompletedSuccessfully && task.Result != null)
                {
                    Debug.Log($"✅ VÉRIFICATION CACHE RÉUSSIE: {itemName} trouvé !");
                }
                else if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Erreur vérification cache: {task.Exception?.GetBaseException()?.Message}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ VÉRIFICATION CACHE ÉCHOUÉE: {itemName} non trouvé");
                }
            }
        }
        
        /// <summary>
        /// Détermine la qualité de l'asset selon le nombre de triangles
        /// </summary>
        AssetQuality DetermineAssetQuality(int triangles)
        {
            if (triangles < 500) return AssetQuality.Low;
            if (triangles < 1500) return AssetQuality.Medium;
            if (triangles < 3000) return AssetQuality.High;
            return AssetQuality.Ultra;
        }
        
        /// <summary>
        /// Détermine l'extension de fichier
        /// </summary>
        string GetFileExtension(string format, string url)
        {
            if (!string.IsNullOrEmpty(format))
            {
                switch (format.ToLower())
                {
                    case "fbx": return ".fbx";
                    case "obj": return ".obj";
                    case "glb": return ".glb";
                    case "gltf": return ".gltf";
                    default: return ".fbx";
                }
            }
            
            // Essaie d'extraire l'extension de l'URL
            try
            {
                string fileName = Path.GetFileName(new System.Uri(url).LocalPath);
                return Path.GetExtension(fileName);
            }
            catch
            {
                return ".fbx"; // Par défaut
            }
        }
        
        /// <summary>
        /// Affiche les statistiques d'import
        /// </summary>
        [ContextMenu("Show Import Stats")]
        public void ShowImportStats()
        {
            Debug.Log($@"📊 STATISTIQUES IMPORT CSM
Téléchargements actifs: {activeDownloads}
Total téléchargements: {totalDownloads}
Imports réussis: {successfulImports}
Imports échoués: {failedImports}
Taux de succès: {(totalDownloads > 0 ? (successfulImports * 100f / totalDownloads) : 0):F1}%

Dossiers:
- Downloads: {fullDownloadPath}
- Models: {fullModelsPath}
- Prefabs: {fullPrefabsPath}");
        }
        
        /// <summary>
        /// Test d'instance forcé
        /// </summary>
        [ContextMenu("Test Instance")]
        public void TestInstance()
        {
            Debug.Log($"🔍 Test Instance CSMModelImporter:");
            Debug.Log($"  • Instance static: {(Instance != null ? "✅" : "❌")}");
            Debug.Log($"  • Ce GameObject: {(this != null ? "✅" : "❌")}");
            Debug.Log($"  • GameObject actif: {gameObject.activeInHierarchy}");
            Debug.Log($"  • Composant activé: {enabled}");
            
            if (Instance == null)
            {
                Debug.LogWarning("⚠️ Instance NULL - Force l'assignation...");
                Instance = this;
                Debug.Log("✅ Instance forcée assignée");
            }
        }
        
        /// <summary>
        /// NOUVELLE MÉTHODE : Force l'initialisation si nécessaire
        /// </summary>
        public static CSMModelImporter GetOrCreateInstance()
        {
            if (Instance == null)
            {
                Debug.LogWarning("⚠️ CSMModelImporter.Instance NULL - Recherche dans la scène...");
                
                Instance = FindObjectOfType<CSMModelImporter>();
                
                if (Instance != null)
                {
                    Debug.Log("✅ CSMModelImporter trouvé dans la scène et assigné");
                }
                else
                {
                    Debug.LogError("❌ Aucun CSMModelImporter trouvé dans la scène !");
                }
            }
            
            return Instance;
        }
        [ContextMenu("Test Import")]
        public async void TestImport()
        {
            Debug.Log("🧪 Test d'import CSM...");
            
            // Crée une réponse de test
            CSMResponse testResponse = CSMResponse.CreateTestSuccessResponse("test_import");
            testResponse.download_url = "https://fake-url.com/test_model.fbx";
            testResponse.file_format = "fbx";
            
            GameObject result = await ImportModelFromCSMResponse(testResponse, "test_import");
            
            if (result != null)
            {
                Debug.Log($"✅ Test d'import réussi: {result.name}");
                
                // Optionnel : Spawn pour visualiser
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    Vector3 spawnPos = Vector3.zero + Vector3.right * 3f;
                    Instantiate(result, spawnPos, Quaternion.identity);
                    Debug.Log($"👁️ Modèle spawné à {spawnPos} pour visualisation");
                }
            }
            else
            {
                Debug.LogError("❌ Test d'import échoué");
            }
        }
        
        /// <summary>
        /// Nettoie les fichiers temporaires
        /// </summary>
        [ContextMenu("Cleanup Downloads")]
        public void CleanupDownloads()
        {
            try
            {
                if (Directory.Exists(fullDownloadPath))
                {
                    string[] files = Directory.GetFiles(fullDownloadPath);
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }
                    Debug.Log($"🧹 {files.Length} fichiers nettoyés du dossier Downloads");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erreur nettoyage: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Composant pour stocker les métadonnées des assets générés
    /// </summary>
    public class GeneratedAssetInfo : MonoBehaviour
    {
        [Header("Generation Info")]
        public string itemName;
        public string generationId;
        public System.DateTime generatedDate;
        
        [Header("Technical Info")]
        public int triangleCount;
        public long fileSize;
        public int qualityScore;
        
        [Header("Real File Info")]
        public string realFilePath;
        public long realFileSize;
        public bool wasReallyDownloaded;
        
        /// <summary>
        /// Affiche les infos de cet asset
        /// </summary>
        [ContextMenu("Show Asset Info")]
        public void ShowAssetInfo()
        {
            Debug.Log($@"📋 ASSET INFO: {itemName}
Generation ID: {generationId}
Date: {generatedDate:yyyy-MM-dd HH:mm:ss}
Triangles: {triangleCount:N0}
File Size: {fileSize:N0} bytes
Quality Score: {qualityScore}/100

REAL FILE INFO:
Downloaded: {(wasReallyDownloaded ? "✅ OUI" : "❌ NON")}
File Path: {realFilePath ?? "N/A"}
Real Size: {realFileSize:N0} bytes");
        }
        
        /// <summary>
        /// Ouvre le fichier réel dans l'explorateur
        /// </summary>
        [ContextMenu("Open Real File")]
        public void OpenRealFile()
        {
            if (wasReallyDownloaded && !string.IsNullOrEmpty(realFilePath) && File.Exists(realFilePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{realFilePath}\"");
                Debug.Log($"📁 Ouverture: {realFilePath}");
            }
            else
            {
                Debug.LogWarning("⚠️ Aucun fichier réel disponible");
            }
        }
    }
}