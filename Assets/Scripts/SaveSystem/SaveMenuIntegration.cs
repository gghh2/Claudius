using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple integration between pause menu and save system
/// Add this to the same GameObject as ModernPauseMenu
/// </summary>
[RequireComponent(typeof(ModernPauseMenu))]
public class SaveMenuIntegration : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button saveLoadButton;
    [SerializeField] private SaveSystemUI saveSystemUI;
    
    // Quick save/load removed - no longer needed
    // [Header("Quick Save/Load")]
    // [SerializeField] private bool enableQuickSave = true;
    // [SerializeField] private KeyCode quickSaveKey = KeyCode.F5;
    // [SerializeField] private KeyCode quickLoadKey = KeyCode.F9;
    
    private ModernPauseMenu pauseMenu;
    
    void Start()
    {
        pauseMenu = GetComponent<ModernPauseMenu>();
        
        // Find SaveSystemUI if not assigned
        if (saveSystemUI == null)
        {
            saveSystemUI = FindObjectOfType<SaveSystemUI>();
        }
        
        // Setup button if assigned
        if (saveLoadButton != null && saveSystemUI != null)
        {
            saveLoadButton.onClick.AddListener(OpenSaveMenu);
        }
        else
        {
            Debug.LogWarning("[SaveMenuIntegration] SaveLoadButton or SaveSystemUI not assigned!");
        }
    }
    
    // Quick save/load removed
    /*
    void Update()
    {
        if (!enableQuickSave) return;
        
        // Quick save
        if (Input.GetKeyDown(quickSaveKey))
        {
            QuickSave();
        }
        
        // Quick load
        if (Input.GetKeyDown(quickLoadKey))
        {
            QuickLoad();
        }
    }
    */
    
    void OpenSaveMenu()
    {
        if (saveSystemUI == null) return;
        
        // Use UnifiedUIManager for navigation
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.SaveMenu);
        }
        else
        {
            // Fallback to old method
            Transform pausePanel = transform.Find("PauseMenuPanel");
            if (pausePanel != null)
            {
                pausePanel.gameObject.SetActive(false);
            }
            
            saveSystemUI.OpenSaveMenu();
        }
        
        // Ensure we return to pause menu when closing
        EnsureCloseButtonReturns();
    }
    
    void EnsureCloseButtonReturns()
    {
        // Find the close button in SaveMenuPanel
        Transform savePanel = saveSystemUI.transform.Find("SaveMenuPanel");
        if (savePanel != null)
        {
            Button closeButton = savePanel.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton != null)
            {
                // Clear and set new listener
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => {
                    // Use UnifiedUIManager for navigation back
                    if (UnifiedUIManager.Instance != null)
                    {
                        UnifiedUIManager.Instance.NavigateBack();
                    }
                    else
                    {
                        // Fallback to old method
                        savePanel.gameObject.SetActive(false);
                        
                        Transform pausePanel = transform.Find("PauseMenuPanel");
                        if (pausePanel != null)
                        {
                            pausePanel.gameObject.SetActive(true);
                        }
                        
                        // Keep game paused
                        Time.timeScale = 0f;
                    }
                });
            }
        }
    }
    
    // Quick save/load methods removed
    /*
    void QuickSave()
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame("quicksave");
            ShowNotification("Quick Save Complete!");
        }
    }
    
    void QuickLoad()
    {
        if (SaveGameManager.Instance != null && SaveGameManager.Instance.SaveExists("quicksave"))
        {
            SaveGameManager.Instance.LoadGame("quicksave");
            ShowNotification("Quick Load Complete!");
        }
        else
        {
            ShowNotification("No Quick Save Found!");
        }
    }
    
    void ShowNotification(string message)
    {
        // Try to use SaveGameUI's notification system
        if (saveGameUI != null)
        {
            var method = saveGameUI.GetType().GetMethod("ShowNotification", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(saveGameUI, new object[] { message });
                return;
            }
        }
        
        // Fallback to console
        Debug.Log($"[Save System] {message}");
    }
    */
    
    void OnDestroy()
    {
        if (saveLoadButton != null)
        {
            saveLoadButton.onClick.RemoveListener(OpenSaveMenu);
        }
    }
}
