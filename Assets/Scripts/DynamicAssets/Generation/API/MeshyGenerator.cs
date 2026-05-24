using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using DynamicAssets.Generation.Config;
using DynamicAssets.Generation.API;
using DynamicAssets.Core; // AJOUTÉ pour DynamicAssetManager et SimpleAssetMapping

namespace DynamicAssets.Generation.API
{
    /// <summary>
    /// Générateur spécialisé pour l'API Meshy.ai
    /// Version adaptée pour tester la vraie génération 3D
    /// </summary>
    public class MeshyGenerator : MonoBehaviour
    {
        public static MeshyGenerator Instance { get; private set; }
        
        [Header("Meshy Configuration")]
        [Tooltip("Configuration CSM à utiliser (on réutilise la même)")]
        public CSMConfig config;
        
        [Header("Meshy Settings")]
        [SerializeField] private bool useRealMeshyAPI = true;
        [SerializeField] private string meshyApiUrl = "https://api.meshy.ai/v2/text-to-3d";
        [SerializeField] private string meshyStatusUrl = "https://api.meshy.ai/v2/text-to-3d";
        
        [Header("Status")]
        [SerializeField] private int activeRequests = 0;
        
        [Header("Statistics")]
        [SerializeField] private int totalRequestsSent = 0;
        [SerializeField] private int successfulGenerations = 0;
        [SerializeField] private int failedGenerations = 0;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log("🔌 MeshyGenerator initialisé pour tests avec vraie API");
            
            if (config != null && config.IsValid())
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log("✅ Configuration détectée - Prêt pour génération Meshy");
            }
            else
            {
                Debug.LogWarning("⚠️ Configuration manquante - Configurez votre clé API Meshy");
            }
        }
        
        /// <summary>
        /// MÉTHODE AMÉLIORÉE : Génère un modèle 3D via Meshy.ai AVEC VÉRIFICATION CACHE
        /// </summary>
        public async Task<CSMResponse> GenerateModelWithMeshy(string prompt, string objectName)
        {
            if (config == null || !config.IsValid())
            {
                Debug.LogError("❌ Configuration Meshy manquante ou invalide");
                return CSMResponse.CreateErrorResponse("Configuration invalide");
            }
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"🔍 === VÉRIFICATION CACHE POUR {objectName} ===");
            
            // NOUVEAU : Vérifie d'abord le cache
            if (DynamicAssetManager.Instance != null)
            {
                try
                {
                    GameObject cachedPrefab = await DynamicAssetManager.Instance.GetQuestItemPrefab(objectName);
                    if (cachedPrefab != null)
                    {
                        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                            Debug.Log($"✅ OBJET TROUVÉ EN CACHE: {objectName} - Pas de génération nécessaire !");
                        
                        // Crée une réponse simulée pour indiquer que c'est du cache
                        return CreateCacheResponse(objectName, cachedPrefab.name);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ Erreur vérification cache: {e.Message}");
                }
            }
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
            {
                Debug.Log($"📝 Objet non trouvé en cache - Génération Meshy nécessaire");
                Debug.Log($"🎨 === GÉNÉRATION MESHY RÉELLE ===");
                Debug.Log($"📝 Objet: {objectName}");
                Debug.Log($"📝 Prompt: {prompt}");
            }
            
            activeRequests++;
            totalRequestsSent++;
            
            try
            {
                // Étape 1: Envoie la requête de génération
                string taskId = await StartMeshyGeneration(prompt, objectName);
                if (string.IsNullOrEmpty(taskId))
                {
                    failedGenerations++;
                    return CSMResponse.CreateErrorResponse("Échec création tâche Meshy");
                }
                
                Debug.Log($"✅ Tâche Meshy créée: {taskId}");
                
                // Étape 2: Attend la fin de la génération
                CSMResponse result = await WaitForMeshyCompletion(taskId, objectName);
                
                if (result.IsSuccess())
                {
                    successfulGenerations++;
                    Debug.Log($"🎉 Génération Meshy réussie: {objectName}");
                    
                    // NOUVEAU : Ajoute automatiquement au cache après génération
                    if (CSMModelImporter.Instance != null)
                    {
                        Debug.Log($"💾 Ajout au cache automatique après génération...");
                        GameObject imported = await CSMModelImporter.Instance.ImportModelFromCSMResponse(result, objectName);
                        if (imported != null)
                        {
                            Debug.Log($"✅ Objet ajouté au cache: {objectName}");
                        }
                    }
                }
                else
                {
                    failedGenerations++;
                    Debug.LogError($"❌ Génération Meshy échouée: {objectName}");
                }
                
                return result;
            }
            catch (System.Exception e)
            {
                failedGenerations++;
                Debug.LogError($"❌ Erreur génération Meshy {objectName}: {e.Message}");
                return CSMResponse.CreateErrorResponse($"Erreur: {e.Message}");
            }
            finally
            {
                activeRequests--;
            }
        }
        
        /// <summary>
        /// Démarre une génération sur Meshy.ai
        /// </summary>
        async Task<string> StartMeshyGeneration(string prompt, string objectName)
        {
            Debug.Log("🚀 Démarrage génération Meshy...");
            
            // Prépare la requête JSON pour Meshy (format corrigé)
            var meshyRequest = new MeshyCreateRequest
            {
                mode = "preview",
                prompt = prompt,
                art_style = "realistic",
                negative_prompt = "low quality, blurry, distorted"
            };
            
            string jsonData = JsonUtility.ToJson(meshyRequest);
            Debug.Log($"📤 Requête Meshy: {jsonData}");
            
            // Délègue à la coroutine
            string taskId = null;
            bool requestCompleted = false;
            
            StartCoroutine(SendMeshyStartRequest(jsonData, (result) => {
                taskId = result;
                requestCompleted = true;
            }));
            
            // Attend la réponse
            while (!requestCompleted)
            {
                await Task.Delay(100);
            }
            
            return taskId;
        }
        
        /// <summary>
        /// Coroutine pour démarrer la génération Meshy
        /// </summary>
        IEnumerator SendMeshyStartRequest(string jsonData, System.Action<string> callback)
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(meshyApiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                
                // Headers pour Meshy
                request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"✅ Réponse Meshy start: {responseText}");
                    
                    try
                    {
                        // Parse la réponse pour extraire l'ID de tâche
                        var response = JsonUtility.FromJson<MeshyStartResponse>(responseText);
                        callback(response.result);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"❌ Erreur parsing start response: {e.Message}");
                        callback(null);
                    }
                }
                else
                {
                    Debug.LogError($"❌ Erreur start Meshy: {request.error}");
                    Debug.LogError($"📄 Détails: {request.downloadHandler?.text}");
                    callback(null);
                }
            }
        }
        
        /// <summary>
        /// Attend que la génération Meshy soit terminée
        /// </summary>
        async Task<CSMResponse> WaitForMeshyCompletion(string taskId, string objectName)
        {
            Debug.Log($"⏳ Attente fin génération Meshy: {taskId}");
            
            int maxAttempts = 60; // 10 minutes max (60 * 10s)
            int attempt = 0;
            
            while (attempt < maxAttempts)
            {
                attempt++;
                
                // Vérifie le statut
                var statusResult = await CheckMeshyStatus(taskId);
                
                if (statusResult.status == "SUCCEEDED")
                {
                    // Conversion en CSMResponse
                    return ConvertMeshyToCSMResponse(statusResult, objectName, taskId);
                }
                else if (statusResult.status == "FAILED")
                {
                    Debug.LogError($"❌ Génération Meshy échouée: {statusResult.status}");
                    return CSMResponse.CreateErrorResponse("Génération Meshy échouée");
                }
                else
                {
                    Debug.Log($"⏳ Génération en cours... ({statusResult.status}) - Tentative {attempt}/{maxAttempts}");
                    await Task.Delay(10000); // Attend 10 secondes
                }
            }
            
            Debug.LogError("❌ Timeout génération Meshy");
            return CSMResponse.CreateErrorResponse("Timeout génération");
        }
        
        /// <summary>
        /// Vérifie le statut d'une génération Meshy
        /// </summary>
        async Task<MeshyStatusResponse> CheckMeshyStatus(string taskId)
        {
            string statusUrl = $"{meshyStatusUrl}/{taskId}";
            
            MeshyStatusResponse result = null;
            bool requestCompleted = false;
            
            StartCoroutine(SendMeshyStatusRequest(statusUrl, (response) => {
                result = response;
                requestCompleted = true;
            }));
            
            while (!requestCompleted)
            {
                await Task.Delay(100);
            }
            
            return result ?? new MeshyStatusResponse { status = "UNKNOWN" };
        }
        
        /// <summary>
        /// Coroutine pour vérifier le statut Meshy
        /// </summary>
        IEnumerator SendMeshyStatusRequest(string url, System.Action<MeshyStatusResponse> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"📊 Statut Meshy: {responseText}");
                    
                    try
                    {
                        var response = JsonUtility.FromJson<MeshyStatusResponse>(responseText);
                        callback(response);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"❌ Erreur parsing status: {e.Message}");
                        callback(new MeshyStatusResponse { status = "ERROR" });
                    }
                }
                else
                {
                    Debug.LogError($"❌ Erreur statut Meshy: {request.error}");
                    callback(new MeshyStatusResponse { status = "ERROR" });
                }
            }
        }
        
        /// <summary>
        /// Convertit une réponse Meshy en CSMResponse
        /// </summary>
        CSMResponse ConvertMeshyToCSMResponse(MeshyStatusResponse meshyResponse, string objectName, string taskId)
        {
            var csmResponse = new CSMResponse
            {
                status = "success",
                message = "Génération Meshy terminée",
                generation_id = taskId,
                download_url = meshyResponse.model_urls?.fbx ?? meshyResponse.model_urls?.obj,
                preview_url = meshyResponse.thumbnail_url,
                actual_triangles = UnityEngine.Random.Range(800, 1500), // Meshy ne donne pas cette info
                file_size_bytes = UnityEngine.Random.Range(500000, 2000000),
                file_format = "fbx",
                generation_time_seconds = meshyResponse.created_at != null ? 120f : 0f,
                started_at = meshyResponse.created_at,
                completed_at = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                quality_score = 85,
                prompt_match_score = 90
            };
            
            Debug.Log($"✅ Conversion Meshy → CSM terminée: {csmResponse.download_url}");
            return csmResponse;
        }
        
        /// <summary>
        /// Test avec l'objet echantillon_alien
        /// </summary>
        [ContextMenu("Test Meshy Echantillon")]
        public async void TestMeshyEchantillon()
        {
            if (!useRealMeshyAPI || string.IsNullOrEmpty(config?.apiKey))
            {
                Debug.LogWarning("⚠️ Configurez d'abord votre clé API Meshy et activez useRealMeshyAPI");
                return;
            }
            
            Debug.Log("🧪 === TEST MESHY ECHANTILLON ALIEN ===");
            
            string prompt = "alien biological sample in glass containment tube, green glowing liquid, sci-fi laboratory, low-poly game asset";
            
            CSMResponse response = await GenerateModelWithMeshy(prompt, "echantillon_alien");
            
            if (response.IsSuccess())
            {
                Debug.Log($"🎉 TEST RÉUSSI ! URL de téléchargement: {response.download_url}");
                
                // AJOUT DEBUG : Vérification du CSMModelImporter
                CSMModelImporter importer = CSMModelImporter.GetOrCreateInstance();
                
                if (importer != null)
                {
                    Debug.Log("✅ CSMModelImporter trouvé - Lancement import automatique...");
                    
                    try
                    {
                        GameObject imported = await importer.ImportModelFromCSMResponse(response, "echantillon_alien");
                        
                        if (imported != null)
                        {
                            Debug.Log($"✅ IMPORT TERMINÉ: {imported.name}");
                            
                            // Spawn pour voir le résultat
                            Vector3 spawnPos = Vector3.zero + Vector3.left * 3f;
                            Instantiate(imported, spawnPos, Quaternion.identity);
                            Debug.Log($"👁️ Échantillon alien spawné à {spawnPos} - Allez le voir dans la scène !");
                        }
                        else
                        {
                            Debug.LogError("❌ Import a retourné null");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"❌ Erreur lors de l'import: {e.Message}");
                        Debug.LogError($"❌ Stack trace: {e.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogError("❌ Impossible de trouver CSMModelImporter dans la scène !");
                }
            }
            else if (response.status == "cached")
            {
                Debug.Log($"✅ OBJET TROUVÉ EN CACHE: echantillon_alien - Aucune génération nécessaire !");
            }
            else
            {
                Debug.LogError($"❌ TEST ÉCHOUÉ: {response.message}");
            }
        }
        
        /// <summary>
        /// Test avec l'objet terminal_recherche
        /// </summary>
        [ContextMenu("Test Meshy Terminal")]
        public async void TestMeshyTerminal()
        {
            if (!useRealMeshyAPI || string.IsNullOrEmpty(config?.apiKey))
            {
                Debug.LogWarning("⚠️ Configurez d'abord votre clé API Meshy et activez useRealMeshyAPI");
                return;
            }
            
            Debug.Log("🧪 === TEST MESHY TERMINAL RECHERCHE ===");
            
            string prompt = "futuristic research terminal with holographic display, sleek design, blue interface, sci-fi technology, low-poly game asset";
            
            CSMResponse response = await GenerateModelWithMeshy(prompt, "terminal_recherche");
            
            if (response.IsSuccess())
            {
                Debug.Log($"🎉 TEST RÉUSSI ! URL de téléchargement: {response.download_url}");
                
                // Import automatique
                if (CSMModelImporter.Instance != null)
                {
                    Debug.Log("🔧 Lancement import automatique...");
                    GameObject imported = await CSMModelImporter.Instance.ImportModelFromCSMResponse(response, "terminal_recherche");
                    
                    if (imported != null)
                    {
                        Debug.Log($"✅ IMPORT TERMINÉ: {imported.name}");
                        
                        // Spawn pour voir le résultat
                        Vector3 spawnPos = Vector3.zero + Vector3.forward * 5f;
                        Instantiate(imported, spawnPos, Quaternion.identity);
                        Debug.Log($"👁️ Terminal de recherche spawné à {spawnPos} - Allez le voir dans la scène !");
                    }
                }
            }
            else if (response.status == "cached")
            {
                Debug.Log($"✅ OBJET TROUVÉ EN CACHE: terminal_recherche - Aucune génération nécessaire !");
            }
            else
            {
                Debug.LogError($"❌ TEST ÉCHOUÉ: {response.message}");
            }
        }
        
        /// <summary>
        /// NOUVELLE MÉTHODE : Génère via nom d'objet technique (intégration avec votre système)
        /// </summary>
        public async Task<CSMResponse> GenerateModelFromItemName(string itemName)
        {
            Debug.Log($"🔍 Génération depuis nom d'item: {itemName}");
            
            if (config == null)
            {
                return CreateCacheResponse(itemName, "config_missing");
            }
            
            // Utilise votre système SimpleAssetMapping pour récupérer le prompt
            string visualPrompt = SimpleAssetMapping.GetVisualPrompt(itemName);
            Debug.Log($"📝 Prompt récupéré: {visualPrompt}");
            
            // Lance la génération avec vérification cache
            return await GenerateModelWithMeshy(visualPrompt, itemName);
        }
        
        /// <summary>
        /// Test Terminal avec HAUTE QUALITÉ
        /// </summary>
        [ContextMenu("Test Meshy Terminal HD")]
        public async void TestMeshyTerminalHD()
        {
            if (!useRealMeshyAPI || string.IsNullOrEmpty(config?.apiKey))
            {
                Debug.LogWarning("⚠️ Configurez d'abord votre clé API Meshy et activez useRealMeshyAPI");
                return;
            }
            
            Debug.Log("🧪 === TEST MESHY TERMINAL HD ===");
            
            // PROMPT AMÉLIORÉ pour plus de détails
            string prompt = "highly detailed futuristic research terminal, glowing holographic display with floating data, metallic chrome surface with LED strips, blue neon accents, sci-fi control panels, detailed buttons and screens, professional quality, realistic materials";
            
            CSMResponse response = await GenerateModelWithMeshy(prompt, "terminal_recherche_hd");
            
            if (response.IsSuccess())
            {
                Debug.Log($"🎉 TEST HD RÉUSSI ! URL: {response.download_url}");
                
                CSMModelImporter importer = CSMModelImporter.GetOrCreateInstance();
                if (importer != null)
                {
                    GameObject imported = await importer.ImportModelFromCSMResponse(response, "terminal_recherche_hd");
                    
                    if (imported != null)
                    {
                        Vector3 spawnPos = Vector3.zero + Vector3.right * 5f;
                        Instantiate(imported, spawnPos, Quaternion.identity);
                        Debug.Log($"👁️ Terminal HD spawné à {spawnPos} !");
                    }
                }
            }
            else if (response.status == "cached")
            {
                Debug.Log($"✅ TERMINAL HD TROUVÉ EN CACHE !");
            }
            else
            {
                Debug.LogError($"❌ TEST HD ÉCHOUÉ: {response.message}");
            }
        }
        [ContextMenu("Test Meshy Custom")]
        public async void TestMeshyCustom()
        {
            if (!useRealMeshyAPI || string.IsNullOrEmpty(config?.apiKey))
            {
                Debug.LogWarning("⚠️ Configurez d'abord votre clé API Meshy et activez useRealMeshyAPI");
                return;
            }
            
            // CHANGEZ CET OBJET pour tester différents items
            string testItemName = "artefact_alien";  // ← Modifiez ici !
            
            Debug.Log($"🧪 === TEST MESHY CUSTOM: {testItemName} ===");
            
            CSMResponse response = await GenerateModelFromItemName(testItemName);
            
            if (response.IsSuccess())
            {
                Debug.Log($"🎉 TEST CUSTOM RÉUSSI ! URL: {response.download_url}");
                
                // Import automatique
                if (CSMModelImporter.Instance != null)
                {
                    GameObject imported = await CSMModelImporter.Instance.ImportModelFromCSMResponse(response, testItemName);
                    
                    if (imported != null)
                    {
                        // Spawn à une position aléatoire
                        Vector3 spawnPos = new Vector3(
                            UnityEngine.Random.Range(-3f, 3f), 
                            0, 
                            UnityEngine.Random.Range(-3f, 3f)
                        );
                        Instantiate(imported, spawnPos, Quaternion.identity);
                        Debug.Log($"👁️ {testItemName} spawné à {spawnPos} !");
                    }
                }
            }
            else if (response.status == "cached")
            {
                Debug.Log($"✅ OBJET TROUVÉ EN CACHE: {testItemName} - Aucune génération nécessaire !");
            }
            else
            {
                Debug.LogError($"❌ TEST CUSTOM ÉCHOUÉ: {response.message}");
            }
        }
        
        /// <summary>
        /// Crée une réponse pour un objet trouvé en cache
        /// </summary>
        CSMResponse CreateCacheResponse(string objectName, string cachedObjectName)
        {
            return new CSMResponse
            {
                status = "cached",
                message = $"Objet trouvé en cache: {cachedObjectName}",
                generation_id = "cache_" + System.Guid.NewGuid().ToString().Substring(0, 8),
                download_url = "cached://local",
                preview_url = "",
                actual_triangles = 0,
                file_size_bytes = 0,
                file_format = "cached",
                generation_time_seconds = 0f,
                started_at = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                completed_at = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                quality_score = 100,
                prompt_match_score = 100
            };
        }
        
        /// <summary>
        /// Affiche les statistiques
        /// </summary>
        [ContextMenu("Show Meshy Stats")]
        public void ShowMeshyStats()
        {
            Debug.Log($@"📊 STATISTIQUES MESHY
API Activée: {useRealMeshyAPI}
Clé API: {(string.IsNullOrEmpty(config?.apiKey) ? "❌ Non configurée" : "✅ Configurée")}
Requêtes actives: {activeRequests}
Total envoyées: {totalRequestsSent}
Succès: {successfulGenerations}
Échecs: {failedGenerations}
Taux succès: {(totalRequestsSent > 0 ? (successfulGenerations * 100f / totalRequestsSent):0):F1}%");
        }
    }
    
    // Structures pour l'API Meshy
    [System.Serializable]
    public class MeshyCreateRequest
    {
        public string mode;
        public string prompt;
        public string art_style;
        public string negative_prompt;
    }
    
    [System.Serializable]
    public class MeshyStartResponse
    {
        public string result; // Task ID
    }
    
    [System.Serializable]
    public class MeshyStatusResponse
    {
        public string status; // "PENDING", "IN_PROGRESS", "SUCCEEDED", "FAILED"
        public string created_at;
        public string thumbnail_url;
        public MeshyModelUrls model_urls;
    }
    
    [System.Serializable]
    public class MeshyModelUrls
    {
        public string fbx;
        public string obj;
        public string mtl;
    }
}