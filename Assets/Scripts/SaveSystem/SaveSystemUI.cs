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
    
    // Index spécial pour le slot quicksave (les slots normaux commencent à 1).
    private const int QUICKSAVE_SLOT_INDEX = 0;
    private const string QUICKSAVE_NAME = "quicksave";

    // Private
    private Dictionary<int, SlotReferences> slotRefs = new Dictionary<int, SlotReferences>();

    static string GetSaveNameForSlot(int slotIndex) =>
        slotIndex == QUICKSAVE_SLOT_INDEX ? QUICKSAVE_NAME : $"save_{slotIndex}";

    static string GetDisplayNameForSlot(int slotIndex) =>
        slotIndex == QUICKSAVE_SLOT_INDEX ? "Sauvegarde rapide" : $"Save {slotIndex}";
    
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
        // Check if we're in MainMenu scene - if so, don't hide the panel
        bool isMainMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
        
        // Hide the save menu at start (except in MainMenu)
        if (saveMenuPanel != null && !isMainMenuScene)
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

        int childCount = saveSlotContainer.childCount;

        // Setup each slot existant (save_1, save_2, ...).
        for (int i = 0; i < childCount; i++)
        {
            Transform slotTransform = saveSlotContainer.GetChild(i);
            SetupSlot(slotTransform.gameObject, i + 1);
        }

        // Slot quicksave : clone du premier slot, déplacé en tête de liste.
        if (childCount > 0)
        {
            GameObject template = saveSlotContainer.GetChild(0).gameObject;
            GameObject quickSlot = Instantiate(template, saveSlotContainer);
            quickSlot.name = "SaveSlot_Quick";
            quickSlot.transform.SetSiblingIndex(0);
            SetupSlot(quickSlot, QUICKSAVE_SLOT_INDEX);
        }

        // Initial refresh
        RefreshAllSlots();
        
        // Log if we're in MainMenu
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
        {
            // Silent in MainMenu
        }
    }
    
    void SetupSlot(GameObject slot, int index)
    {
        SlotReferences refs = new SlotReferences();
        refs.gameObject = slot;
        
        // Check if we're in MainMenu scene
        bool isMainMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
        
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
                
                // Hide save button in MainMenu
                if (isMainMenuScene)
                {
                    button.gameObject.SetActive(false);
                }
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
                
                // Hide delete button in MainMenu
                if (isMainMenuScene)
                {
                    button.gameObject.SetActive(false);
                }
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
        
        // Check if we're in MainMenu scene
        bool isMainMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
        
        foreach (var kvp in slotRefs)
        {
            int index = kvp.Key;
            SlotReferences refs = kvp.Value;
            
            string saveName = GetSaveNameForSlot(index);
            string displayName = GetDisplayNameForSlot(index);
            bool hasSave = SaveGameManager.Instance.SaveExists(saveName);

            // Update text
            if (refs.infoText != null)
            {
                refs.infoText.text = hasSave ? displayName : $"Empty — {displayName}";
            }
            
            // Update button visibility
            if (refs.loadButton != null)
                refs.loadButton.gameObject.SetActive(hasSave);
                
            if (refs.deleteButton != null)
                refs.deleteButton.gameObject.SetActive(hasSave && !isMainMenuScene); // Hide delete in MainMenu
                
            if (refs.saveButton != null)
                refs.saveButton.gameObject.SetActive(!isMainMenuScene); // Always hide save in MainMenu
        }
    }
    
    // Button callbacks
    void OnSaveClicked(int slotIndex)
    {
        string saveName = GetSaveNameForSlot(slotIndex);
        string displayName = GetDisplayNameForSlot(slotIndex);

        if (SaveGameManager.Instance.SaveExists(saveName))
        {
            // Use ConfirmationDialogManager
            if (ConfirmationDialogManager.Instance != null)
            {
                ConfirmationDialogManager.Instance.ShowOverwriteDialog(
                    displayName,
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
        string saveName = GetSaveNameForSlot(slotIndex);
        string displayName = GetDisplayNameForSlot(slotIndex);
        Debug.Log($"[SaveSystem] Load clicked for slot {slotIndex}, save name: {saveName}");
        
        // Check if we're in MainMenu scene
        bool isMainMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
        
        if (isMainMenuScene)
        {
            // In MainMenu, load directly without confirmation
            if (debugMode)
                Debug.Log($"[SaveSystem] Loading save from MainMenu: {saveName}");
            
            // DON'T hide the panel in MainMenu - LoadingScreen will cover it
            // The panel will be automatically hidden when scene changes
            
            MainMenuManager mainMenu = FindFirstObjectByType<MainMenuManager>();
            if (mainMenu != null)
            {
                // Store save to load after scene transition
                PlayerPrefs.SetString("LoadOnStart", saveName);
                PlayerPrefs.Save();
                
                // Start loading the game scene
                mainMenu.StartCoroutine(mainMenu.LoadGameScene(false, saveName));
            }
            else
            {
                Debug.LogError("[SaveSystem] MainMenuManager not found!");
            }
        }
        else
        {
            // In Game scene, use confirmation dialog
            string saveToLoad = saveName;
            
            if (ConfirmationDialogManager.Instance != null)
            {
                Debug.Log("[SaveSystem] ConfirmationDialogManager found, showing load dialog");
                ConfirmationDialogManager.Instance.ShowDialog(
                    $"Charger {displayName} ?\nLa progression actuelle sera perdue.",
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
    }
    
    void OnDeleteClicked(int slotIndex)
    {
        string saveName = GetSaveNameForSlot(slotIndex);
        string displayName = GetDisplayNameForSlot(slotIndex);
        Debug.Log($"[SaveSystem] Delete clicked for slot {slotIndex}, save name: {saveName}");

        // Capture the correct save name in a local variable
        string saveToDelete = saveName;

        // Use ConfirmationDialogManager
        if (ConfirmationDialogManager.Instance != null)
        {
            Debug.Log("[SaveSystem] ConfirmationDialogManager found, showing delete dialog");
            ConfirmationDialogManager.Instance.ShowDeleteDialog(
                displayName,
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
        Debug.Log("[SaveSystem] Close button clicked");
        
        // Check if we're in MainMenu scene
        bool isMainMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
        
        if (isMainMenuScene)
        {
            // In MainMenu, just hide this panel and show main menu
            if (saveMenuPanel != null)
                saveMenuPanel.SetActive(false);
                
            // Find and show main menu panel
            MainMenuManager mainMenu = FindFirstObjectByType<MainMenuManager>();
            if (mainMenu != null)
            {
                mainMenu.ShowMainMenu();
            }
            else
            {
                // Fallback - try to find MainMenuPanel directly
                GameObject mainMenuPanel = GameObject.Find("MainMenuPanel");
                if (mainMenuPanel != null)
                    mainMenuPanel.SetActive(true);
            }
        }
        else
        {
            // In Game scene, use UnifiedUIManager
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
                
            PauseMenuUI pauseMenu = FindFirstObjectByType<PauseMenuUI>();
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
