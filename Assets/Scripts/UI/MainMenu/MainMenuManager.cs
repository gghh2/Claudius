using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Main menu manager - Handles the main menu scene
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadGamePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    
    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    
    [Header("Settings")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private float artificialLoadDelay = 0.5f;
    
    // Audio
    private AudioSource audioSource;
    
    void Start()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup buttons
        SetupButtons();
        
        // Check if continue is available - with delay to ensure SaveGameManager is ready
        StartCoroutine(UpdateContinueButtonDelayed());
        
        // Ensure we're at normal time scale
        Time.timeScale = 1f;
        
        // Hide loading screen
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
        
        // Show main menu
        ShowMainMenu();
        
        // Play menu music if configured
        if (MusicManager.Instance != null)
        {
            //MusicManager.Instance.PlayMenuMusic();
        }
    }
    
    IEnumerator UpdateContinueButtonDelayed()
    {
        // Wait a frame to ensure SaveGameManager is initialized
        yield return null;
        
        // Try multiple times in case SaveGameManager takes time to initialize
        for (int i = 0; i < 5; i++)
        {
            UpdateContinueButton();
            
            // If we found saves, stop trying
            if (continueButton != null && continueButton.interactable)
                break;
                
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void SetupButtons()
    {
        // New Game
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartNewGame);
        
        // Continue
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);
        
        // Load Game
        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(ShowLoadGameMenu);
        
        // Options
        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);
        
        // Credits
        if (creditsButton != null)
            creditsButton.onClick.AddListener(ShowCredits);
        
        // Quit
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }
    
    void UpdateContinueButton()
    {
        if (continueButton == null) return;
        
        // Check if we have any save
        bool hasSave = false;
        if (SaveGameManager.Instance != null)
        {
            string[] saves = SaveGameManager.Instance.GetAllSaves();
            hasSave = saves != null && saves.Length > 0;
            
            // Debug log
            Debug.Log($"[MainMenu] SaveGameManager found. Saves found: {saves?.Length ?? 0}");
            if (saves != null)
            {
                foreach (string save in saves)
                {
                    Debug.Log($"  - Save: {save}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[MainMenu] SaveGameManager.Instance is NULL!");
        }
        
        continueButton.interactable = hasSave;
        
        // Optional: Change text color to indicate disabled state
        TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            // Use custom colors: #323232 when active, #C3C3C3 when disabled
            buttonText.color = hasSave ? new Color32(0x32, 0x32, 0x32, 0xFF) : new Color32(0xC3, 0xC3, 0xC3, 0xFF);
        }
    }
    
    void StartNewGame()
    {
        PlayClickSound();
        
        // Optional: Show confirmation if save exists
        if (SaveGameManager.Instance != null && SaveGameManager.Instance.GetAllSaves().Length > 0)
        {
            // You could show a confirmation dialog here
            // For now, just start new game
        }
        
        StartCoroutine(LoadGameScene(true));
    }
    
    void ContinueGame()
    {
        PlayClickSound();
        
        if (SaveGameManager.Instance != null)
        {
            // Get all saves
            string[] saves = SaveGameManager.Instance.GetAllSaves();
            if (saves.Length > 0)
            {
                // Find the most recent save by checking modification time
                string mostRecentSave = FindMostRecentSave(saves);
                
                Debug.Log($"[MainMenu] Continue - Loading most recent save: {mostRecentSave}");
                
                // Start loading the game scene
                StartCoroutine(LoadGameScene(false, mostRecentSave));
            }
        }
    }
    
    string FindMostRecentSave(string[] saves)
    {
        if (saves.Length == 1)
            return saves[0];
            
        string mostRecent = saves[0];
        System.DateTime mostRecentTime = System.DateTime.MinValue;
        
        foreach (string save in saves)
        {
            // Try to get the save file info from SaveGameManager
            var saveTime = SaveGameManager.Instance.GetSaveDateTime(save);
            if (saveTime > mostRecentTime)
            {
                mostRecentTime = saveTime;
                mostRecent = save;
            }
        }
        
        // Fallback: if we can't get dates, use the highest numbered save
        if (mostRecentTime == System.DateTime.MinValue)
        {
            // Extract numbers and find highest
            int highestNum = -1;
            foreach (string save in saves)
            {
                string numStr = save.Replace("save_", "");
                if (int.TryParse(numStr, out int num))
                {
                    if (num > highestNum)
                    {
                        highestNum = num;
                        mostRecent = save;
                    }
                }
            }
        }
        
        return mostRecent;
    }
    
    void ShowLoadGameMenu()
    {
        PlayClickSound();
        
        if (loadGamePanel != null)
        {
            Debug.Log("[MainMenu] Showing LoadGamePanel");
            mainMenuPanel.SetActive(false);
            loadGamePanel.SetActive(true);
            
            // Setup back button if it exists
            SetupLoadGamePanelBackButton();
            
            // The SaveSystemUI or SimpleSaveGameUI should handle the rest
        }
        else
        {
            Debug.LogError("[MainMenu] LoadGamePanel is NULL! Please assign it in the Inspector.");
        }
    }
    
    void SetupLoadGamePanelBackButton()
    {
        // Look for a back button in the LoadGamePanel
        Button[] buttons = loadGamePanel.GetComponentsInChildren<Button>();
        foreach (Button btn in buttons)
        {
            if (btn.name.ToLower().Contains("back") || btn.name.ToLower().Contains("retour") || 
                btn.name.ToLower().Contains("close") || btn.name.ToLower().Contains("fermer"))
            {
                // Remove any existing listeners
                btn.onClick.RemoveAllListeners();
                // Add our simple back action
                btn.onClick.AddListener(ShowMainMenu);
                Debug.Log($"[MainMenu] Back button '{btn.name}' configured for LoadGamePanel");
            }
        }
    }
    
    void ShowOptions()
    {
        PlayClickSound();
        
        if (optionsPanel != null)
        {
            mainMenuPanel.SetActive(false);
            optionsPanel.SetActive(true);
            SetupBackButton(optionsPanel);
        }
    }
    
    void ShowCredits()
    {
        PlayClickSound();
        
        if (creditsPanel != null)
        {
            mainMenuPanel.SetActive(false);
            creditsPanel.SetActive(true);
            SetupBackButton(creditsPanel);
        }
    }
    
    void SetupBackButton(GameObject panel)
    {
        // Generic method to setup back buttons
        Button[] buttons = panel.GetComponentsInChildren<Button>();
        foreach (Button btn in buttons)
        {
            if (btn.name.ToLower().Contains("back") || btn.name.ToLower().Contains("retour") || 
                btn.name.ToLower().Contains("close") || btn.name.ToLower().Contains("fermer"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ShowMainMenu);
                Debug.Log($"[MainMenu] Back button '{btn.name}' configured");
            }
        }
    }
    
    public void ShowMainMenu()
    {
        PlayClickSound();
        
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);
        
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }
    
    void QuitGame()
    {
        PlayClickSound();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    public IEnumerator LoadGameScene(bool isNewGame, string saveToLoad = null)
    {
        // Hide all panels first
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
            
        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            loadingBar.value = 0f;
            loadingText.text = "Loading...";
        }
        
        // Small delay for UI feedback
        yield return new WaitForSeconds(0.1f);
        
        // Start async loading
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        asyncLoad.allowSceneActivation = false;
        
        // Update loading bar
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (loadingBar != null)
                loadingBar.value = progress;
            
            if (loadingText != null)
                loadingText.text = $"Loading... {(progress * 100f):F0}%";
            
            // Allow scene activation when ready
            if (asyncLoad.progress >= 0.9f)
            {
                // Add artificial delay if desired
                if (artificialLoadDelay > 0)
                {
                    yield return new WaitForSeconds(artificialLoadDelay);
                }
                
                // Store what we need to do after loading
                if (!isNewGame && !string.IsNullOrEmpty(saveToLoad))
                {
                    Debug.Log($"[MainMenu] Setting LoadOnStart to: {saveToLoad}");
                    PlayerPrefs.SetString("LoadOnStart", saveToLoad);
                }
                else
                {
                    Debug.Log("[MainMenu] Clearing LoadOnStart (new game)");
                    PlayerPrefs.DeleteKey("LoadOnStart");
                }
                PlayerPrefs.Save();
                
                // Verify it was saved
                string check = PlayerPrefs.GetString("LoadOnStart", "EMPTY");
                Debug.Log($"[MainMenu] LoadOnStart after save: {check}");
                
                // Store that we're coming from MainMenu
                PlayerPrefs.SetString("PreviousScene", "MainMenu");
                PlayerPrefs.Save();
                
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
    }
    
    void PlayClickSound()
    {
        if (SoundEffectsManager.Instance != null)
        {
            SoundEffectsManager.Instance.PlaySound("UI_Click");
        }
    }
    
    // Called when returning from game
    public void OnReturnFromGame()
    {
        UpdateContinueButton();
        ShowMainMenu();
    }
}
