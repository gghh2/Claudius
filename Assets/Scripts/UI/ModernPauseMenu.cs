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
    
    [Tooltip("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button quitButton;
    
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
    [Tooltip("Show debug sliders in pause menu")]
    [SerializeField] private bool showDebugControls = false;
    
    [Tooltip("Min/Max values for jump height slider")]
    [SerializeField] private Vector2 jumpHeightRange = new Vector2(8f, 150f);
    
    [Tooltip("Min/Max values for move speed slider")]
    [SerializeField] private Vector2 moveSpeedRange = new Vector2(1f, 50f);
    
    // Private variables
    private bool isPaused = false;
    private GameObject player;
    private PlayerController playerController;
    private bool cursorWasLocked = false;
    
    // Cached cursor managers
    private MonoBehaviour[] cursorManagers;
    
    // Default values
    private const float DEFAULT_JUMP_HEIGHT = 8f;
    private const float DEFAULT_MOVE_SPEED = 10f;
    
    void Start()
    {
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            
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
        
        // Hide pause menu at start
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
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
        // Setup button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
            
        if (respawnButton != null)
            respawnButton.onClick.AddListener(ResetPlayerPosition);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
            
        // Setup debug controls if enabled
        if (showDebugControls && debugControlsPanel != null)
        {
            debugControlsPanel.SetActive(true);
            SetupDebugControls();
        }
        else if (debugControlsPanel != null)
        {
            debugControlsPanel.SetActive(false);
        }
    }
    
    void SetupDebugControls()
    {
        if (playerController == null) return;
        
        // Initialize sliders with current player values
        if (jumpHeightSlider != null)
        {
            jumpHeightSlider.minValue = jumpHeightRange.x;
            jumpHeightSlider.maxValue = jumpHeightRange.y;
            jumpHeightSlider.value = playerController.jumpForce;
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
            playerController.jumpForce = value;
            UpdateDebugValueTexts();
        }
    }
    
    void OnMoveSpeedChanged(float value)
    {
        if (playerController != null)
        {
            playerController.moveSpeed = value;
            UpdateDebugValueTexts();
        }
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
            
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
            
        if (jumpHeightSlider != null)
            jumpHeightSlider.onValueChanged.RemoveListener(OnJumpHeightChanged);
            
        if (moveSpeedSlider != null)
            moveSpeedSlider.onValueChanged.RemoveListener(OnMoveSpeedChanged);
            
        if (resetDebugButton != null)
            resetDebugButton.onClick.RemoveListener(ResetDebugValues);
    }
}