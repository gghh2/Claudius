using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

/// <summary>
/// Debug tool to print scene hierarchy to console
/// </summary>
public class HierarchyDebugger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showComponents = false;
    [SerializeField] private bool showInactiveObjects = true;
    [SerializeField] private int maxDepth = 10;
    
    [ContextMenu("Print Current Scene Hierarchy")]
    public void PrintSceneHierarchy()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"\n========== SCENE HIERARCHY: {currentScene.name} ==========\n");
        
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        
        foreach (GameObject rootObj in rootObjects)
        {
            PrintGameObjectHierarchy(rootObj, 0);
        }
        
        Debug.Log("\n========== END OF HIERARCHY ==========\n");
    }
    
    [ContextMenu("Print Hierarchy Summary")]
    public void PrintHierarchySummary()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"\n========== SCENE SUMMARY: {currentScene.name} ==========");
        
        int totalObjects = 0;
        int activeObjects = 0;
        int withComponents = 0;
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            totalObjects++;
            if (obj.activeInHierarchy) activeObjects++;
            if (obj.GetComponents<Component>().Length > 1) withComponents++; // >1 because Transform is always there
        }
        
        Debug.Log($"Total GameObjects: {totalObjects}");
        Debug.Log($"Active GameObjects: {activeObjects}");
        Debug.Log($"GameObjects with Components: {withComponents}");
        
        // List important components
        Debug.Log("\n--- Key Components Found ---");
        LogComponentCount<Camera>("Cameras");
        LogComponentCount<Light>("Lights");
        LogComponentCount<Canvas>("UI Canvas");
        LogComponentCount<AudioSource>("Audio Sources");
        LogComponentCount<Collider>("Colliders");
        LogComponentCount<Rigidbody>("Rigidbodies");
        
        // Project specific
        Debug.Log("\n--- Project Specific ---");
        LogComponentCount<PlayerControllerCC>("Player Controllers");
        LogComponentCount<NPC>("NPCs");
        LogComponentCount<QuestManager>("Quest Managers");
        LogComponentCount<SaveGameManager>("Save Managers");
        
        Debug.Log("\n========== END OF SUMMARY ==========\n");
    }
    
    void PrintGameObjectHierarchy(GameObject obj, int depth)
    {
        if (depth > maxDepth) return;
        
        if (!showInactiveObjects && !obj.activeInHierarchy) return;
        
        // Create indentation
        string indent = new string(' ', depth * 2);
        
        // Object status
        string status = obj.activeInHierarchy ? "✓" : "✗";
        
        // Build object info
        StringBuilder info = new StringBuilder();
        info.Append($"{indent}{status} {obj.name}");
        
        // Add tag if not default
        if (!string.IsNullOrEmpty(obj.tag) && obj.tag != "Untagged")
        {
            info.Append($" [Tag: {obj.tag}]");
        }
        
        // Add layer if not default
        if (obj.layer != 0)
        {
            info.Append($" [Layer: {LayerMask.LayerToName(obj.layer)}]");
        }
        
        // Add components if requested
        if (showComponents)
        {
            Component[] components = obj.GetComponents<Component>();
            if (components.Length > 1) // More than just Transform
            {
                info.Append(" <");
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] is Transform) continue;
                    if (components[i] == null)
                    {
                        info.Append("Missing Script, ");
                        continue;
                    }
                    info.Append(components[i].GetType().Name);
                    if (i < components.Length - 1) info.Append(", ");
                }
                info.Append(">");
            }
        }
        
        Debug.Log(info.ToString());
        
        // Print children
        foreach (Transform child in obj.transform)
        {
            PrintGameObjectHierarchy(child.gameObject, depth + 1);
        }
    }
    
    void LogComponentCount<T>(string componentName) where T : Component
    {
        T[] components = FindObjectsOfType<T>(true);
        if (components.Length > 0)
        {
            Debug.Log($"  {componentName}: {components.Length}");
        }
    }
    
    [ContextMenu("Export Hierarchy to File")]
    public void ExportHierarchyToFile()
    {
        StringBuilder sb = new StringBuilder();
        Scene currentScene = SceneManager.GetActiveScene();
        
        sb.AppendLine($"SCENE HIERARCHY: {currentScene.name}");
        sb.AppendLine($"Date: {System.DateTime.Now}");
        sb.AppendLine("=====================================\n");
        
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        
        foreach (GameObject rootObj in rootObjects)
        {
            ExportGameObjectToStringBuilder(rootObj, 0, sb);
        }
        
        // Save to persistent data path
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"SceneHierarchy_{currentScene.name}_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        System.IO.File.WriteAllText(filePath, sb.ToString());
        
        Debug.Log($"Hierarchy exported to: {filePath}");
        
        // Open folder in explorer (Windows)
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", "/select," + filePath.Replace('/', '\\'));
        #endif
    }
    
    void ExportGameObjectToStringBuilder(GameObject obj, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        string status = obj.activeInHierarchy ? "[Active]" : "[Inactive]";
        
        sb.AppendLine($"{indent}{obj.name} {status}");
        
        foreach (Transform child in obj.transform)
        {
            ExportGameObjectToStringBuilder(child.gameObject, depth + 1, sb);
        }
    }
}