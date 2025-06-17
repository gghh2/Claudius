using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Modern pause menu using Unity's UI system with SetActive()
/// </summary>
public class ModernPauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main pause menu GameObject to show/hide")]
    [SerializeField] private GameObject pauseMenuPanel;
    
    [Tooltip("Container for main menu buttons")]
    [SerializeField] private GameObject menuContainer;
    
    [Tooltip("Main Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Options Panel")]
    [Tooltip("Options panel container")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backFromOptionsButton;
    
    [Header("Audio Settings")]
    [Tooltip("Music volume slider")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    
    [Tooltip("Sound effects volume slider")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    
    [Tooltip("Debug Controls Panel (Optional)")]
    [SerializeField] private GameObject debugControlsPanel;
    [SerializeField] private Slider jumpHeightSlider;
    [SerializeField] private Slider moveSpeedSlider;
    [SerializeField] private TextMeshProUGUI jumpHeightValueText;
    [SerializeField] private TextMeshProUGUI moveSpeedValueText;
    [SerializeField] private Button resetDebugButton;
    
    [Header("Settings")]
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;
    [SerializeField] private Vector3 spawnRotation = Vector3.zero;
    [SerializeField] private bool autoSaveSpawnPosition = true;
    
    [Header("Debug Controls")]
    [Tooltip("Show debug controls in options menu")]
    [SerializeField] private bool showDebugControls = false;
    
    [Tooltip("Min/Max values for jump height slider")]
    [SerializeField] private Vector2 jumpHeightRange = new Vector2(8f, 150f);
    
    [Tooltip("Min/Max values for move speed slider")]
    [SerializeField] private Vector2 moveSpeedRange = new Vector2(1f, 50f);
    
    [Header("Additional Options")]
    [SerializeField] private Button resetAudioButton;
    [SerializeField] private Button resetAllSettingsButton;
    
    // Private variables
    private bool isPaused = false;
    private GameObject player;
    private PlayerControllerCC playerController;
    private bool cursorWasLocked = false;
    
    // Cached cursor managers
    private MonoBehaviour[] cursorManagers;
    
    // Default values
    private const float DEFAULT_JUMP_HEIGHT = 2f; // For Character Controller
    private const float DEFAULT_MOVE_SPEED = 5f;
    private const float DEFAULT_MUSIC_VOLUME = 0.7f;
    private const float DEFAULT_SFX_VOLUME = 1f;
    
    // Audio settings keys for PlayerPrefs
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    void Start()
    {
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerControllerCC>();
            
            if (autoSaveSpawnPosition)
            {
                spawnPosition = player.transform.position;
                spawnRotation = player.transform.eulerAngles;
            }
        }
        
        // Cache cursor managers
        CacheCursorManagers();
        
        // Setup UI
        SetupUI();
        
        // Hide pause menu and options at start
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
        
        // Apply audio volumes after a delay to ensure managers are ready
        StartCoroutine(ApplyAudioVolumesDelayed());
    }
    
    void CacheCursorManagers()
    {
        var managers = new System.Collections.Generic.List<MonoBehaviour>();
        
        // Find SmartCursorManager (the only cursor manager in the project)
        var smartCursor = FindObjectOfType<SmartCursorManager>();
        if (smartCursor != null) managers.Add(smartCursor);
        
        // Find any other MonoBehaviour that might control the cursor
        // by checking for scripts with "Cursor" in their name
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
        // Setup main menu button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
            
        if (respawnButton != null)
            respawnButton.onClick.AddListener(ResetPlayerPosition);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
            
        // Setup options panel
        if (backFromOptionsButton != null)
            backFromOptionsButton.onClick.AddListener(HideOptions);
            
        // Setup audio controls
        SetupAudioControls();
        
        // Setup debug controls (will be in options panel)
        if (showDebugControls && debugControlsPanel != null)
        {
            SetupDebugControls();
        }
        
        // Setup reset buttons
        if (resetAudioButton != null)
            resetAudioButton.onClick.AddListener(ResetAudioSettings);
            
        if (resetAllSettingsButton != null)
            resetAllSettingsButton.onClick.AddListener(ResetAllSettings);
    }
    
    void SetupDebugControls()
    {
        if (playerController == null) return;
        
        // Initialize sliders with current player values
        if (jumpHeightSlider != null)
        {
            // For Character Controller, use jump height directly
            jumpHeightSlider.minValue = 0.5f;
            jumpHeightSlider.maxValue = 5f;
            jumpHeightSlider.value = playerController.jumpHeight;
            jumpHeightSlider.onValueChanged.AddListener(OnJumpHeightChanged);
        }
        
        if (moveSpeedSlider != null)
        {
            moveSpeedSlider.minValue = moveSpeedRange.x;
            moveSpeedSlider.maxValue = moveSpeedRange.y;
            moveSpeedSlider.value = playerController.moveSpeed;
            moveSpeedSlider.onValueChanged.AddListener(OnMoveSpeedChanged);
        }
        
        if (resetDebugButton != null)
        {
            resetDebugButton.onClick.AddListener(ResetDebugValues);
        }
        
        // Update value displays
        UpdateDebugValueTexts();
    }
    
    void Update()
    {
        // Check for pause input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
        
        // Respawn shortcut when paused
        if (isPaused && Input.GetKeyDown(KeyCode.R))
        {
            ResetPlayerPosition();
        }
    }
    
    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        // Show pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        // Notify UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPanelState(UIPanelNames.PauseMenu, true);
        }
        
        // Handle cursor
        cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Disable other cursor managers temporarily
        SetCursorManagersEnabled(false);
        
        // Disable player controller
        if (playerController != null)
            playerController.enabled = false;
            
        // Force cursor visible
        StartCoroutine(ForceCursorVisible());
    }
    
    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        // Hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Notify UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPanelState(UIPanelNames.PauseMenu, false);
        }
        
        // Re-enable cursor managers
        SetCursorManagersEnabled(true);
        
        // Restore cursor state
        if (cursorWasLocked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        // Enable player controller
        if (playerController != null)
            playerController.enabled = true;
    }
    
    void ResetPlayerPosition()
    {
        if (player != null)
        {
            // Reset position and rotation
            player.transform.position = spawnPosition;
            player.transform.eulerAngles = spawnRotation;
            
            // Reset velocity if using Rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Handle CharacterController
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = spawnPosition;
                cc.enabled = true;
            }
            
            // Resume game after respawn
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
        // Wait a bit for audio managers to initialize
        yield return new WaitForSeconds(0.5f);
        
        // Apply saved volumes
        float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        
        ApplyMusicVolume(savedMusicVolume);
        ApplySFXVolume(savedSFXVolume);
    }
    
    IEnumerator ForceCursorVisible()
    {
        // Force cursor to stay visible for a few frames
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForEndOfFrame();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    // Debug control methods
    void OnJumpHeightChanged(float value)
    {
        if (playerController != null)
        {
            playerController.jumpHeight = value;
        }
        UpdateDebugValueTexts();
    }
    
    void OnMoveSpeedChanged(float value)
    {
        if (playerController != null)
        {
            playerController.moveSpeed = value;
        }

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
        if (optionsPanel != null)
        {
            // If menuContainer is assigned, use it. Otherwise fall back to old behavior
            if (menuContainer != null)
            {
                // Hide main menu container (buttons) but keep the background
                menuContainer.SetActive(false);
            }
            else if (pauseMenuPanel != null)
            {
                // Fallback: hide entire pause menu panel if no menuContainer
                pauseMenuPanel.SetActive(false);
            }
            
            // Show options panel
            optionsPanel.SetActive(true);
            
            // Show/hide debug controls based on setting
            if (debugControlsPanel != null)
            {
                debugControlsPanel.SetActive(showDebugControls);
            }
            
            // Apply current audio volumes when showing options
            ApplyCurrentAudioVolumes();
        }
    }
    
    void HideOptions()
    {
        if (optionsPanel != null)
        {
            // Hide options panel
            optionsPanel.SetActive(false);
        }
        
        // Show main menu container or pause menu panel
        if (menuContainer != null)
        {
            menuContainer.SetActive(true);
        }
        else if (pauseMenuPanel != null)
        {
            // Fallback if menuContainer not assigned
            pauseMenuPanel.SetActive(true);
        }
    }
    
    void SetupAudioControls()
    {
        // Load saved volumes
        float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        
        // Setup sliders
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
    
    // Helper method to get current or saved volume
    float GetVolumeValue(Slider slider, string prefsKey, float defaultValue)
    {
        return slider != null ? slider.value : PlayerPrefs.GetFloat(prefsKey, defaultValue);
    }
    
    void ApplyCurrentAudioVolumes()
    {
        // Get current slider values or saved values
        float musicVolume = GetVolumeValue(musicVolumeSlider, MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        float sfxVolume = GetVolumeValue(sfxVolumeSlider, SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
            
        // Force apply both volumes
        ApplyMusicVolume(musicVolume);
        ApplySFXVolume(sfxVolume);
    }
    
    void ApplyMusicVolume(float volume)
    {
        // Apply to MusicManager if it exists
        MusicManager musicManager = FindObjectOfType<MusicManager>();
        if (musicManager != null)
        {
            musicManager.SetMasterVolume(volume);
        }
    }
    
    void ApplySFXVolume(float volume)
    {
        // Apply to SoundEffectsManager if it exists
        SoundEffectsManager sfxManager = FindObjectOfType<SoundEffectsManager>();
        if (sfxManager != null)
        {
            sfxManager.SetMasterVolume(volume);
        }
        
        // Also apply to FootstepSystem if it exists
        FootstepSystem footsteps = FindObjectOfType<FootstepSystem>();
        if (footsteps != null)
        {
            footsteps.SetFootstepVolume(volume * 0.7f); // Footsteps at 70% of master SFX
        }
    }
    
    void ResetAudioSettings()
    {
        // Reset to default values
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = DEFAULT_MUSIC_VOLUME;
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = DEFAULT_SFX_VOLUME;
            
        // Save defaults
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        PlayerPrefs.Save();
    }
    
    void ResetAllSettings()
    {
        // Reset audio
        ResetAudioSettings();
        
        // Reset debug values
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
    
    void OnDestroy()
    {
        // Ensure time scale is reset
        Time.timeScale = 1f;
        
        // Remove button listeners to prevent memory leaks
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);
            
        if (respawnButton != null)
            respawnButton.onClick.RemoveListener(ResetPlayerPosition);
            
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(ShowOptions);
            
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
            
        if (backFromOptionsButton != null)
            backFromOptionsButton.onClick.RemoveListener(HideOptions);
            
        // Remove audio listeners
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            
        // Remove debug listeners
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