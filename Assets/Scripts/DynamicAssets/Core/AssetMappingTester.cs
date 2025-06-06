using UnityEngine;
using DynamicAssets.Core;

/// <summary>
/// Composant pour tester les mappings facilement
/// </summary>
public class AssetMappingTester : MonoBehaviour
{
    [Header("Test Mapping")]
    public string testItemName = "cristal_energie";
    
    [Header("Results")]
    [TextArea(3, 6)]
    public string lastGeneratedPrompt = "";
    
    void Start()
    {
        // Test automatique au démarrage
        TestGetPrompt();
        ListAllMappings();
    }
    
    [ContextMenu("Test Get Prompt")]
    public void TestGetPrompt()
    {
        string prompt = SimpleAssetMapping.GetVisualPrompt(testItemName);
        lastGeneratedPrompt = prompt;
        Debug.Log($"🎨 Prompt pour '{testItemName}':\n{prompt}");
    }
    
    [ContextMenu("List All Mappings")]
    public void ListAllMappings()
    {
        string[] items = SimpleAssetMapping.GetAllItemNames();
        Debug.Log($"📋 {items.Length} mappings disponibles:");
        foreach (string item in items)
        {
            string prompt = SimpleAssetMapping.GetVisualPrompt(item);
            Debug.Log($"  • {item} → {prompt}");
        }
    }
    
    [ContextMenu("Test Random Item")]
    public void TestRandomItem()
    {
        string[] items = SimpleAssetMapping.GetAllItemNames();
        if (items.Length > 0)
        {
            testItemName = items[Random.Range(0, items.Length)];
            TestGetPrompt();
        }
    }
    
    [ContextMenu("Test Unknown Item")]
    public void TestUnknownItem()
    {
        testItemName = "objet_inexistant_test";
        TestGetPrompt();
        Debug.Log("↑ Ceci devrait montrer le fallback automatique");
    }
}