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
        Debug.Log("\n========== SCENE HIERARCHY: " + currentScene.name + " ==========\n");
        Debug.Log("Legend: ✓ = Active, ✗ = Inactive, 📜 = Has Scripts\n");
        
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        
        foreach (GameObject rootObj in rootObjects)
        {
            PrintGameObjectHierarchy(rootObj, 0);
        }
        
        Debug.Log("\n========== END OF HIERARCHY ==========\n");
    }
    
    [ContextMenu("Print Detailed Hierarchy with Scripts")]
    public void PrintDetailedHierarchy()
    {
        showComponents = true; // Force show components
        PrintSceneHierarchy();
    }
    
    [ContextMenu("Print Hierarchy Summary")]
    public void PrintHierarchySummary()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log("\n========== SCENE SUMMARY: " + currentScene.name + " ==========");
        
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
        
        Debug.Log("Total GameObjects: " + totalObjects);
        Debug.Log("Active GameObjects: " + activeObjects);
        Debug.Log("GameObjects with Components: " + withComponents);
        
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
        
        // Check if has scripts
        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
        bool hasScripts = scripts.Length > 0;
        string scriptIcon = hasScripts ? "📜" : "  ";
        
        // Build object info
        StringBuilder info = new StringBuilder();
        info.Append(indent + status + " " + scriptIcon + " " + obj.name);
        
        // Add tag if not default
        if (!string.IsNullOrEmpty(obj.tag) && obj.tag != "Untagged")
        {
            info.Append(" [Tag: " + obj.tag + "]");
        }
        
        // Add layer if not default
        if (obj.layer != 0)
        {
            info.Append(" [Layer: " + LayerMask.LayerToName(obj.layer) + "]");
        }
        
        // First line - basic info
        Debug.Log(info.ToString());
        
        // Add components/scripts on separate lines with proper indentation
        if (showComponents || hasScripts)
        {
            Component[] components = obj.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp is Transform) continue; // Skip Transform
                
                string compIndent = indent + "    → ";
                
                if (comp == null)
                {
                    Debug.Log(compIndent + "<color=red>Missing Script!</color>");
                }
                else
                {
                    string compName = comp.GetType().Name;
                    string compInfo = compIndent + "<color=yellow>" + compName + "</color>";
                    
                    // Add extra info for specific components
                    if (comp is MonoBehaviour)
                    {
                        compInfo += " (Script)";
                        
                        // Special handling for project scripts
                        if (comp is SaveGameManager)
                            compInfo += " <color=green>[Save System]</color>";
                        else if (comp is PlayerControllerCC)
                            compInfo += " <color=cyan>[Player]</color>";
                        else if (comp is NPC)
                            compInfo += " <color=magenta>[NPC: " + ((NPC)comp).npcName + "]</color>";
                        else if (comp is QuestManager)
                            compInfo += " <color=orange>[Quest System]</color>";
                        else if (comp.GetType().Name.Contains("UI"))
                            compInfo += " <color=lightblue>[UI]</color>";
                    }
                    else if (comp is Collider)
                    {
                        Collider col = (Collider)comp;
                        compInfo += " (" + col.GetType().Name + ", Trigger: " + col.isTrigger + ")";
                    }
                    else if (comp is Camera)
                    {
                        Camera cam = (Camera)comp;
                        compInfo += " (Depth: " + cam.depth + ", Type: " + (cam.orthographic ? "Orthographic" : "Perspective") + ")";
                    }
                    
                    Debug.Log(compInfo);
                }
            }
        }
        
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
            Debug.Log("  " + componentName + ": " + components.Length);
        }
    }
    
    [ContextMenu("Print All Scripts in Scene")]
    public void PrintAllScripts()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log("\n========== ALL SCRIPTS IN SCENE: " + currentScene.name + " ==========\n");
        
        // Get all MonoBehaviours including inactive
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>(true);
        
        // Group by script type
        var scriptGroups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<MonoBehaviour>>();
        
        foreach (var script in allScripts)
        {
            if (script == null) continue;
            
            string typeName = script.GetType().Name;
            if (!scriptGroups.ContainsKey(typeName))
                scriptGroups[typeName] = new System.Collections.Generic.List<MonoBehaviour>();
            
            scriptGroups[typeName].Add(script);
        }
        
        // Sort and print
        var sortedKeys = new System.Collections.Generic.List<string>(scriptGroups.Keys);
        sortedKeys.Sort();
        
        foreach (string scriptType in sortedKeys)
        {
            Debug.Log("\n<color=yellow>" + scriptType + "</color> (" + scriptGroups[scriptType].Count + " instances):");
            
            foreach (var script in scriptGroups[scriptType])
            {
                string path = GetGameObjectPath(script.gameObject);
                string active = script.gameObject.activeInHierarchy ? "✓" : "✗";
                Debug.Log("  " + active + " " + path);
            }
        }
        
        Debug.Log("\n\nTotal unique script types: " + scriptGroups.Count);
        Debug.Log("Total script instances: " + allScripts.Length);
        Debug.Log("\n========== END OF SCRIPTS ==========\n");
    }
    
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    [ContextMenu("Export Hierarchy to File")]
    public void ExportHierarchyToFile()
    {
        StringBuilder sb = new StringBuilder();
        Scene currentScene = SceneManager.GetActiveScene();
        
        // Create structured output for parsing
        sb.AppendLine("SCENE_NAME: " + currentScene.name);
        sb.AppendLine("EXPORT_DATE: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("UNITY_VERSION: " + Application.unityVersion);
        sb.AppendLine("=====================================\n");
        
        sb.AppendLine("HIERARCHY:");
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        
        foreach (GameObject rootObj in rootObjects)
        {
            ExportGameObjectToStringBuilder(rootObj, 0, sb);
        }
        
        // Add script summary
        sb.AppendLine("\n\nSCRIPT_SUMMARY:");
        ExportScriptSummary(sb);
        
        // Save to Scripts/_Documentation folder
        string docPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "_Documentation");
        
        // Create directory if it doesn't exist
        if (!System.IO.Directory.Exists(docPath))
        {
            System.IO.Directory.CreateDirectory(docPath);
        }
        
        string fileName = "SceneHierarchy_" + currentScene.name + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string filePath = System.IO.Path.Combine(docPath, fileName);
        
        System.IO.File.WriteAllText(filePath, sb.ToString());
        
        Debug.Log("<color=green>Hierarchy exported to: " + filePath + "</color>");
        
        // Refresh AssetDatabase in Editor
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
        
        // Open folder in explorer (Windows)
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", "/select," + filePath.Replace('/', '\\'));
        #endif
    }
    
    void ExportGameObjectToStringBuilder(GameObject obj, int depth, StringBuilder sb)
    {
        string indent = new string('\t', depth); // Use tabs for easier parsing
        string status = obj.activeInHierarchy ? "ACTIVE" : "INACTIVE";
        
        // GameObject line format: [DEPTH]\t[STATUS]\t[NAME]\t[TAG]\t[LAYER]
        sb.Append(indent + "GAMEOBJECT|" + status + "|" + obj.name);
        
        if (!string.IsNullOrEmpty(obj.tag) && obj.tag != "Untagged")
            sb.Append("|TAG:" + obj.tag);
            
        if (obj.layer != 0)
            sb.Append("|LAYER:" + LayerMask.LayerToName(obj.layer));
            
        sb.AppendLine();
        
        // Components
        Component[] components = obj.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp == null)
            {
                sb.AppendLine(indent + "\tCOMPONENT|MISSING_SCRIPT");
                continue;
            }
            
            if (comp is Transform) continue; // Skip Transform
            
            string compType = comp.GetType().Name;
            string compNamespace = comp.GetType().Namespace ?? "UnityEngine";
            sb.Append(indent + "\tCOMPONENT|" + compType + "|" + compNamespace);
            
            // Add extra data for specific components
            if (comp is MonoBehaviour)
            {
                sb.Append("|SCRIPT");
                
                // Add specific data for known scripts
                if (comp is NPC)
                {
                    NPC npc = comp as NPC;
                    sb.Append("|NPC_NAME:" + npc.npcName);
                }
                else if (comp is Camera)
                {
                    Camera cam = comp as Camera;
                    sb.Append("|DEPTH:" + cam.depth + "|ORTHO:" + cam.orthographic);
                }
                else if (comp is Canvas)
                {
                    Canvas canvas = comp as Canvas;
                    sb.Append("|RENDER_MODE:" + canvas.renderMode);
                }
            }
            
            sb.AppendLine();
        }
        
        // Children
        foreach (Transform child in obj.transform)
        {
            ExportGameObjectToStringBuilder(child.gameObject, depth + 1, sb);
        }
    }
    
    void ExportScriptSummary(StringBuilder sb)
    {
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>(true);
        var scriptGroups = new System.Collections.Generic.Dictionary<string, int>();
        
        foreach (var script in allScripts)
        {
            if (script == null) continue;
            string typeName = script.GetType().Name;
            
            if (!scriptGroups.ContainsKey(typeName))
                scriptGroups[typeName] = 0;
            
            scriptGroups[typeName]++;
        }
        
        foreach (var kvp in scriptGroups)
        {
            sb.AppendLine("SCRIPT|" + kvp.Key + "|COUNT:" + kvp.Value);
        }
    }
}
