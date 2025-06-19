using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Extensions and helpers for the save system
/// </summary>
public static class SaveSystemExtensions
{
    /// <summary>
    /// Add save/load support to QuestJournal
    /// </summary>
    public static void ClearAllQuests(this QuestJournal journal)
    {
        // Implementation would clear all quests
        // This is a placeholder - actual implementation depends on QuestJournal internals
        Debug.Log("[SaveSystem] Clearing all quests from journal");
    }
    
    /// <summary>
    /// Add save/load support to PlayerInventory
    /// </summary>
    public static void ClearInventory(this PlayerInventory inventory)
    {
        // Implementation would clear inventory
        Debug.Log("[SaveSystem] Clearing player inventory");
    }
    
    /// <summary>
    /// Get all items from inventory
    /// </summary>
    public static List<SaveableInventoryItem> GetAllItems(this PlayerInventory inventory)
    {
        // This would return all items - placeholder for now
        return new List<SaveableInventoryItem>();
    }
}

/// <summary>
/// Simple inventory item structure for saving
/// </summary>
[System.Serializable]
public class SaveableInventoryItem
{
    public string itemName;
    public int quantity;
    public string associatedQuestId;
}

/// <summary>
/// Interface for saveable components
/// </summary>
public interface ISaveable
{
    SaveableData GetSaveData();
    void LoadSaveData(SaveableData data);
}

/// <summary>
/// Base class for saveable data
/// </summary>
public abstract class SaveableData
{
    public string componentType;
    public string jsonData;
}

/// <summary>
/// Component to make any GameObject saveable
/// </summary>
public class SaveableObject : MonoBehaviour
{
    [Header("Save Settings")]
    public string saveId = "";
    public bool savePosition = true;
    public bool saveRotation = true;
    public bool saveActive = true;
    
    void Awake()
    {
        // Generate unique ID if not set
        if (string.IsNullOrEmpty(saveId))
        {
            saveId = System.Guid.NewGuid().ToString();
        }
    }
    
    public SaveableObjectData GetSaveData()
    {
        return new SaveableObjectData
        {
            id = saveId,
            position = transform.position,
            rotation = transform.rotation.eulerAngles,
            isActive = gameObject.activeSelf
        };
    }
    
    public void LoadSaveData(SaveableObjectData data)
    {
        if (savePosition)
            transform.position = data.position;
            
        if (saveRotation)
            transform.rotation = Quaternion.Euler(data.rotation);
            
        if (saveActive)
            gameObject.SetActive(data.isActive);
    }
}

public class SaveableObjectData
{
    public string id;
    public Vector3 position;
    public Vector3 rotation;
    public bool isActive;
}

/// <summary>
/// Quick save/load keybinds
/// </summary>
public class SaveGameKeybinds : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode quickSaveKey = KeyCode.F5;
    public KeyCode quickLoadKey = KeyCode.F9;
    public KeyCode saveMenuKey = KeyCode.Escape;
    
    [Header("Settings")]
    public bool enableKeybinds = true;
    public bool requireModifier = false;
    public KeyCode modifierKey = KeyCode.LeftControl;
    
    void Update()
    {
        if (!enableKeybinds) return;
        
        bool modifierPressed = !requireModifier || Input.GetKey(modifierKey);
        
        if (modifierPressed)
        {
            // Quick save
            if (Input.GetKeyDown(quickSaveKey))
            {
                if (SaveGameManager.Instance != null)
                {
                    SaveGameManager.Instance.SaveGame("quicksave");
                    Debug.Log("[SaveSystem] Quick save performed");
                }
            }
            
            // Quick load
            if (Input.GetKeyDown(quickLoadKey))
            {
                if (SaveGameManager.Instance != null && SaveGameManager.Instance.SaveExists("quicksave"))
                {
                    SaveGameManager.Instance.LoadGame("quicksave");
                    Debug.Log("[SaveSystem] Quick load performed");
                }
                else
                {
                    Debug.LogWarning("[SaveSystem] No quicksave found");
                }
            }
        }
    }
}

/// <summary>
/// Auto-save trigger zones
/// </summary>
public class AutoSaveTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string saveName = "autosave";
    public bool oneTimeOnly = true;
    public string triggerMessage = "Game saved";
    
    private bool hasTriggered = false;
    
    void OnTriggerEnter(Collider other)
    {
        if (oneTimeOnly && hasTriggered) return;
        
        if (other.CompareTag("Player"))
        {
            if (SaveGameManager.Instance != null)
            {
                SaveGameManager.Instance.SaveGame(saveName);
                hasTriggered = true;
                
                if (!string.IsNullOrEmpty(triggerMessage))
                {
                    Debug.Log($"[AutoSave] {triggerMessage}");
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = hasTriggered ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);
        Gizmos.DrawIcon(transform.position, "SaveIcon.png", true);
    }
}