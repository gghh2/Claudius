using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using DynamicAssets.Generation.Config;
using DynamicAssets.Generation.API;

namespace DynamicAssets.Generation.API
{
    /// <summary>
    /// Générateur principal pour l'API CSM - VERSION PHASE 2B.2
    /// Gère la communication RÉELLE avec les serveurs CSM
    /// </summary>
    public class CSMGenerator : MonoBehaviour
    {
        public static CSMGenerator Instance { get; private set; }
        
        [Header("Configuration")]
        [Tooltip("Configuration CSM à utiliser")]
        public CSMConfig config;
        
        [Header("Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private bool isTesting = false;
        [SerializeField] private int activeRequests = 0;
        
        [Header("Statistics")]
        [SerializeField] private int totalRequestsSent = 0;
        [SerializeField] private int successfulGenerations = 0;
        [SerializeField] private int failedGenerations = 0;
        
        [Header("Phase 2B Settings")]
        [SerializeField] private bool useRealAPI = false; // Toggle pour Phase 2
        
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
            if (config != null)
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log("🔌 CSMGenerator initialisé (Phase 2B.2)");
                
                // Test automatique de connexion si config valide
                if (config.IsValid())
                {
                    StartCoroutine(TestConnectionCoroutine());
                }
                else
                {
                    Debug.LogWarning("⚠️ Configuration CSM invalide - test de connexion ignoré");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Aucune configuration CSM assignée");
            }
        }
        
        /// <summary>
        /// Teste la connexion avec l'API CSM
        /// </summary>
        [ContextMenu("Test Connection")]
        public void TestConnection()
        {
            if (isTesting)
            {
                Debug.LogWarning("⚠️ Test de connexion déjà en cours");
                return;
            }
            
            StartCoroutine(TestConnectionCoroutine());
        }
        
        /// <summary>
        /// Coroutine pour tester la connexion
        /// </summary>
        IEnumerator TestConnectionCoroutine()
        {
            if (config == null)
            {
                Debug.LogError("❌ Configuration CSM manquante pour test connexion");
                yield break;
            }
            
            isTesting = true;
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log("🔍 Test de connexion CSM...");
            
            using (UnityWebRequest request = new UnityWebRequest())
            {
                // Configuration de la requête de test
                request.method = "GET";
                request.url = config.apiUrl.Replace("/generate", "/health"); // Endpoint hypothétique
                request.downloadHandler = new DownloadHandlerBuffer();
                
                // Headers d'authentification
                if (!string.IsNullOrEmpty(config.apiKey))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                }
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("User-Agent", "Unity-DynamicAssets/1.0");
                
                // Timeout
                request.timeout = (int)config.downloadTimeout;
                
                // Envoi de la requête
                yield return request.SendWebRequest();
                
                // Traitement de la réponse
                if (request.result == UnityWebRequest.Result.Success)
                {
                    isConnected = true;
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    {
                        Debug.Log("✅ Connexion CSM réussie !");
                        Debug.Log($"📊 Code réponse: {request.responseCode}");
                    }
                    
                    if (config.debugMode)
                    {
                        Debug.Log($"📄 Réponse: {request.downloadHandler.text}");
                    }
                }
                else
                {
                    isConnected = false;
                    HandleConnectionError(request);
                }
            }
            
            isTesting = false;
        }
        
        /// <summary>
        /// Gère les erreurs de connexion
        /// </summary>
        void HandleConnectionError(UnityWebRequest request)
        {
            string errorMessage = "";
            
            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    errorMessage = "Erreur de connexion - Vérifiez votre internet";
                    break;
                    
                case UnityWebRequest.Result.ProtocolError:
                    errorMessage = $"Erreur protocole (Code {request.responseCode})";
                    
                    // Messages spécifiques selon le code d'erreur
                    switch (request.responseCode)
                    {
                        case 401:
                            errorMessage += " - Clé API invalide";
                            break;
                        case 403:
                            errorMessage += " - Accès refusé";
                            break;
                        case 404:
                            errorMessage += " - API CSM introuvable";
                            break;
                        case 429:
                            errorMessage += " - Trop de requêtes, attendez";
                            break;
                        case 500:
                            errorMessage += " - Erreur serveur CSM";
                            break;
                    }
                    break;
                    
                case UnityWebRequest.Result.DataProcessingError:
                    errorMessage = "Erreur traitement des données";
                    break;
                    
                default:
                    errorMessage = $"Erreur inconnue: {request.error}";
                    break;
            }
            
            Debug.LogError($"❌ Test connexion CSM échoué: {errorMessage}");
            
            if (config.debugMode && !string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                Debug.LogError($"📄 Détails erreur: {request.downloadHandler.text}");
            }
        }
        
        /// <summary>
        /// NOUVELLE MÉTHODE : Génère un modèle 3D avec vraie API
        /// </summary>
        public async Task<CSMResponse> GenerateModel(string prompt, string objectName)
        {
            if (config == null)
            {
                Debug.LogError("❌ Configuration CSM manquante");
                return CSMResponse.CreateErrorResponse("Configuration manquante");
            }
            
            if (!config.IsValid())
            {
                Debug.LogError("❌ Configuration CSM invalide");
                return CSMResponse.CreateErrorResponse("Configuration invalide");
            }
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
            {
                Debug.Log($"🎨 Génération CSM demandée: {objectName}");
                Debug.Log($"📝 Prompt: {prompt}");
            }
            
            activeRequests++;
            totalRequestsSent++;
            
            try
            {
                if (useRealAPI && !string.IsNullOrEmpty(config.apiKey))
                {
                    // VRAIE GÉNÉRATION via API
                    return await GenerateRealModel(prompt, objectName);
                }
                else
                {
                    // MODE SIMULATION (pour Phase 2 sans vraie clé API)
                    return await GenerateSimulatedModel(prompt, objectName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erreur génération {objectName}: {e.Message}");
                failedGenerations++;
                return CSMResponse.CreateErrorResponse($"Erreur génération: {e.Message}");
            }
            finally
            {
                activeRequests--;
            }
        }
        
        /// <summary>
        /// GÉNÉRATION RÉELLE via API CSM
        /// </summary>
        async Task<CSMResponse> GenerateRealModel(string prompt, string objectName)
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log("🌐 === GÉNÉRATION RÉELLE CSM ===");
            
            // Convertit le prompt en requête CSM formatée
            CSMRequest csmRequest = CSMPromptConverter.ConvertToCSMRequest(prompt, objectName, config);
            
            // Valide la requête
            if (!CSMPromptConverter.ValidateRequest(csmRequest))
            {
                return CSMResponse.CreateErrorResponse("Requête CSM invalide");
            }
            
            // Prépare la requête HTTP
            string jsonData = csmRequest.ToJson();
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"📤 Envoi requête JSON: {jsonData}");
            
            // Délégue à la coroutine
            CSMResponse response = null;
            bool requestCompleted = false;
            
            StartCoroutine(SendRealCSMRequest(jsonData, objectName, (result) => {
                response = result;
                requestCompleted = true;
            }));
            
            // Attend la fin de la coroutine
            while (!requestCompleted)
            {
                await Task.Delay(100);
            }
            
            return response ?? CSMResponse.CreateErrorResponse("Timeout de requête");
        }
        
        /// <summary>
        /// Coroutine pour envoyer la vraie requête CSM
        /// </summary>
        IEnumerator SendRealCSMRequest(string jsonData, string objectName, System.Action<CSMResponse> callback)
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(config.apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                
                // Headers pour CSM
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                request.SetRequestHeader("User-Agent", "Unity-DynamicAssets/1.0");
                
                // Timeout de génération
                request.timeout = (int)config.generationTimeout;
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log($"🚀 Envoi requête CSM vers: {config.apiUrl}");
                
                // Envoi
                yield return request.SendWebRequest();
                
                // Traitement de la réponse
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                        Debug.Log($"✅ Réponse CSM reçue: {responseText}");
                    
                    try
                    {
                        CSMResponse csmResponse = CSMResponse.FromJson(responseText);
                        
                        if (csmResponse != null && csmResponse.IsValid())
                        {
                            successfulGenerations++;
                            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                                Debug.Log($"🎉 Génération réussie: {objectName}");
                            callback(csmResponse);
                        }
                        else
                        {
                            failedGenerations++;
                            Debug.LogError("❌ Réponse CSM invalide");
                            callback(CSMResponse.CreateErrorResponse("Réponse API invalide"));
                        }
                    }
                    catch (System.Exception e)
                    {
                        failedGenerations++;
                        Debug.LogError($"❌ Erreur parsing réponse: {e.Message}");
                        callback(CSMResponse.CreateErrorResponse($"Erreur parsing: {e.Message}"));
                    }
                }
                else
                {
                    failedGenerations++;
                    
                    string errorMsg = $"Erreur HTTP {request.responseCode}: {request.error}";
                    if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                    {
                        errorMsg += $"\nDétails: {request.downloadHandler.text}";
                    }
                    
                    Debug.LogError($"❌ Erreur requête CSM: {errorMsg}");
                    callback(CSMResponse.CreateErrorResponse(errorMsg));
                }
            }
        }
        
        /// <summary>
        /// MODE SIMULATION (pour Phase 2 sans vraie clé API)
        /// </summary>
        async Task<CSMResponse> GenerateSimulatedModel(string prompt, string objectName)
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
            {
                Debug.Log("🧪 === GÉNÉRATION SIMULÉE ===");
                Debug.Log($"Mode simulation utilisé car useRealAPI={useRealAPI}");
            }
            
            // Simule un délai de génération réaliste
            float simulatedTime = UnityEngine.Random.Range(45f, 180f);
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"⏳ Simulation génération pendant {simulatedTime:F1}s...");
            
            await Task.Delay((int)(simulatedTime * 1000));
            
            // Créé une réponse de test avec des données réalistes
            CSMResponse testResponse = CSMResponse.CreateTestSuccessResponse(objectName);
            testResponse.generation_time_seconds = simulatedTime;
            
            successfulGenerations++;
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"✅ Génération simulée terminée: {objectName}");
            
            return testResponse;
        }
        
        /// <summary>
        /// NOUVELLE MÉTHODE : Génère via nom d'objet technique (intégration avec votre système)
        /// </summary>
        public async Task<CSMResponse> GenerateModelFromItemName(string itemName)
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"🔍 Génération depuis nom d'item: {itemName}");
            
            if (config == null)
            {
                return CSMResponse.CreateErrorResponse("Configuration manquante");
            }
            
            // Utilise votre convertisseur pour créer la requête
            CSMRequest request = CSMPromptConverter.ConvertItemNameToCSMRequest(itemName, config);
            
            // Lance la génération
            return await GenerateModel(request.prompt, itemName);
        }
        
        /// <summary>
        /// Vérifie si le générateur est prêt à fonctionner
        /// </summary>
        public bool IsReady()
        {
            return config != null && config.IsValid() && !isTesting;
        }
        
        /// <summary>
        /// Active/désactive l'API réelle
        /// </summary>
        [ContextMenu("Toggle Real API")]
        public void ToggleRealAPI()
        {
            useRealAPI = !useRealAPI;
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log($"🔄 Mode API: {(useRealAPI ? "RÉELLE" : "SIMULATION")}");
        }
        
        /// <summary>
        /// Affiche les statistiques du générateur
        /// </summary>
        [ContextMenu("Show Stats")]
        public void ShowStats()
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
            {
                Debug.Log($@"📊 STATISTIQUES CSM GENERATOR (Phase 2B.2)
Configuration: {(config != null ? "✅" : "❌")}
Connexion: {(isConnected ? "✅" : "❌")}
Mode API: {(useRealAPI ? "RÉELLE" : "SIMULATION")}
En test: {(isTesting ? "✅" : "❌")}
Requêtes actives: {activeRequests}
Total envoyées: {totalRequestsSent}
Succès: {successfulGenerations}
Échecs: {failedGenerations}
Taux succès: {(totalRequestsSent > 0 ? (successfulGenerations * 100f / totalRequestsSent):0):F1}%");
            }
        }
        
        /// <summary>
        /// Teste la génération avec un objet simple
        /// </summary>
        [ContextMenu("Test Generation")]
        public async void TestGeneration()
        {
            if (!IsReady())
            {
                Debug.LogWarning("⚠️ CSMGenerator pas prêt pour test génération");
                return;
            }
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log("🧪 Test de génération CSM...");
            
            CSMResponse response = await GenerateModel(
                "simple blue cube, low-poly style", 
                "test_cube"
            );
            
            if (response.IsSuccess())
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log($"✅ Test génération réussi:\n{response}");
            }
            else
            {
                Debug.LogError($"❌ Test génération échoué:\n{response}");
            }
        }
        
        /// <summary>
        /// NOUVEAU : Teste la génération via votre système de mapping
        /// </summary>
        [ContextMenu("Test Item Generation")]
        public async void TestItemGeneration()
        {
            if (!IsReady())
            {
                Debug.LogWarning("⚠️ CSMGenerator pas prêt pour test");
                return;
            }
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log("🧪 Test génération d'item avec mapping...");
            
            CSMResponse response = await GenerateModelFromItemName("cristal_energie");
            
            if (response.IsSuccess())
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                    Debug.Log($"✅ Test génération d'item réussi:\n{response}");
            }
            else
            {
                Debug.LogError($"❌ Test génération d'item échoué:\n{response}");
            }
        }
        
        /// <summary>
        /// Réinitialise les statistiques
        /// </summary>
        [ContextMenu("Reset Stats")]
        public void ResetStats()
        {
            totalRequestsSent = 0;
            successfulGenerations = 0;
            failedGenerations = 0;
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets))
                Debug.Log("📊 Statistiques réinitialisées");
        }
    }
}