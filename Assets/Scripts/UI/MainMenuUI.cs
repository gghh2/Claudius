using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Main menu controller - Displays when the game starts
/// Separate from pause menu for cleaner architecture
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject logoContainer;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Game Title")]
    [SerializeField] private TextMeshProUGUI gameTitleText;
    [SerializeField] private string gameTitle = "CLAUDIUS";
    
    [Header("Settings")]
    [SerializeField] private bool showContinueButton = true;
    [SerializeField] private float fadeInDuration = 1f;
    
    // Private variables
    private bool hasExistingSave = false;
    private bool isTransitioning = false;
    
    public static MainMenuUI Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Setup UI
        SetupButtons();
        
        // Check for existing saves
        CheckForExistingSaves();
        
        // Update UI based on save status
        UpdateButtonVisibility();
        
        // Set game title
        if (gameTitleText != null)
        {
            gameTitleText.text = gameTitle;
        }
        
        // In MainMenu scene, UnifiedUIManager shouldn't try to navigate
        // Just ensure the menu is visible directly
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        
        // Pause the game
        Time.timeScale = 0f;
        
        // Ensure cursor is visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Disable player control
        DisablePlayerControl();
        
        // Hide all other UI elements that shouldn't be visible
        HideGameplayUI();
        
        // Optional: Fade in effect
        if (fadeInDuration > 0)
        {
            StartCoroutine(FadeInMenu());
        }
    }
    
    void Update()
    {
        // Handle ESC to show quit confirmation from main menu
        if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Show quit confirmation
                ShowQuitConfirmation();
            }
        }
    }
    
    void ShowQuitConfirmation()
    {
        if (ConfirmationDialogManager.Instance != null)
        {
            ConfirmationDialogManager.Instance.ShowYesNoDialog(
                "Voulez-vous vraiment quitter le jeu ?",
                onYes: () => {
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                },
                onNo: null
            );
        }
    }
    
    void SetupButtons()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartNewGame);
            
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);
            
        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OpenLoadMenu);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);
            
        if (creditsButton != null)
            creditsButton.onClick.AddListener(ShowCredits);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }
    
    void CheckForExistingSaves()
    {
        // Check if there are any save files
        if (SaveGameManager.Instance != null)
        {
            string[] saves = SaveGameManager.Instance.GetAllSaves();
            hasExistingSave = saves != null && saves.Length > 0;
        }
    }
    
    void UpdateButtonVisibility()
    {
        // Show/hide continue button based on save existence
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(showContinueButton && hasExistingSave);
        }
        
        // Disable load button if no saves
        if (loadGameButton != null)
        {
            loadGameButton.interactable = hasExistingSave;
            
            // Optional: Change text color to show it's disabled
            var buttonText = loadGameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                // Use the button's disabled color from ColorBlock instead of forcing white
                if (!hasExistingSave)
                {
                    buttonText.color = new Color(1f, 1f, 1f, 0.5f);
                }
            }
        }
    }
    
    void StartNewGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        
        // Optional: Show confirmation if save exists
        if (hasExistingSave && ConfirmationDialogManager.Instance != null)
        {
            ConfirmationDialogManager.Instance.ShowYesNoDialog(
                "Une partie existe déjà. Commencer une nouvelle partie ?",
                onYes: () => {
                    StartCoroutine(TransitionToGame());
                },
                onNo: () => {
                    isTransitioning = false;
                }
            );
        }
        else
        {
            StartCoroutine(TransitionToGame());
        }
    }
    
    void ContinueGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        
        // Load the most recent save
        if (SaveGameManager.Instance != null)
        {
            string[] saves = SaveGameManager.Instance.GetAllSaves();
            if (saves != null && saves.Length > 0)
            {
                // Load the first save (assuming it's the most recent)
                SaveGameManager.Instance.LoadGame(saves[0]);
                StartCoroutine(TransitionToGame());
            }
        }
    }
    
    void HideGameplayUI()
    {
        // Hide stamina bar
        GameObject staminaUI = GameObject.Find("StaminaUI");
        if (staminaUI != null)
            staminaUI.SetActive(false);
            
        // Hide any other gameplay UI elements
        GameObject interactionPrompt = GameObject.Find("InteractionPrompt");
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
    
    void ShowGameplayUI()
    {
        // Show stamina bar
        GameObject staminaUI = GameObject.Find("StaminaUI");
        if (staminaUI != null)
            staminaUI.SetActive(true);
            
        // Show any other gameplay UI elements
        GameObject interactionPrompt = GameObject.Find("InteractionPrompt");
        if (interactionPrompt != null)
            interactionPrompt.SetActive(true);
    }
    
    void OpenLoadMenu()
    {
        // In MainMenu, manage panels directly without UnifiedUIManager
        GameObject loadPanel = GameObject.Find("LoadGamePanel");
        if (loadPanel != null)
        {
            loadPanel.SetActive(true);
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
        }
    }
    
    void OpenOptions()
    {
        // In MainMenu, manage panels directly without UnifiedUIManager
        GameObject optionsPanel = GameObject.Find("OptionsPanel");
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
        }
    }
    
    void ShowCredits()
    {
        // TODO: Implement credits screen
        Debug.Log("Credits not implemented yet");
    }
    
    void QuitGame()
    {
        // Optional: Show confirmation
        if (ConfirmationDialogManager.Instance != null)
        {
            ConfirmationDialogManager.Instance.ShowYesNoDialog(
                "Voulez-vous vraiment quitter le jeu ?",
                onYes: () => {
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                },
                onNo: null
            );
        }
        else
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
    
    IEnumerator TransitionToGame()
    {
        // Optional: Fade out effect
        if (fadeInDuration > 0)
        {
            // TODO: Implement fade out
            yield return new WaitForSecondsRealtime(0.5f);
        }
        
        // Don't use UnifiedUIManager in MainMenu
        // Just hide the menu and restore game state
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
            
        // Resume game time
        Time.timeScale = 1f;
        
        // Enable player control
        EnablePlayerControl();
        
        // Hide cursor for gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Show gameplay UI
        ShowGameplayUI();
        
        isTransitioning = false;
    }
    
    IEnumerator FadeInMenu()
    {
        // TODO: Implement fade in effect
        yield return null;
    }
    
    void DisablePlayerControl()
    {
        var player = FindFirstObjectByType<PlayerControllerCC>();
        if (player != null)
        {
            player.EnableControls(false);
        }
    }
    
    void EnablePlayerControl()
    {
        var player = FindFirstObjectByType<PlayerControllerCC>();
        if (player != null)
        {
            player.EnableControls(true);
        }
    }
    
    public void ReturnToMainMenu()
    {
        // Called when returning from game to main menu
        // Don't use UnifiedUIManager in MainMenu
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        
        // Hide gameplay UI
        HideGameplayUI();
        
        // Update save status
        CheckForExistingSaves();
        UpdateButtonVisibility();
        
        // Pause game
        Time.timeScale = 0f;
        
        // Disable player
        DisablePlayerControl();
    }
    
    void OnDestroy()
    {
        // Clean up button listeners
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(StartNewGame);
            
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueGame);
            
        if (loadGameButton != null)
            loadGameButton.onClick.RemoveListener(OpenLoadMenu);
            
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(OpenOptions);
            
        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(ShowCredits);
            
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }
}
