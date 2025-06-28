using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simplified UI for save/load game functionality
/// This version manages slots directly without complex separation
/// </summary>
public class SimpleSaveGameUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject saveMenuPanel;
    [SerializeField] private Transform saveSlotContainer;
    [SerializeField] private Button closeButton;
    
    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmDialog;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    
    [Header("Notifications")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 2f;
    
    [Header("Settings")]
    [SerializeField] private bool showAllSlots = false;
    [SerializeField] private int maxSlots = 10;
    
    private System.Action pendingAction;
    
    void Start()
    {
        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSaveMenu);
        
        // Setup confirmation dialog
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(ConfirmAction);
            
        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(CancelAction);
        
        // Hide panels
        if (saveMenuPanel != null)
            saveMenuPanel.SetActive(false);
            
        if (confirmDialog != null)
            confirmDialog.SetActive(false);
            
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
        
        // Subscribe to events
        SaveGameManager.OnGameSaved += OnGameSaved;
        SaveGameManager.OnGameLoaded += OnGameLoaded;
        
        // Initial setup of slots
        SetupSaveSlots();
    }
    
    void OnDestroy()
    {
        SaveGameManager.OnGameSaved -= OnGameSaved;
        SaveGameManager.OnGameLoaded -= OnGameLoaded;
    }
    
    void SetupSaveSlots()
    {
        if (saveSlotContainer == null) return;
        
        // Process each existing slot in the container
        for (int i = 0; i < saveSlotContainer.childCount && i < maxSlots; i++)
        {
            Transform slotTransform = saveSlotContainer.GetChild(i);
            SetupSlot(slotTransform.gameObject, i);
        }
        
        RefreshAllSlots();
    }
    
    void SetupSlot(GameObject slotObj, int index)
    {
        // Find UI elements
        TextMeshProUGUI slotText = null;
        Button saveBtn = null;
        Button loadBtn = null;
        Button deleteBtn = null;
        
        Debug.Log($"[SimpleSaveGameUI] Setting up slot {index}: {slotObj.name}");
        
        // Find text components
        TextMeshProUGUI[] texts = slotObj.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var text in texts)
        {
            // Skip if it's inside a button
            if (text.transform.parent.GetComponent<Button>() == null)
            {
                slotText = text;
                break;
            }
        }
        
        // Find buttons
        Button[] buttons = slotObj.GetComponentsInChildren<Button>();
        Debug.Log($"  Found {buttons.Length} buttons in slot {index}");
        
        foreach (var button in buttons)
        {
            string btnName = button.name.ToLower();
            
            // Also check button text
            TextMeshProUGUI btnText = button.GetComponentInChildren<TextMeshProUGUI>();
            string btnContent = btnText != null ? btnText.text.ToLower() : "";
            
            Debug.Log($"  Button: {button.name}, Text content: '{btnContent}'");
            
            // More flexible detection
            if ((btnName.Contains("save") || btnContent.Contains("save")) && saveBtn == null)
            {
                saveBtn = button;
                Debug.Log($"    → Identified as SAVE button");
            }
            else if ((btnName.Contains("load") || btnContent.Contains("load") || 
                     btnName.Contains("charger") || btnContent.Contains("charger")) && loadBtn == null)
            {
                loadBtn = button;
                Debug.Log($"    → Identified as LOAD button");
            }
            else if ((btnName.Contains("delete") || btnContent.Contains("delete") || 
                     btnName.Contains("del") || btnContent.Contains("suppr") ||
                     btnName.Contains("remove") || btnContent.Contains("effacer")) && deleteBtn == null)
            {
                deleteBtn = button;
                Debug.Log($"    → Identified as DELETE button");
            }
        }
        
        // Setup button listeners
        if (saveBtn != null)
        {
            saveBtn.onClick.RemoveAllListeners();
            int slotIndex = index; // Capture for closure
            saveBtn.onClick.AddListener(() => SaveToSlot(slotIndex));
        }
        
        if (loadBtn != null)
        {
            loadBtn.onClick.RemoveAllListeners();
            int slotIndex = index; // Capture for closure
            loadBtn.onClick.AddListener(() => LoadFromSlot(slotIndex));
        }
        
        if (deleteBtn != null)
        {
            deleteBtn.onClick.RemoveAllListeners();
            int slotIndex = index; // Capture for closure
            deleteBtn.onClick.AddListener(() => DeleteSlot(slotIndex));
        }
        
        // Store references for later updates
        slotObj.name = $"SaveSlot_{index}";
        
        // Debug summary
        Debug.Log($"  Slot {index} setup complete:");
        Debug.Log($"    - Save button: {(saveBtn != null ? "Found" : "NOT FOUND")}");
        Debug.Log($"    - Load button: {(loadBtn != null ? "Found" : "NOT FOUND")}");
        Debug.Log($"    - Delete button: {(deleteBtn != null ? "Found" : "NOT FOUND")}");
    }
    
    public void OpenSaveMenu()
    {
        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(true);
            RefreshAllSlots();
            Time.timeScale = 0f;
        }
    }
    
    public void CloseSaveMenu()
    {
        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(false);
            
            // Check if we came from pause menu
            ModernPauseMenu pauseMenu = FindObjectOfType<ModernPauseMenu>();
            if (pauseMenu != null && pauseMenu.IsPaused())
            {
                // Return to pause menu (not game)
                Transform pausePanel = pauseMenu.transform.Find("PauseMenuPanel");
                if (pausePanel != null)
                {
                    pausePanel.gameObject.SetActive(true);
                }
                Time.timeScale = 0f;
            }
            else
            {
                // Resume game
                Time.timeScale = 1f;
            }
        }
    }
    
    void RefreshAllSlots()
    {
        if (SaveGameManager.Instance == null || saveSlotContainer == null) return;
        
        string[] saves = SaveGameManager.Instance.GetAllSaves();
        int highestUsedSlot = -1;
        
        // Find highest used slot
        for (int i = 0; i < maxSlots; i++)
        {
            if (System.Array.Exists(saves, s => s == $"save_{i}"))
            {
                highestUsedSlot = i;
            }
        }
        
        // Update each slot
        for (int i = 0; i < saveSlotContainer.childCount && i < maxSlots; i++)
        {
            Transform slotTransform = saveSlotContainer.GetChild(i);
            GameObject slotObj = slotTransform.gameObject;
            
            bool hasData = System.Array.Exists(saves, s => s == $"save_{i}");
            
            // Update slot display
            UpdateSlotDisplay(slotObj, i, hasData);
            
            // Show/hide slot based on progressive system
            if (!showAllSlots)
            {
                bool shouldShow = i <= highestUsedSlot + 1;
                slotObj.SetActive(shouldShow);
            }
            else
            {
                slotObj.SetActive(true);
            }
        }
    }
    
    void UpdateSlotDisplay(GameObject slotObj, int index, bool hasData)
    {
        // Find and update text
        TextMeshProUGUI[] texts = slotObj.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var text in texts)
        {
            if (text.transform.parent.GetComponent<Button>() == null)
            {
                text.text = hasData ? $"Claudius-{index + 1}" : "Empty";
                break;
            }
        }
        
        // Update button visibility
        Button[] buttons = slotObj.GetComponentsInChildren<Button>(true); // Include inactive
        int loadCount = 0, deleteCount = 0;
        
        foreach (var button in buttons)
        {
            string btnName = button.name.ToLower();
            TextMeshProUGUI btnText = button.GetComponentInChildren<TextMeshProUGUI>();
            string btnContent = btnText != null ? btnText.text.ToLower() : "";
            
            // Check for load button
            if (btnName.Contains("load") || btnContent.Contains("load") || 
                btnName.Contains("charger") || btnContent.Contains("charger"))
            {
                button.gameObject.SetActive(hasData);
                loadCount++;
                Debug.Log($"  Slot {index}: Set LOAD button '{button.name}' to {(hasData ? "VISIBLE" : "HIDDEN")}");
            }
            // Check for delete button
            else if (btnName.Contains("delete") || btnContent.Contains("delete") || 
                     btnName.Contains("del") || btnContent.Contains("suppr") ||
                     btnName.Contains("remove") || btnContent.Contains("effacer"))
            {
                button.gameObject.SetActive(hasData);
                deleteCount++;
                Debug.Log($"  Slot {index}: Set DELETE button '{button.name}' to {(hasData ? "VISIBLE" : "HIDDEN")}");
            }
        }
        
        if (loadCount == 0) Debug.LogWarning($"  Slot {index}: No LOAD button found!");
        if (deleteCount == 0) Debug.LogWarning($"  Slot {index}: No DELETE button found!");
    }
    
    public void SaveToSlot(int slotIndex)
    {
        string saveName = $"save_{slotIndex}";
        string displayName = $"Claudius-{slotIndex + 1}";
        
        if (SaveGameManager.Instance.SaveExists(saveName))
        {
            ShowConfirmDialog($"Overwrite {displayName}?", () =>
            {
                PerformSave(saveName);
            });
        }
        else
        {
            PerformSave(saveName);
        }
    }
    
    public void LoadFromSlot(int slotIndex)
    {
        string saveName = $"save_{slotIndex}";
        string displayName = $"Claudius-{slotIndex + 1}";
        
        ShowConfirmDialog($"Load {displayName}? Current progress will be lost.", () =>
        {
            PerformLoad(saveName);
        });
    }
    
    public void DeleteSlot(int slotIndex)
    {
        string saveName = $"save_{slotIndex}";
        string displayName = $"Claudius-{slotIndex + 1}";
        
        ShowConfirmDialog($"Delete {displayName}?", () =>
        {
            SaveGameManager.Instance.DeleteSave(saveName);
            RefreshAllSlots();
            ShowNotification("Save deleted");
        });
    }
    
    void PerformSave(string saveName)
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame(saveName);
            ShowNotification("Game saved!");
            
            // Small delay to show notification before closing
            Invoke(nameof(CloseAndReturnToGame), 0.5f);
        }
    }
    
    void PerformLoad(string saveName)
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.LoadGame(saveName);
            ShowNotification("Game loaded!");
            
            // Small delay to show notification before closing
            Invoke(nameof(CloseAndReturnToGame), 0.5f);
        }
    }
    
    void CloseAndReturnToGame()
    {
        // Close save menu
        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(false);
        }
        
        // Close pause menu completely and resume game
        ModernPauseMenu pauseMenu = FindObjectOfType<ModernPauseMenu>();
        if (pauseMenu != null)
        {
            // Force complete resume
            pauseMenu.Resume();
        }
        else
        {
            // Just resume time if no pause menu
            Time.timeScale = 1f;
        }
        
        // Ensure we're really back in game
        Time.timeScale = 1f;
        
        Debug.Log("[SaveSystem] Returned to game after save/load");
    }
    
    void ShowConfirmDialog(string message, System.Action onConfirm)
    {
        if (confirmDialog != null && confirmText != null)
        {
            confirmText.text = message;
            confirmDialog.SetActive(true);
            
            // Ensure dialog is on top by moving it to the end of the hierarchy
            confirmDialog.transform.SetAsLastSibling();
            
            // Alternative: If dialog is outside SaveMenuPanel, bring SaveMenuPanel to back
            if (confirmDialog.transform.parent != saveMenuPanel.transform)
            {
                saveMenuPanel.transform.SetAsFirstSibling();
            }
            
            pendingAction = onConfirm;
        }
    }
    
    void ConfirmAction()
    {
        if (confirmDialog != null)
            confirmDialog.SetActive(false);
            
        pendingAction?.Invoke();
        pendingAction = null;
    }
    
    void CancelAction()
    {
        if (confirmDialog != null)
            confirmDialog.SetActive(false);
            
        pendingAction = null;
    }
    
    void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            
            CancelInvoke(nameof(HideNotification));
            Invoke(nameof(HideNotification), notificationDuration);
        }
    }
    
    void HideNotification()
    {
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    void OnGameSaved()
    {
        ShowNotification("Game saved!");
        RefreshAllSlots();
    }
    
    void OnGameLoaded()
    {
        ShowNotification("Game loaded!");
        RefreshAllSlots();
    }
    
    [ContextMenu("Force Refresh Slots")]
    public void ForceRefresh()
    {
        SetupSaveSlots();
    }
    
    [ContextMenu("Diagnose Slot Structure")]
    public void DiagnoseSlotStructure()
    {
        if (saveSlotContainer == null)
        {
            Debug.LogError("[DIAGNOSTIC] SaveSlotContainer is not assigned!");
            return;
        }
        
        Debug.Log("\n=== SAVE SLOT STRUCTURE DIAGNOSTIC ===");
        Debug.Log($"Total slots found: {saveSlotContainer.childCount}");
        
        for (int i = 0; i < saveSlotContainer.childCount; i++)
        {
            Transform slot = saveSlotContainer.GetChild(i);
            Debug.Log($"\n--- SLOT {i}: {slot.name} ---");
            
            // List all children
            Debug.Log("  Children:");
            for (int j = 0; j < slot.childCount; j++)
            {
                Transform child = slot.GetChild(j);
                string type = "";
                if (child.GetComponent<Button>()) type = "[BUTTON]";
                if (child.GetComponent<TextMeshProUGUI>()) type = "[TEXT]";
                Debug.Log($"    - {child.name} {type}");
                
                // If it's a button, show its text
                if (child.GetComponent<Button>())
                {
                    TextMeshProUGUI btnText = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText) Debug.Log($"      Button text: '{btnText.text}'");
                }
            }
            
            // Check for buttons including inactive
            Button[] allButtons = slot.GetComponentsInChildren<Button>(true);
            Debug.Log($"  Total buttons (including inactive): {allButtons.Length}");
            foreach (var btn in allButtons)
            {
                Debug.Log($"    - {btn.name} (Active: {btn.gameObject.activeSelf})");
            }
        }
        
        Debug.Log("\n=== END DIAGNOSTIC ===\n");
    }
}
