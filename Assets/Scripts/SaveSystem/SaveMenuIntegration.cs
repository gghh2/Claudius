using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple integration between pause menu and save system
/// Add this to the same GameObject as PauseMenuUI
/// </summary>
[RequireComponent(typeof(PauseMenuUI))]
public class SaveMenuIntegration : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button saveLoadButton;
    [SerializeField] private SaveSystemUI saveSystemUI;
    
    private PauseMenuUI pauseMenu;
    
    void Start()
    {
        pauseMenu = GetComponent<PauseMenuUI>();
        
        // Find SaveSystemUI if not assigned
        if (saveSystemUI == null)
        {
            saveSystemUI = FindFirstObjectByType<SaveSystemUI>();
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
    
    void OnDestroy()
    {
        if (saveLoadButton != null)
        {
            saveLoadButton.onClick.RemoveListener(OpenSaveMenu);
        }
    }
}
