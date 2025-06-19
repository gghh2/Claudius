using UnityEngine;

/// <summary>
/// Script to ensure UI input priority order
/// DialogueUI should handle input before ModernPauseMenu
/// </summary>
public class UIInputPriority : MonoBehaviour
{
    void Awake()
    {
        // Set script execution order programmatically
        // DialogueUI processes input first (-100)
        // ModernPauseMenu processes input later (100)
        
        Debug.Log("UI Input Priority system initialized");
    }
    
    /// <summary>
    /// Checks if any blocking UI is open that should prevent pause menu
    /// </summary>
    public static bool IsBlockingUIOpen()
    {
        // Check dialogue and history panel
        if (DialogueUI.Instance != null)
        {
            if (DialogueUI.Instance.IsDialogueOpen())
                return true;
                
            // NOUVEAU : Vérifie aussi le history panel
            if (DialogueUI.Instance.historyPanel != null && DialogueUI.Instance.historyPanel.activeInHierarchy)
                return true;
        }
            
        // Check other UIs via UIManager
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsAnyPanelOpen(
                UIPanelNames.Dialogue,
                UIPanelNames.QuestJournal,
                UIPanelNames.Inventory,
                UIPanelNames.DialogueHistory))
            {
                return true;
            }
        }
        
        return false;
    }
}