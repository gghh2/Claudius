using UnityEngine;
using DynamicAssets.Core;
using System.Threading.Tasks;

/// <summary>
/// Composant pour tester le DynamicAssetManager
/// </summary>
public class AssetManagerTester : MonoBehaviour
{
    [Header("Test Configuration")]
    public string testItemName = "cristal_energie";
    public QuestObjectType testObjectType = QuestObjectType.Item;
    
    [Header("Test Results")]
    [SerializeField] private GameObject lastLoadedPrefab;
    [SerializeField] private bool isLoading = false;
    [SerializeField] private string lastError = "";
    
    void Start()
    {
        // Test automatique au démarrage
        if (DynamicAssetManager.Instance != null)
        {
            Debug.Log("✅ DynamicAssetManager détecté - Lancement tests automatiques");
            Invoke(nameof(RunAutoTests), 1f);
        }
        else
        {
            Debug.LogWarning("⚠️ DynamicAssetManager non trouvé dans la scène !");
        }
    }
    
    async void RunAutoTests()
    {
        Debug.Log("🧪 === DÉBUT TESTS AUTOMATIQUES ===");
        
        // Test 1: Chargement asset basique
        StartCoroutine(TestBasicAssetLoadCoroutine());
        await Task.Delay(1000);
        // L'objet a pu être détruit pendant l'attente (sortie de Play,
        // rechargement de domaine pendant un build) → ne plus y toucher.
        if (this == null) return;

        // Test 2: Test fallback
        StartCoroutine(TestFallbackSystemCoroutine());
        await Task.Delay(1000);
        if (this == null) return;

        // Test 3: Test cache stats
        TestCacheStats();
        
        Debug.Log("🧪 === FIN TESTS AUTOMATIQUES ===");
    }
    
    [ContextMenu("Test Basic Asset Load")]
    public void TestBasicAssetLoad()
    {
        StartCoroutine(TestBasicAssetLoadCoroutine());
    }
    
    System.Collections.IEnumerator TestBasicAssetLoadCoroutine()
    {
        if (DynamicAssetManager.Instance == null)
        {
            Debug.LogError("❌ DynamicAssetManager non disponible");
            yield break;
        }
        
        Debug.Log($"🔍 Test chargement: {testItemName}");
        isLoading = true;
        lastError = "";
        
        // Utilise une tâche Task sans await
        var task = DynamicAssetManager.Instance.GetQuestItemPrefab(testItemName, testObjectType);
        
        // Attend que la tâche se termine
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        try
        {
            GameObject prefab = task.Result;
            if (prefab != null)
            {
                Debug.Log($"✅ Asset chargé avec succès: {prefab.name}");
                
                // Spawn optionnel pour visualiser
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    Vector3 spawnPos = transform.position + Vector3.right * 2f;
                    GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);
                    instance.name = $"Test_{testItemName}";
                    Debug.Log($"👁️ Instance créée à {spawnPos}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Aucun prefab retourné pour {testItemName}");
            }
        }
        catch (System.Exception e)
        {
            lastError = e.Message;
            Debug.LogError($"❌ Erreur chargement: {e.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }
    
    [ContextMenu("Test Fallback System")]
    public void TestFallbackSystem()
    {
        StartCoroutine(TestFallbackSystemCoroutine());
    }
    
    System.Collections.IEnumerator TestFallbackSystemCoroutine()
    {
        Debug.Log("🔄 Test système de fallback...");
        
        // Test avec un nom d'objet inexistant
        string fakeItem = "objet_completement_inexistant_12345";
        
        var task = DynamicAssetManager.Instance.GetQuestItemPrefab(fakeItem, testObjectType);
        
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        try
        {
            GameObject fallbackPrefab = task.Result;
            if (fallbackPrefab != null)
            {
                Debug.Log($"✅ Fallback fonctionnel: {fallbackPrefab.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ Fallback a retourné null");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erreur fallback: {e.Message}");
        }
    }
    
    [ContextMenu("Test Cache Stats")]
    public void TestCacheStats()
    {
        if (DynamicAssetManager.Instance != null)
        {
            DynamicAssetManager.Instance.ShowCacheStats();
        }
    }
    
    [ContextMenu("Test Multiple Assets")]
    public void TestMultipleAssets()
    {
        StartCoroutine(TestMultipleAssetsCoroutine());
    }
    
    System.Collections.IEnumerator TestMultipleAssetsCoroutine()
    {
        string[] testItems = {
            "cristal_energie",
            "echantillon_alien", 
            "medicament_rare",
            "artefact_alien",
            "composant_electronique"
        };
        
        Debug.Log($"🔄 Test chargement multiple: {testItems.Length} assets");
        
        foreach (string item in testItems)
        {
            var task = DynamicAssetManager.Instance.GetQuestItemPrefab(item);
            
            // Attend HORS du try/catch
            while (!task.IsCompleted)
            {
                yield return null;
            }
            
            // Traite le résultat
            try
            {
                GameObject prefab = task.Result;
                Debug.Log($"  • {item}: {(prefab != null ? "✅" : "❌")}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"  • {item}: ❌ {e.Message}");
            }
            
            // Petit délai entre chaque test
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("✅ Test multiple terminé");
    }
    
    [ContextMenu("Add Test Asset to Cache")]
    public void AddTestAssetToCache()
    {
        if (DynamicAssetManager.Instance == null) return;
        
        // Utilise un prefab existant comme test
        GameObject testPrefab = lastLoadedPrefab ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        string testName = $"test_asset_{System.DateTime.Now.Ticks % 10000}";
        DynamicAssetManager.Instance.AddAssetToCache(testName, testPrefab);
        
        Debug.Log($"✅ Asset test ajouté: {testName}");
    }
    
    [ContextMenu("Cleanup Cache")]
    public void TestCleanupCache()
    {
        if (DynamicAssetManager.Instance != null)
        {
            DynamicAssetManager.Instance.CleanupCache();
        }
    }
    
    [ContextMenu("Clear Memory Cache")]
    public void TestClearMemoryCache()
    {
        if (DynamicAssetManager.Instance != null)
        {
            DynamicAssetManager.Instance.ClearMemoryCache();
        }
    }
    
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        // Vérifie si le debug des assets dynamiques est activé
        if (!GlobalDebugManager.IsDebugEnabled(DebugSystem.DynamicAssets)) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== ASSET MANAGER TESTER ===");
        
        GUILayout.Label($"Item: {testItemName}");
        GUILayout.Label($"Type: {testObjectType}");
        GUILayout.Label($"Loading: {isLoading}");
        
        if (!string.IsNullOrEmpty(lastError))
        {
            GUI.color = Color.red;
            GUILayout.Label($"Error: {lastError}");
            GUI.color = Color.white;
        }
        
        if (lastLoadedPrefab != null)
        {
            GUI.color = Color.green;
            GUILayout.Label($"Loaded: {lastLoadedPrefab.name}");
            GUI.color = Color.white;
        }
        
        if (GUILayout.Button("Test Load"))
        {
            StartCoroutine(TestBasicAssetLoadCoroutine());
        }
        
        if (GUILayout.Button("Test Multiple"))
        {
            StartCoroutine(TestMultipleAssetsCoroutine());
        }
        
        GUILayout.EndArea();
    }
}