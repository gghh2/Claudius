using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central manager for UI state tracking.
/// Follows SOLID principles by providing a single responsibility: tracking UI panel states.
/// </summary>
public class UIManager : MonoBehaviour
{
    // Singleton instance
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    instance = go.AddComponent<UIManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    // Dictionary to track open UI panels
    private Dictionary<string, bool> openPanels = new Dictionary<string, bool>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Register a UI panel state change
    /// </summary>
    /// <param name="panelName">Unique identifier for the panel</param>
    /// <param name="isOpen">Whether the panel is open or closed</param>
    public void SetPanelState(string panelName, bool isOpen)
    {
        if (string.IsNullOrEmpty(panelName)) return;

        if (isOpen)
        {
            openPanels[panelName] = true;
            Debug.Log($"[UIManager] Panel opened: {panelName}");
        }
        else
        {
            openPanels.Remove(panelName);
            Debug.Log($"[UIManager] Panel closed: {panelName}");
        }
    }

    /// <summary>
    /// Check if any UI panel is currently open
    /// </summary>
    /// <returns>True if any UI panel is open</returns>
    public bool IsAnyUIOpen()
    {
        return openPanels.Count > 0;
    }
    
    /// <summary>
    /// Check if any of the specified panels is open
    /// </summary>
    /// <param name="panelNames">Panel names to check</param>
    /// <returns>True if any of the specified panels is open</returns>
    public bool IsAnyPanelOpen(params string[] panelNames)
    {
        foreach (string panelName in panelNames)
        {
            if (IsPanelOpen(panelName))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if a specific UI panel is open
    /// </summary>
    /// <param name="panelName">Name of the panel to check</param>
    /// <returns>True if the specified panel is open</returns>
    public bool IsPanelOpen(string panelName)
    {
        return openPanels.ContainsKey(panelName) && openPanels[panelName];
    }

    /// <summary>
    /// Get a list of all currently open panels
    /// </summary>
    /// <returns>List of open panel names</returns>
    public List<string> GetOpenPanels()
    {
        return new List<string>(openPanels.Keys);
    }

    /// <summary>
    /// Close all UI panels (useful for emergency situations)
    /// </summary>
    public void CloseAllPanels()
    {
        // Notify all UI components to close
        // This is a safety method - individual UIs should handle their own closing logic
        
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsInventoryOpen())
        {
            InventoryUI.Instance.CloseInventory();
        }

        if (QuestJournalUI.Instance != null && QuestJournalUI.Instance.IsJournalOpen())
        {
            QuestJournalUI.Instance.CloseJournal();
        }

        if (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueOpen())
        {
            // DialogueUI doesn't have a public close method, so we'll just clear our tracking
        }

        // Clear the tracking dictionary
        openPanels.Clear();
        Debug.Log("[UIManager] All panels closed");
    }

    /// <summary>
    /// Debug method to print all open panels
    /// </summary>
    [ContextMenu("Debug - Print Open Panels")]
    public void DebugPrintOpenPanels()
    {
        if (openPanels.Count == 0)
        {
            Debug.Log("[UIManager] No panels are currently open");
            return;
        }

        Debug.Log($"[UIManager] Open panels ({openPanels.Count}):");
        foreach (var panel in openPanels)
        {
            Debug.Log($"  - {panel.Key}");
        }
    }
}

/// <summary>
/// Static class containing UI panel name constants to avoid magic strings
/// </summary>
public static class UIPanelNames
{
    public const string Inventory = "Inventory";
    public const string QuestJournal = "QuestJournal";
    public const string Dialogue = "Dialogue";
    public const string DialogueHistory = "DialogueHistory";
    public const string PauseMenu = "PauseMenu";
    public const string Settings = "Settings";
    public const string AudioSettings = "AudioSettings";
}
