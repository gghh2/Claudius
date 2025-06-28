using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Clean save system UI that works with the existing UI navigation
/// </summary>
public class SaveSystemUI : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private GameObject saveMenuPanel;
    [SerializeField] private Transform saveSlotContainer;
    // Remove confirmDialog reference - we'll use ConfirmationDialogManager
    
    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    
    // Remove Confirm Dialog section - not needed anymore
    
    [Header("Settings")]
    [SerializeField] private bool debugMode = false;
    
    // Private
    private Dictionary<int, SlotReferences> slotRefs = new Dictionary<int, SlotReferences>();
    
    private class SlotReferences
    {
        public GameObject gameObject;
        public TextMeshProUGUI infoText;
        public Button saveButton;
        public Button loadButton;
        public Button deleteButton;
    }
    
    void Start()
    {
        // Hide the save menu at start
        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(false);
        }
        
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }
        
        // Remove confirm dialog setup - using ConfirmationDialogManager instead
        
        // Initialize slots
        InitializeSlots();
        
        // Subscribe to save events
        SaveGameManager.OnGameSaved += OnGameSaved;
        SaveGameManager.OnGameLoaded += OnGameLoaded;
    }
    
    void OnDestroy()
    {
        SaveGameManager.OnGameSaved -= OnGameSaved;
        SaveGameManager.OnGameLoaded -= OnGameLoaded;
    }
    
    void InitializeSlots()
    {
        if (saveSlotContainer == null) return;
        
        // Clear previous references
        slotRefs.Clear();
        
        // Setup each slot
        for (int i = 0; i < saveSlotContainer.childCount; i++)
        {
            Transform slotTransform = saveSlotContainer.GetChild(i);
            // Use i+1 for slot index to match display (Slot 1 = save_1)
            SetupSlot(slotTransform.gameObject, i + 1);
        }
        
        // Initial refresh
        RefreshAllSlots();
    }
    
    void SetupSlot(GameObject slot, int index)
    {
        SlotReferences refs = new SlotReferences();
        refs.gameObject = slot;
        
        // Find all buttons in the slot
        Button[] buttons = slot.GetComponentsInChildren<Button>();
        foreach (var button in buttons)
        {
            string btnName = button.name.ToLower();
            
            if (btnName.Contains("save"))
            {
                refs.saveButton = button;
                int capturedIndex = index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnSaveClicked(capturedIndex));
            }
            else if (btnName.Contains("load"))
            {
                refs.loadButton = button;
                int capturedIndex = index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnLoadClicked(capturedIndex));
            }
            else if (btnName.Contains("delete"))
            {
                refs.deleteButton = button;
                int capturedIndex = index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnDeleteClicked(capturedIndex));
            }
        }
        
        // Find info text (should be "SlotInfo" based on your hierarchy)
        Transform slotInfo = slot.transform.Find("SlotInfo");
        if (slotInfo != null)
        {
            refs.infoText = slotInfo.GetComponent<TextMeshProUGUI>();
        }
        
        // Store references
        slotRefs[index] = refs;
        
        if (debugMode)
        {
            Debug.Log($"[SaveSystem] Slot {index} initialized - Save: {refs.saveButton != null}, Load: {refs.loadButton != null}, Delete: {refs.deleteButton != null}");
        }
    }
    
    void RefreshAllSlots()
    {
        if (SaveGameManager.Instance == null) return;
        
        foreach (var kvp in slotRefs)
        {
            int index = kvp.Key;
            SlotReferences refs = kvp.Value;
            
            string saveName = $"save_{index}";
            bool hasSave = SaveGameManager.Instance.SaveExists(saveName);
            
            // Update text - index already starts at 1
            if (refs.infoText != null)
            {
                refs.infoText.text = hasSave ? $"Save {index}" : $"Empty Slot {index}";
            }
            
            // Update button visibility
            if (refs.loadButton != null)
                refs.loadButton.gameObject.SetActive(hasSave);
                
            if (refs.deleteButton != null)
                refs.deleteButton.gameObject.SetActive(hasSave);
        }
    }
    
    // Button callbacks
    void OnSaveClicked(int slotIndex)
    {
        string saveName = $"save_{slotIndex}";
        
        if (SaveGameManager.Instance.SaveExists(saveName))
        {
            // Use ConfirmationDialogManager
            if (ConfirmationDialogManager.Instance != null)
            {
                ConfirmationDialogManager.Instance.ShowOverwriteDialog(
                    $"Save {slotIndex}",
                    () => PerformSave(saveName)
                );
            }
            else
            {
                // Fallback if manager not found
                PerformSave(saveName);
            }
        }
        else
        {
            PerformSave(saveName);
        }
    }
    
    void OnLoadClicked(int slotIndex)
    {
        string saveName = $"save_{slotIndex}";
        Debug.Log($"[SaveSystem] Load clicked for slot {slotIndex}, save name: {saveName}");
        
        // Capture the correct save name in a local variable
        string saveToLoad = saveName;
        
        // Use ConfirmationDialogManager
        if (ConfirmationDialogManager.Instance != null)
        {
            Debug.Log("[SaveSystem] ConfirmationDialogManager found, showing load dialog");
            ConfirmationDialogManager.Instance.ShowDialog(
                $"Load Save {slotIndex}?\nCurrent progress will be lost.",
                () => {
                    Debug.Log($"[SaveSystem] Load confirmed for {saveToLoad}");
                    PerformLoad(saveToLoad);
                },
                null,
                "Load Game",
                "Load",
                "Cancel"
            );
        }
        else
        {
            Debug.LogError("[SaveSystem] ConfirmationDialogManager.Instance is NULL! Loading directly.");
            // Fallback
            PerformLoad(saveName);
        }
    }
    
    void OnDeleteClicked(int slotIndex)
    {
        string saveName = $"save_{slotIndex}";
        Debug.Log($"[SaveSystem] Delete clicked for slot {slotIndex}, save name: {saveName}");
        
        // Capture the correct save name in a local variable
        string saveToDelete = saveName;
        
        // Use ConfirmationDialogManager
        if (ConfirmationDialogManager.Instance != null)
        {
            Debug.Log("[SaveSystem] ConfirmationDialogManager found, showing delete dialog");
            ConfirmationDialogManager.Instance.ShowDeleteDialog(
                $"Save {slotIndex}",
                () => {
                    Debug.Log($"[SaveSystem] Delete confirmed for {saveToDelete}");
                    SaveGameManager.Instance.DeleteSave(saveToDelete);
                    RefreshAllSlots();
                    
                    // Show notification
                    if (NotificationManager.Instance != null)
                    {
                        NotificationManager.Instance.ShowInfo("Save deleted!");
                    }
                }
            );
        }
        else
        {
            Debug.LogError("[SaveSystem] ConfirmationDialogManager.Instance is NULL!");
        }
    }
    
    void OnCloseClicked()
    {
        Debug.Log("[SaveSystem] Close button clicked, using UnifiedUIManager.NavigateBack()");
        
        // Use UnifiedUIManager for proper navigation
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateBack();
        }
        else
        {
            // Fallback if UnifiedUIManager not available
            if (saveMenuPanel != null)
                saveMenuPanel.SetActive(false);
                
            GameObject pausePanel = GameObject.Find("PauseMenuPanel");
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }
    }
    
    // Save/Load operations
    void PerformSave(string saveName)
    {
        SaveGameManager.Instance.SaveGame(saveName);
        
        // Try notification
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowSuccess("Game saved!");
        }
        
        // Small delay to see notification before closing
        StartCoroutine(CloseAfterDelay(0.5f));
    }
    
    void PerformLoad(string saveName)
    {
        SaveGameManager.Instance.LoadGame(saveName);
        
        // Try notification
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowSuccess("Game loaded!");
        }
        
        // Small delay to see notification before closing
        StartCoroutine(CloseAfterDelay(0.5f));
    }
    
    IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        CloseAllMenus();
        Time.timeScale = 1f;
    }
    
    void CloseAllMenus()
    {
        Debug.Log("[SaveSystem] Closing all menus and returning to game");
        
        // Use UnifiedUIManager to properly close everything
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.CloseAllPanels();
        }
        else
        {
            // Fallback
            if (saveMenuPanel != null)
                saveMenuPanel.SetActive(false);
                
            GameObject pausePanel = GameObject.Find("PauseMenuPanel");
            if (pausePanel != null)
                pausePanel.SetActive(false);
                
            ModernPauseMenu pauseMenu = FindObjectOfType<ModernPauseMenu>();
            if (pauseMenu != null)
                pauseMenu.Resume();
        }
    }
    
    // Events
    void OnGameSaved()
    {
        RefreshAllSlots();
        if (debugMode)
            Debug.Log("[SaveSystem] Game saved!");
    }
    
    void OnGameLoaded()
    {
        if (debugMode)
            Debug.Log("[SaveSystem] Game loaded!");
    }
    
    // Public method for external access
    public void OpenSaveMenu()
    {
        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(true);
            RefreshAllSlots();
        }
    }
}
