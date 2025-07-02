using UnityEngine;
using UnityEditor;

/// <summary>
/// Utility to fix ModernPauseMenu references in the project
/// </summary>
public class FixModernPauseMenuReferences : MonoBehaviour
{
    [MenuItem("Tools/Fix ModernPauseMenu References")]
    public static void FixReferences()
    {
        Debug.Log("=== Fixing ModernPauseMenu References ===");
        
        // Find all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int fixedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            // Check all components
            Component[] components = obj.GetComponents<Component>();
            
            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    Debug.LogWarning($"Found missing component on {obj.name} - cleaning up");
                    continue;
                }
                
                // Special handling for scripts that might reference ModernPauseMenu
                if (comp is SaveMenuIntegration)
                {
                    Debug.Log($"Found SaveMenuIntegration on {obj.name}");
                    
                    // Check if it has PauseMenuUI
                    PauseMenuUI pauseMenu = obj.GetComponent<PauseMenuUI>();
                    if (pauseMenu == null)
                    {
                        Debug.LogWarning($"Adding PauseMenuUI to {obj.name}");
                        obj.AddComponent<PauseMenuUI>();
                        fixedCount++;
                    }
                }
            }
        }
        
        // Also check prefabs in the project
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                Component[] components = prefab.GetComponentsInChildren<Component>(true);
                bool modified = false;
                
                foreach (Component comp in components)
                {
                    if (comp == null)
                    {
                        Debug.LogWarning($"Found missing component in prefab {path}");
                        modified = true;
                    }
                }
                
                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                    fixedCount++;
                }
            }
        }
        
        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Fixed {fixedCount} references. Please check the console for any remaining issues.");
        }
        else
        {
            Debug.Log("No references needed fixing.");
        }
        
        Debug.Log("=== Fix Complete ===");
    }
    
    [MenuItem("Tools/Clean Missing Scripts")]
    public static void CleanMissingScripts()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int removedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            if (count > 0)
            {
                removedCount += count;
                Debug.Log($"Removed {count} missing scripts from {obj.name}");
            }
        }
        
        Debug.Log($"Total missing scripts removed: {removedCount}");
    }
}
