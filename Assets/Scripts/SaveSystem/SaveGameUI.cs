using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI for save/load game functionality
/// </summary>
public class SaveGameUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject saveMenuPanel;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button quickSaveButton;
    [SerializeField] private Button quickLoadButton;
    
    [Header("Save Slots")]
    [SerializeField] private Transform saveSlotContainer;
    [SerializeField] private GameObject saveSlotPrefab;
    [SerializeField] private int maxSaveSlots = 10;
    
    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmDialog;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    
    [Header("Notifications")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 2f;
    
    private List<SaveSlotUI> saveSlots = new List<SaveSlotUI>();
    private System.Action pendingAction;
    
    void Start()
    {
        // Setup buttons
        if (saveButton != null)
            saveButton.onClick.AddListener(OpenSaveMenu);
            
        if (loadButton != null)
            loadButton.onClick.AddListener(OpenLoadMenu);
            
        if (quickSaveButton != null)
            quickSaveButton.onClick.AddListener(QuickSave);
            
        if (quickLoadButton != null)
            quickLoadButton.onClick.AddListener(QuickLoad);
        
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
        
        // Create save slots
        CreateSaveSlots();
        
        // Subscribe to events
        SaveGameManager.OnGameSaved += OnGameSaved;
        SaveGameManager.OnGameLoaded += OnGameLoaded;
    }
    
    void OnDestroy()
    {
        SaveGameManager.OnGameSaved -= OnGameSaved;
        SaveGameManager.OnGameLoaded -= OnGameLoaded;
    }
    
    void CreateSaveSlots()
    {
        if (saveSlotPrefab == null || saveSlotContainer == null) return;
        
        for (int i = 0; i < maxSaveSlots; i++)
        {
            GameObject slotObj = Instantiate(saveSlotPrefab, saveSlotContainer);
            SaveSlotUI slot = slotObj.GetComponent<SaveSlotUI>();
            
            if (slot == null)
            {
                slot = slotObj.AddComponent<SaveSlotUI>();
            }
            
            slot.Initialize(i, this);
            saveSlots.Add(slot);
        }
    }
    
    public void OpenSaveMenu()
    {
        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(true);
            RefreshSaveSlots();
            
            // Pause game
            Time.timeScale = 0f;
        }
    }
    
    public void OpenLoadMenu()
    {
        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(true);
            RefreshSaveSlots();
            
            // Pause game
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
                // Return to pause menu instead of resuming
                Transform pausePanel = pauseMenu.transform.Find("PauseMenuPanel");
                if (pausePanel != null)
                {
                    pausePanel.gameObject.SetActive(true);
                }
                // Keep time paused
                Time.timeScale = 0f;
            }
            else
            {
                // Normal resume
                Time.timeScale = 1f;
            }
        }
    }
    
    void RefreshSaveSlots()
    {
        if (SaveGameManager.Instance == null) return;
        
        string[] saves = SaveGameManager.Instance.GetAllSaves();
        
        foreach (var slot in saveSlots)
        {
            slot.UpdateSlot(saves);
        }
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
            RefreshSaveSlots();
            ShowNotification("Save deleted");
        });
    }
    
    void QuickSave()
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame("quicksave");
        }
    }
    
    void QuickLoad()
    {
        if (SaveGameManager.Instance != null && SaveGameManager.Instance.SaveExists("quicksave"))
        {
            ShowConfirmDialog("Load quicksave? Current progress will be lost.", () =>
            {
                PerformLoad("quicksave");
            });
        }
        else
        {
            ShowNotification("No quicksave found");
        }
    }
    
    void PerformSave(string saveName)
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame(saveName);
            RefreshSaveSlots();
            CloseSaveMenu();
        }
    }
    
    void PerformLoad(string saveName)
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.LoadGame(saveName);
            CloseSaveMenu();
        }
    }
    
    void ShowConfirmDialog(string message, System.Action onConfirm)
    {
        if (confirmDialog != null && confirmText != null)
        {
            confirmText.text = message;
            confirmDialog.SetActive(true);
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
        RefreshSaveSlots(); // Refresh to update button visibility
    }
    
    void OnGameLoaded()
    {
        ShowNotification("Game loaded!");
        RefreshSaveSlots(); // Refresh to update button visibility
    }
}

/// <summary>
/// Individual save slot UI component
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI saveInfoText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private GameObject emptySlotIndicator;
    
    private int slotIndex;
    private SaveGameUI saveGameUI;
    
    public void Initialize(int index, SaveGameUI ui)
    {
        slotIndex = index;
        saveGameUI = ui;
        
        if (slotNumberText != null)
            slotNumberText.text = $"Slot {index + 1}";
        
        if (saveButton != null)
            saveButton.onClick.AddListener(() => saveGameUI.SaveToSlot(slotIndex));
            
        if (loadButton != null)
            loadButton.onClick.AddListener(() => saveGameUI.LoadFromSlot(slotIndex));
            
        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => saveGameUI.DeleteSlot(slotIndex));
    }
    
    public void UpdateSlot(string[] allSaves)
    {
        string saveName = $"save_{slotIndex}";
        bool hasData = System.Array.Exists(allSaves, s => s == saveName);
        
        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(!hasData);
        
        // Hide/show load button based on save data
        if (loadButton != null)
            loadButton.gameObject.SetActive(hasData);
            
        // Hide/show delete button based on save data
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(hasData);
        
        // Update slot text
        if (saveInfoText != null)
        {
            if (hasData)
            {
                saveInfoText.text = $"Claudius-{slotIndex + 1}";
            }
            else
            {
                saveInfoText.text = "Empty";
            }
        }
        
        // Also update the slot number text if it exists
        if (slotNumberText != null)
        {
            slotNumberText.text = $"Slot {slotIndex + 1}";
        }
    }
}