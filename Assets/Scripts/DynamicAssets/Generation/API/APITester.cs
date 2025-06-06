using UnityEngine;
using DynamicAssets.Generation.API;
using DynamicAssets.Generation.Config;

namespace DynamicAssets.Generation.API
{
    /// <summary>
    /// Testeur pour valider les structures de données API
    /// </summary>
    public class APITester : MonoBehaviour
    {
        [Header("Test Configuration")]
        public CSMConfig testConfig;
        
        void Start()
        {
            if (testConfig != null)
            {
                RunAPITests();
            }
            else
            {
                Debug.LogWarning("⚠️ Assignez un CSMConfig pour tester les APIs");
            }
        }
        
        [ContextMenu("Test Request Creation")]
        public void TestRequestCreation()
        {
            Debug.Log("🧪 Test création de requête CSM...");
            
            // Crée une requête de test
            CSMRequest request = new CSMRequest(
                "glowing blue energy crystal with magical aura",
                "cristal_energie",
                testConfig
            );
            
            // Valide la requête
            if (request.IsValid())
            {
                Debug.Log("✅ Requête valide");
                Debug.Log($"📊 Estimation temps: {request.EstimateGenerationTimeSeconds():F0}s");
                Debug.Log($"📊 Estimation taille: {request.EstimateFileSizeMB():F1}MB");
            }
            else
            {
                Debug.LogError("❌ Requête invalide");
            }
            
            // Test JSON
            string json = request.ToJson();
            Debug.Log($"📄 JSON généré:\n{json}");
        }
        
        [ContextMenu("Test Response Parsing")]
        public void TestResponseParsing()
        {
            Debug.Log("🧪 Test parsing de réponse CSM...");
            
            // Crée une réponse de test
            CSMResponse testResponse = CSMResponse.CreateTestSuccessResponse("cristal_energie");
            
            Debug.Log($"✅ Réponse test créée:\n{testResponse}");
            
            // Test sérialisation/désérialisation
            string json = JsonUtility.ToJson(testResponse, true);
            Debug.Log($"📄 JSON de réponse:\n{json}");
            
            // Test parsing inverse
            CSMResponse parsed = CSMResponse.FromJson(json);
            if (parsed != null && parsed.IsValid())
            {
                Debug.Log("✅ Parsing JSON réussi");
                Debug.Log($"📊 Statut: {parsed.GetStatusMessage()}");
                Debug.Log($"📊 Peut télécharger: {(parsed.CanDownload() ? "✅" : "❌")}");
            }
            else
            {
                Debug.LogError("❌ Parsing JSON échoué");
            }
        }
        
        [ContextMenu("Test Error Response")]
        public void TestErrorResponse()
        {
            Debug.Log("🧪 Test réponse d'erreur...");
            
            CSMResponse errorResponse = CSMResponse.CreateErrorResponse("API Key invalide", 401);
            
            Debug.Log($"❌ Réponse d'erreur:\n{errorResponse}");
            Debug.Log($"📊 Est en échec: {errorResponse.IsFailed()}");
            Debug.Log($"📊 Peut télécharger: {errorResponse.CanDownload()}");
        }
        
        void RunAPITests()
        {
            Debug.Log("🧪 === DÉBUT TESTS API STRUCTURES ===");
            
            TestRequestCreation();
            TestResponseParsing();
            TestErrorResponse();
            
            Debug.Log("🧪 === FIN TESTS API STRUCTURES ===");
        }
    }
}