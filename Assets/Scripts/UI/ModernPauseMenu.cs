using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Modern pause menu controller
/// Works with UnifiedUIManager for navigation
/// </summary>
public class ModernPauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject menuContainer;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Options Panel")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backFromOptionsButton;
    
    [Header("Audio Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    
    [Header("Debug Controls")]
    [SerializeField] private GameObject debugControlsPanel;
    [SerializeField] private Slider jumpHeightSlider;
    [SerializeField] private Slider moveSpeedSlider;
    [SerializeField] private TextMeshProUGUI jumpHeightValueText;
    [SerializeField] private TextMeshProUGUI moveSpeedValueText;
    [SerializeField] private Button resetDebugButton;
    [SerializeField] private bool showDebugControls = false;
    
    [Header("Settings")]
    [SerializeField] private Vector3 defaultSpawnPosition = Vector3.zero;  // Position initiale du jeu
    [SerializeField] private Vector3 defaultSpawnRotation = Vector3.zero;  // Rotation initiale du jeu
    [SerializeField] private bool autoSaveSpawnPosition = true;
    
    // Static variables to store game session data
    private static Vector3 lastLoadedPosition = Vector3.zero;
    private static Vector3 lastLoadedRotation = Vector3.zero;
    private static bool hasLoadedSave = false;  // Changed from hasLoadedPosition
    private static bool isInitialized = false;
    
    [Header("Additional Options")]
    [SerializeField] private Button resetAudioButton;
    [SerializeField] private Button resetAllSettingsButton;
    
    // Private variables
    private bool isPaused = false;
    private GameObject player;
    private PlayerControllerCC playerController;
    private bool cursorWasLocked = false;
    private MonoBehaviour[] cursorManagers;
    
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
                Debug.Log($"[PauseMenu] Game started - Default spawn position set to: {defaultSpawnPosition}");
            }
        }
        
        CacheCursorManagers();
        SetupUI();
        
        // Hide panels at start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        
        StartCoroutine(ApplyAudioVolumesDelayed());
    }
    
    void CacheCursorManagers()
    {
        var managers = new System.Collections.Generic.List<MonoBehaviour>();
        
        var smartCursor = FindObjectOfType<SmartCursorManager>();
        if (smartCursor != null) managers.Add(smartCursor);
        
        foreach (var mono in FindObjectsOfType<MonoBehaviour>())
        {
            if (mono != null && mono != this && 
                mono.GetType().Name.Contains("Cursor") && 
                !managers.Contains(mono))
            {
                managers.Add(mono);
            }
        }
        
        cursorManagers = managers.ToArray();
    }
    
    void SetupUI()
    {
        // Setup main menu buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
            
        if (respawnButton != null)
            respawnButton.onClick.AddListener(ResetPlayerPosition);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);
            
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
        // Test if Update is being called
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log($"[PauseMenu] ESC pressed - isPaused: {isPaused}, Time.timeScale: {Time.timeScale}");
        }
        
        // SIMPLE CHECK: Shortcuts work ONLY when pause menu panel is visible
        // This prevents shortcuts from working during dialogues or other UI states
        if (pauseMenuPanel != null && pauseMenuPanel.activeInHierarchy)
        {
            // Debug to check if we're in the paused state
            if (Input.anyKeyDown)
            {
                Debug.Log($"[PauseMenu] Key pressed while paused. isPaused: {isPaused}, Current key: {Input.inputString}");
            }
            
            // C - Continue/Resume (retour au jeu)
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("[PauseMenu] C key pressed - Resume");
                Resume();
            }
            // R - Respawn (retour aux coordonnées du dernier Load/début)
            else if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("[PauseMenu] R key pressed - Respawn");
                ResetPlayerPosition();
            }
            // O - Options (only if not already in options)
            else if (Input.GetKeyDown(KeyCode.O) && (optionsPanel == null || !optionsPanel.activeSelf))
            {
                Debug.Log("[PauseMenu] O key pressed - Options");
                ShowOptions();
            }
            // Q - Quit
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log("[PauseMenu] Q key pressed - Quit");
                QuitGame();
            }
        }
    }
    
    public void Pause()
    {
        isPaused = true;
        
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.PauseMenu);
        }
        else
        {
            Time.timeScale = 0f;
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }
        
        cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        SetCursorManagersEnabled(false);
        
        if (playerController != null)
            playerController.enabled = false;
            
        StartCoroutine(ForceCursorVisible());
    }
    
    public void Resume()
    {
        isPaused = false;
        
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateBack();
        }
        else
        {
            Time.timeScale = 1f;
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }
        
        SetCursorManagersEnabled(true);
        
        if (cursorWasLocked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        if (playerController != null)
            playerController.enabled = true;
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
                Debug.Log($"[PauseMenu] Respawning to last loaded position: {targetPosition}");
            }
            else
            {
                // No save loaded - use default spawn position
                targetPosition = defaultSpawnPosition;
                targetRotation = defaultSpawnRotation;
                Debug.Log($"[PauseMenu] Respawning to default position: {targetPosition}");
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
                rb.velocity = Vector3.zero;
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
    
    void SetCursorManagersEnabled(bool enabled)
    {
        if (cursorManagers != null)
        {
            foreach (var manager in cursorManagers)
            {
                if (manager != null)
                    manager.enabled = enabled;
            }
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
    
    IEnumerator ForceCursorVisible()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForEndOfFrame();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
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
        MusicManager musicManager = FindObjectOfType<MusicManager>();
        if (musicManager != null)
            musicManager.SetMasterVolume(volume);
    }
    
    void ApplySFXVolume(float volume)
    {
        SoundEffectsManager sfxManager = FindObjectOfType<SoundEffectsManager>();
        if (sfxManager != null)
            sfxManager.SetMasterVolume(volume);
        
        FootstepSystem footsteps = FindObjectOfType<FootstepSystem>();
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
    
    void QuitGame()
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
        Debug.Log($"[PauseMenu] Save loaded - Respawn position updated to: {position}");
    }
    
    /// <summary>
    /// Resets the session state (for testing)
    /// </summary>
    [ContextMenu("Reset Session State")]
    public void ResetSessionState()
    {
        hasLoadedSave = false;
        isInitialized = false;
        Debug.Log("[PauseMenu] Session state reset");
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
        
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);
            
        if (respawnButton != null)
            respawnButton.onClick.RemoveListener(ResetPlayerPosition);
            
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(ShowOptions);
            
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
