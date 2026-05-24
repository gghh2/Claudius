using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Pause menu UI controller
/// Handles in-game pause functionality
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject menuContainer;
    
    [Header("Pause Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button saveLoadButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    
    [Header("Options Panel")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backFromOptionsButton;
    
    [Header("Audio Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;

    [Header("Time Settings")]
    [Tooltip("Toggle pour activer le format 12h (coche = 12h, décoche = 24h). " +
        "Persistant via PlayerPrefs sous la clé GameClock.TimeFormat.")]
    [SerializeField] private Toggle use12HourToggle;
    [SerializeField] private TextMeshProUGUI timeFormatLabel;
    
    [Header("Debug Controls")]
    [SerializeField] private GameObject debugControlsPanel;
    [SerializeField] private Slider jumpHeightSlider;
    [SerializeField] private Slider moveSpeedSlider;
    [SerializeField] private TextMeshProUGUI jumpHeightValueText;
    [SerializeField] private TextMeshProUGUI moveSpeedValueText;
    [SerializeField] private Button resetDebugButton;
    [SerializeField] private bool showDebugControls = false;
    
    [Header("Settings")]
    [SerializeField] private Vector3 defaultSpawnPosition = Vector3.zero;
    [SerializeField] private Vector3 defaultSpawnRotation = Vector3.zero;

    // Static variables to store game session data
    private static Vector3 lastLoadedPosition = Vector3.zero;
    private static Vector3 lastLoadedRotation = Vector3.zero;
    private static bool hasLoadedSave = false;
    private static bool isInitialized = false;
    
    [Header("Additional Options")]
    [SerializeField] private Button resetAudioButton;
    [SerializeField] private Button resetAllSettingsButton;
    
    // Private variables
    private bool isPaused = false;
    private GameObject player;
    private PlayerControllerCC playerController;
    
    // Constants
    private const float DEFAULT_JUMP_HEIGHT = 2f;
    private const float DEFAULT_MOVE_SPEED = 5f;
    private const float DEFAULT_MUSIC_VOLUME = 0.7f;
    private const float DEFAULT_SFX_VOLUME = 1f;
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    void Start()
    {
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerControllerCC>();
            
            // Initialize default spawn position on first run
            if (!isInitialized)
            {
                defaultSpawnPosition = player.transform.position;
                defaultSpawnRotation = player.transform.eulerAngles;
                isInitialized = true;
            }
        }
        
        SetupUI();
        
        // Hide panels at start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        
        // Don't start coroutine if gameObject is inactive
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ApplyAudioVolumesDelayed());
        }
        else
        {
            // Apply volumes directly without delay if inactive
            float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
            float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
            
            ApplyMusicVolume(savedMusicVolume);
            ApplySFXVolume(savedSFXVolume);
        }
    }
    
    void SetupUI()
    {
        // Setup pause menu buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
            
        if (respawnButton != null)
            respawnButton.onClick.AddListener(ResetPlayerPosition);
            
        if (saveLoadButton != null)
            saveLoadButton.onClick.AddListener(ShowSaveMenu);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        
        // Note: backFromOptionsButton is handled by UINavigationButton
        
        SetupAudioControls();
        
        if (showDebugControls && debugControlsPanel != null)
            SetupDebugControls();
        
        if (resetAudioButton != null)
            resetAudioButton.onClick.AddListener(ResetAudioSettings);
            
        if (resetAllSettingsButton != null)
            resetAllSettingsButton.onClick.AddListener(ResetAllSettings);
    }
    
    void SetupDebugControls()
    {
        if (playerController == null) return;
        
        if (jumpHeightSlider != null)
        {
            jumpHeightSlider.minValue = 0.5f;
            jumpHeightSlider.maxValue = 5f;
            jumpHeightSlider.value = playerController.jumpHeight;
            jumpHeightSlider.onValueChanged.AddListener(OnJumpHeightChanged);
        }
        
        if (moveSpeedSlider != null)
        {
            moveSpeedSlider.minValue = 1f;
            moveSpeedSlider.maxValue = 50f;
            moveSpeedSlider.value = playerController.moveSpeed;
            moveSpeedSlider.onValueChanged.AddListener(OnMoveSpeedChanged);
        }
        
        if (resetDebugButton != null)
            resetDebugButton.onClick.AddListener(ResetDebugValues);
        
        UpdateDebugValueTexts();
    }
    
    void Update()
    {
        // SIMPLE CHECK: Shortcuts work ONLY when pause menu panel is visible
        // This prevents shortcuts from working during dialogues or other UI states
        if (pauseMenuPanel != null && pauseMenuPanel.activeInHierarchy)
        {
            // C - Continue/Resume
            if (Input.GetKeyDown(KeyCode.C))
            {
                Resume();
            }
            // R - Respawn
            else if (Input.GetKeyDown(KeyCode.R))
            {
                ResetPlayerPosition();
            }
            // O - Options (only if not already in options)
            else if (Input.GetKeyDown(KeyCode.O) && (optionsPanel == null || !optionsPanel.activeSelf))
            {
                ShowOptions();
            }
            // Q - Quit
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                QuitGame();
            }
        }
    }
    
    void OnEnable()
    {
        // Called when the panel becomes active
        if (!isPaused)
        {
            OnPauseMenuOpened();
        }
    }
    
    void OnDisable()
    {
        // Called when the panel becomes inactive.
        if (isPaused)
        {
            OnPauseMenuClosed();
        }
    }

    void OnPauseMenuOpened()
    {
        isPaused = true;

        if (playerController != null)
            playerController.enabled = false;
        // Le curseur est géré par SmartCursorManager (autorité unique).

        // L'horloge in-game est mise en pause (sinon elle continuerait via
        // unscaledDeltaTime même quand le joueur a explicitement pause).
        GameClock.Instance?.Pause();
    }

    void OnPauseMenuClosed()
    {
        isPaused = false;

        if (playerController != null)
            playerController.enabled = true;

        GameClock.Instance?.Resume();
    }
    
    public void Pause()
    {
        // This method is now deprecated - UnifiedUIManager handles navigation
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.PauseMenu);
        }
    }
    
    public void Resume()
    {
        // Use UnifiedUIManager for navigation
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateBack();
        }
    }
    
    void ShowSaveMenu()
    {
        Debug.Log("[PauseMenu] ShowSaveMenu called");
        
        if (UnifiedUIManager.Instance != null)
        {
            Debug.Log("[PauseMenu] UnifiedUIManager found, navigating to SaveMenu");
            UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.SaveMenu);
        }
        else
        {
            Debug.LogError("[PauseMenu] UnifiedUIManager.Instance is null!");
        }
    }
    
    void ResetPlayerPosition()
    {
        if (player != null)
        {
            Vector3 targetPosition;
            Vector3 targetRotation;
            
            // Determine where to respawn
            if (hasLoadedSave)
            {
                // A save has been loaded during this session - use loaded position
                targetPosition = lastLoadedPosition;
                targetRotation = lastLoadedRotation;
            }
            else
            {
                // No save loaded - use default spawn position
                targetPosition = defaultSpawnPosition;
                targetRotation = defaultSpawnRotation;
            }
            
            // Disable CharacterController before teleporting
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
            
            // Apply position and rotation
            player.transform.position = targetPosition;
            player.transform.eulerAngles = targetRotation;
            
            // Clear any velocity
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Re-enable CharacterController
            if (cc != null)
            {
                cc.enabled = true;
            }
            
            // Resume game
            Resume();
        }
    }
    
    IEnumerator ApplyAudioVolumesDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        
        float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        
        ApplyMusicVolume(savedMusicVolume);
        ApplySFXVolume(savedSFXVolume);
    }
    
    // (la gestion du curseur est entièrement déléguée à SmartCursorManager)
    
    void OnJumpHeightChanged(float value)
    {
        if (playerController != null)
            playerController.jumpHeight = value;
        UpdateDebugValueTexts();
    }
    
    void OnMoveSpeedChanged(float value)
    {
        if (playerController != null)
            playerController.moveSpeed = value;
        UpdateDebugValueTexts();
    }
    
    void UpdateDebugValueTexts()
    {
        if (jumpHeightValueText != null && jumpHeightSlider != null)
            jumpHeightValueText.text = jumpHeightSlider.value.ToString("F1");
            
        if (moveSpeedValueText != null && moveSpeedSlider != null)
            moveSpeedValueText.text = moveSpeedSlider.value.ToString("F1");
    }
    
    void ResetDebugValues()
    {
        if (jumpHeightSlider != null)
            jumpHeightSlider.value = DEFAULT_JUMP_HEIGHT;
            
        if (moveSpeedSlider != null)
            moveSpeedSlider.value = DEFAULT_MOVE_SPEED;
            
        UpdateDebugValueTexts();
    }
    
    void ShowOptions()
    {
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.Settings);
            
            if (debugControlsPanel != null)
                debugControlsPanel.SetActive(showDebugControls);
            
            ApplyCurrentAudioVolumes();
        }
    }
    
    void SetupAudioControls()
    {
        float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        
        SetupVolumeSlider(musicVolumeSlider, savedMusicVolume, OnMusicVolumeChanged, UpdateMusicVolumeText);
        SetupVolumeSlider(sfxVolumeSlider, savedSFXVolume, OnSFXVolumeChanged, UpdateSFXVolumeText);

        SetupTimeFormatToggle();
    }

    void SetupTimeFormatToggle()
    {
        if (use12HourToggle == null) return;

        use12HourToggle.onValueChanged.RemoveAllListeners();
        use12HourToggle.isOn = GameClock.Use12HourFormat;
        UpdateTimeFormatLabel(use12HourToggle.isOn);

        use12HourToggle.onValueChanged.AddListener(is12 =>
        {
            GameClock.CurrentFormat = is12 ? GameClock.TimeFormat.Hours12 : GameClock.TimeFormat.Hours24;
            UpdateTimeFormatLabel(is12);
        });
    }

    void UpdateTimeFormatLabel(bool is12)
    {
        if (timeFormatLabel != null)
            timeFormatLabel.text = is12 ? "Format 12h" : "Format 24h";
    }
    
    void SetupVolumeSlider(Slider slider, float savedValue, UnityEngine.Events.UnityAction<float> onChange, System.Action<float> updateText)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = savedValue;
            slider.onValueChanged.AddListener(onChange);
            updateText?.Invoke(savedValue);
        }
    }
    
    void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
        PlayerPrefs.Save();
        UpdateMusicVolumeText(value);
        ApplyMusicVolume(value);
    }
    
    void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        PlayerPrefs.Save();
        UpdateSFXVolumeText(value);
        ApplySFXVolume(value);
    }
    
    void UpdateMusicVolumeText(float value)
    {
        UpdateVolumeText(musicVolumeText, value);
    }
    
    void UpdateSFXVolumeText(float value)
    {
        UpdateVolumeText(sfxVolumeText, value);
    }
    
    void UpdateVolumeText(TextMeshProUGUI text, float value)
    {
        if (text != null)
            text.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
    
    float GetVolumeValue(Slider slider, string prefsKey, float defaultValue)
    {
        return slider != null ? slider.value : PlayerPrefs.GetFloat(prefsKey, defaultValue);
    }
    
    void ApplyCurrentAudioVolumes()
    {
        float musicVolume = GetVolumeValue(musicVolumeSlider, MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        float sfxVolume = GetVolumeValue(sfxVolumeSlider, SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
            
        ApplyMusicVolume(musicVolume);
        ApplySFXVolume(sfxVolume);
    }
    
    void ApplyMusicVolume(float volume)
    {
        MusicManager musicManager = FindFirstObjectByType<MusicManager>();
        if (musicManager != null)
            musicManager.SetMasterVolume(volume);
    }
    
    void ApplySFXVolume(float volume)
    {
        SoundEffectsManager sfxManager = FindFirstObjectByType<SoundEffectsManager>();
        if (sfxManager != null)
            sfxManager.SetMasterVolume(volume);
        
        FootstepSystem footsteps = FindFirstObjectByType<FootstepSystem>();
        if (footsteps != null)
            footsteps.SetFootstepVolume(volume * 0.7f);
    }
    
    void ResetAudioSettings()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = DEFAULT_MUSIC_VOLUME;
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = DEFAULT_SFX_VOLUME;
            
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        PlayerPrefs.Save();
    }
    
    void ResetAllSettings()
    {
        ResetAudioSettings();
        ResetDebugValues();
    }
    
    void ReturnToMainMenu()
    {
        // Show confirmation dialog
        if (ConfirmationDialogManager.Instance != null)
        {
            ConfirmationDialogManager.Instance.ShowDialog(
                "Retourner au menu principal ? Toute progression non sauvegardée sera perdue.",
                () => {
                    // Close pause menu
                    Resume();

                    // Clean up the game session
                    CleanupGameSession();

                    // Return to main menu
                    SceneNavigationManager.ReturnToMainMenu();
                },
                null,
                "Menu principal",
                "Menu principal",
                "Annuler");
        }
    }
    
    void CleanupGameSession()
    {
        // Stop all music
        MusicManager musicManager = FindFirstObjectByType<MusicManager>();
        if (musicManager != null)
        {
            musicManager.StopMusic();
        }
        
        // Stop all sound effects
        SoundEffectsManager sfxManager = FindFirstObjectByType<SoundEffectsManager>();
        if (sfxManager != null)
        {
            sfxManager.StopAllSounds();
        }
        
        // Stop footsteps
        FootstepSystem footsteps = FindFirstObjectByType<FootstepSystem>();
        if (footsteps != null)
        {
            footsteps.enabled = false;
        }
        
        // Optionally clear quests and other game state
        // This depends on if you want to reset everything or keep progress
        // QuestManager.Instance?.ClearAllQuests();
        // QuestJournal.Instance?.ClearAllQuests();
    }
    
    void QuitGame()
    {
        // Demande confirmation — toute progression non sauvegardée sera perdue.
        if (ConfirmationDialogManager.Instance != null)
        {
            ConfirmationDialogManager.Instance.ShowDialog(
                "Quitter le jeu ? Toute progression non sauvegardée sera perdue.",
                PerformQuit,
                null,
                "Quitter le jeu",
                "Quitter",
                "Annuler");
        }
        else
        {
            // Pas de dialogue disponible : quitte directement.
            PerformQuit();
        }
    }

    void PerformQuit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    public bool IsPaused() => isPaused;
    
    /// <summary>
    /// Updates the spawn position when a save is loaded
    /// </summary>
    public static void UpdateSpawnPosition(Vector3 position, Vector3 rotation)
    {
        lastLoadedPosition = position;
        lastLoadedRotation = rotation;
        hasLoadedSave = true;
    }
    
    /// <summary>
    /// Resets the session state (for testing)
    /// </summary>
    [ContextMenu("Reset Session State")]
    public void ResetSessionState()
    {
        hasLoadedSave = false;
        isInitialized = false;
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
        
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);
            
        if (respawnButton != null)
            respawnButton.onClick.RemoveListener(ResetPlayerPosition);
            
        if (saveLoadButton != null)
            saveLoadButton.onClick.RemoveListener(ShowSaveMenu);
            
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(ShowOptions);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
            
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            
        if (jumpHeightSlider != null)
            jumpHeightSlider.onValueChanged.RemoveListener(OnJumpHeightChanged);
            
        if (moveSpeedSlider != null)
            moveSpeedSlider.onValueChanged.RemoveListener(OnMoveSpeedChanged);
            
        if (resetDebugButton != null)
            resetDebugButton.onClick.RemoveListener(ResetDebugValues);
            
        if (resetAudioButton != null)
            resetAudioButton.onClick.RemoveListener(ResetAudioSettings);
            
        if (resetAllSettingsButton != null)
            resetAllSettingsButton.onClick.RemoveListener(ResetAllSettings);
    }
}
