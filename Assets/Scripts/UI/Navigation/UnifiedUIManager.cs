using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unified UI Manager - Central navigation system for all UI panels
/// Uses a stack-based navigation to handle ESCAPE properly
/// </summary>
public class UnifiedUIManager : MonoBehaviour
{
    #region Singleton
    private static UnifiedUIManager instance;
    public static UnifiedUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UnifiedUIManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("UnifiedUIManager");
                    instance = go.AddComponent<UnifiedUIManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    #endregion

    #region UI Panel References
    [Header("UI Panel References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject saveMenuPanel;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject questJournalPanel;
    [SerializeField] private GameObject confirmDialog;
    [SerializeField] private GameObject notificationPanel;
    #endregion

    #region Layer Management
    [Header("Layer Configuration")]
    [SerializeField] private int baseUILayer = 0;
    [SerializeField] private int dialogueLayer = 10;
    [SerializeField] private int pauseMenuLayer = 20;
    [SerializeField] private int saveMenuLayer = 30;
    [SerializeField] private int confirmDialogLayer = 40;
    [SerializeField] private int notificationLayer = 50;
    #endregion

    #region Navigation State
    // Navigation stack to track panel history
    private Stack<string> navigationStack = new Stack<string>();
    
    // Special state to track if we're in game
    private const string GAME_STATE = "GAME";
    
    // Currently active panel
    private string currentPanel = GAME_STATE;
    
    // Panel configurations
    private Dictionary<string, PanelConfig> panelConfigs = new Dictionary<string, PanelConfig>();
    
    // Class to store panel configuration
    private class PanelConfig
    {
        public GameObject gameObject;
        public int sortOrder;
        public bool blocksPause;
        public bool blocksGameplay;
        public bool blocksDirect;
        public bool allowsEscape = true;
        public bool blocksAllInput = false;
        public List<string> allowedTransitions = new List<string>();
        
        public PanelConfig(GameObject go, int order, bool blocksPause = false, bool blocksGameplay = true)
        {
            this.gameObject = go;
            this.sortOrder = order;
            this.blocksPause = blocksPause;
            this.blocksGameplay = blocksGameplay;
            this.blocksDirect = blocksGameplay;
        }
    }
    #endregion

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePanels();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void InitializePanels()
    {
        // Configure all panels according to the navigation rules
        ConfigurePanel(UnifiedUIPanelNames.MainMenu, mainMenuPanel, baseUILayer, true, true,
            new[] { UnifiedUIPanelNames.Settings, UnifiedUIPanelNames.SaveMenu });
        
        // Special configuration for MainMenu - doesn't allow escape
        if (panelConfigs.ContainsKey(UnifiedUIPanelNames.MainMenu))
        {
            panelConfigs[UnifiedUIPanelNames.MainMenu].allowsEscape = false;
        }
            
        ConfigurePanel(UnifiedUIPanelNames.PauseMenu, pauseMenuPanel, pauseMenuLayer, false, true,
            new[] { UnifiedUIPanelNames.Settings, UnifiedUIPanelNames.SaveMenu });
            
        ConfigurePanel(UnifiedUIPanelNames.Settings, optionsPanel, pauseMenuLayer + 1, false, true,
            new[] { UnifiedUIPanelNames.PauseMenu });
            
        ConfigurePanel(UnifiedUIPanelNames.SaveMenu, saveMenuPanel, saveMenuLayer, false, true,
            new[] { UnifiedUIPanelNames.PauseMenu });
            
        ConfigurePanel(UnifiedUIPanelNames.Dialogue, dialoguePanel, dialogueLayer, true, true,
            new[] { UnifiedUIPanelNames.DialogueHistory, GAME_STATE });
            
        ConfigurePanel(UnifiedUIPanelNames.DialogueHistory, historyPanel, dialogueLayer + 1, true, true,
            new[] { UnifiedUIPanelNames.Dialogue });
            
        ConfigurePanel(UnifiedUIPanelNames.QuestJournal, questJournalPanel, pauseMenuLayer, true, true,
            new[] { GAME_STATE });
            
        ConfigurePanel(UnifiedUIPanelNames.Inventory, inventoryPanel, pauseMenuLayer, true, true,
            new[] { GAME_STATE });
            
        // Confirmation dialog special configuration
        var confirmConfig = ConfigurePanel(UnifiedUIPanelNames.Confirmation, confirmDialog, confirmDialogLayer, false, true);
        if (confirmConfig != null)
        {
            confirmConfig.allowsEscape = false;
            confirmConfig.blocksAllInput = true;
        }
        
        ConfigurePanel(UnifiedUIPanelNames.Notification, notificationPanel, notificationLayer, false, false);
        
        // Setup canvas sorting
        SetupCanvasSorting();
        
        // Start in game state
        navigationStack.Clear();
        currentPanel = GAME_STATE;
        
        // Hide all panels at start (they will be shown as needed)
        HideAllPanelsAtStart();
    }

    PanelConfig ConfigurePanel(string name, GameObject panel, int sortOrder, bool blocksPause, bool blocksGameplay, string[] allowedTransitions = null)
    {
        if (panel != null)
        {
            var config = new PanelConfig(panel, sortOrder, blocksPause, blocksGameplay);
            if (allowedTransitions != null)
            {
                config.allowedTransitions.AddRange(allowedTransitions);
            }
            panelConfigs[name] = config;
            return config;
        }
        return null;
    }

    void SetupCanvasSorting()
    {
        foreach (var config in panelConfigs)
        {
            SetupPanelCanvas(config.Value.gameObject, config.Value.sortOrder);
        }
    }
    
    void HideAllPanelsAtStart()
    {
        // Check if we're in the MainMenu scene
        bool isMainMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu";
        
        foreach (var kvp in panelConfigs)
        {
            if (kvp.Value.gameObject != null)
            {
                // In MainMenu scene, only hide non-MainMenu panels
                // In Game scene, hide all panels
                if (!isMainMenuScene || kvp.Key != UnifiedUIPanelNames.MainMenu)
                {
                    kvp.Value.gameObject.SetActive(false);
                }
            }
        }
        
        // If we're in MainMenu scene, ensure MainMenu is visible and set as current
        if (isMainMenuScene && panelConfigs.ContainsKey(UnifiedUIPanelNames.MainMenu))
        {
            var mainMenuConfig = panelConfigs[UnifiedUIPanelNames.MainMenu];
            if (mainMenuConfig.gameObject != null)
            {
                mainMenuConfig.gameObject.SetActive(true);
                currentPanel = UnifiedUIPanelNames.MainMenu;
                navigationStack.Clear();
            }
        }
    }

    void SetupPanelCanvas(GameObject panel, int sortOrder)
    {
        if (panel == null) return;
        
        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = panel.AddComponent<Canvas>();
            panel.AddComponent<GraphicRaycaster>();
        }
        
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortOrder;
    }

    void Update()
    {
        // Check if a modal dialog is blocking all input
        if (IsModalDialogOpen())
        {
            return;
        }
        
        // Handle ESCAPE key for navigation
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
        }
        
        // Handle direct shortcuts ONLY from game state
        if (currentPanel == GAME_STATE && !IsAnyBlockingPanelOpen())
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                NavigateTo(UnifiedUIPanelNames.QuestJournal);
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                NavigateTo(UnifiedUIPanelNames.Inventory);
            }
        }
    }

    #region Navigation Methods

    /// <summary>
    /// Navigate to a specific panel
    /// </summary>
    public void NavigateTo(string panelName)
    {
        Debug.Log($"[UnifiedUIManager] NavigateTo called: {panelName}");
        
        // Block navigation if a modal dialog is open
        if (IsModalDialogOpen() && panelName != UnifiedUIPanelNames.Confirmation)
        {
            Debug.Log("[UnifiedUIManager] Navigation blocked by modal dialog");
            return;
        }
        
        // Check if transition is allowed
        if (!IsTransitionAllowed(currentPanel, panelName))
        {
            Debug.Log($"[UnifiedUIManager] Transition not allowed from {currentPanel} to {panelName}");
            return;
        }
        
        // Check if panel exists in config
        if (!panelConfigs.ContainsKey(panelName))
        {
            Debug.LogError($"[UnifiedUIManager] Panel not found in configs: {panelName}");
            return;
        }
        
        // Check if panel GameObject is assigned
        if (panelConfigs[panelName].gameObject == null)
        {
            Debug.LogError($"[UnifiedUIManager] Panel GameObject is null for: {panelName}");
            return;
        }
        
        // Special handling for notifications
        if (panelName == UnifiedUIPanelNames.Notification)
        {
            ShowPanel(panelName);
            return;
        }
        
        // Special handling for confirmation dialog
        if (panelName == UnifiedUIPanelNames.Confirmation)
        {
            Debug.Log("[UnifiedUIManager] NavigateTo: Special handling for Confirmation dialog");
            ShowModalDialog(panelName);
            return;
        }
        
        // Push current state to stack (unless it's the same panel)
        if (currentPanel != panelName)
        {
            navigationStack.Push(currentPanel);
            
            // Hide current panel
            if (currentPanel != GAME_STATE)
            {
                HidePanel(currentPanel);
            }
            
            // Show new panel
            ShowPanel(panelName);
            currentPanel = panelName;
            
            Debug.Log($"[UnifiedUIManager] Successfully navigated to: {panelName}");
        }
    }

    /// <summary>
    /// Navigate back to previous panel
    /// </summary>
    public void NavigateBack()
    {
        if (navigationStack.Count > 0)
        {
            // Hide current panel
            if (currentPanel != GAME_STATE)
            {
                HidePanel(currentPanel);
            }
            
            // Pop previous state
            string previousPanel = navigationStack.Pop();
            currentPanel = previousPanel;
            
            // Show previous panel (unless it's game state)
            if (previousPanel != GAME_STATE)
            {
                ShowPanel(previousPanel);
            }
            else
            {
                // Returning to game - ensure everything is restored
                Time.timeScale = 1f;
                EnablePlayerControl();
                
                // Force cursor state update
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                
                // Let SmartCursorManager take over after a frame
                StartCoroutine(RestoreCursorStateDelayed());
            }
        }
    }

    void HandleEscapeKey()
    {
        // Check if a modal dialog is blocking all input
        if (IsModalDialogOpen())
        {
            return;
        }
        
        // Check if current panel allows escape
        if (currentPanel != GAME_STATE && panelConfigs.ContainsKey(currentPanel))
        {
            var config = panelConfigs[currentPanel];
            if (config.allowsEscape)
            {
                NavigateBack();
            }
        }
        else if (currentPanel == GAME_STATE)
        {
            // From game, ESC opens pause menu (not main menu)
            NavigateTo(UnifiedUIPanelNames.PauseMenu);
        }
        // No action for ESC from MainMenu - it doesn't allow escape
    }

    bool IsTransitionAllowed(string from, string to)
    {
        // Always allow transition to game state
        if (to == GAME_STATE) return true;
        
        // Always allow transition from game state
        if (from == GAME_STATE) return true;
        
        // Check specific panel rules
        if (panelConfigs.ContainsKey(from))
        {
            var config = panelConfigs[from];
            // If no specific transitions defined, allow all
            if (config.allowedTransitions.Count == 0) return true;
            // Otherwise check if transition is in allowed list
            return config.allowedTransitions.Contains(to);
        }
        
        return true;
    }

    bool IsAnyBlockingPanelOpen()
    {
        return currentPanel != GAME_STATE && panelConfigs.ContainsKey(currentPanel) && panelConfigs[currentPanel].blocksDirect;
    }

    bool IsModalDialogOpen()
    {
        if (panelConfigs.ContainsKey(currentPanel))
        {
            return panelConfigs[currentPanel].blocksAllInput;
        }
        
        if (currentPanel == UnifiedUIPanelNames.Confirmation && 
            panelConfigs.ContainsKey(UnifiedUIPanelNames.Confirmation))
        {
            var confirmPanel = panelConfigs[UnifiedUIPanelNames.Confirmation];
            return confirmPanel.gameObject != null && confirmPanel.gameObject.activeSelf;
        }
        
        return false;
    }

    #endregion

    #region Panel Management

    void ShowPanel(string panelName)
    {
        if (panelConfigs.ContainsKey(panelName))
        {
            var config = panelConfigs[panelName];
            if (config.gameObject != null)
            {
                config.gameObject.SetActive(true);
                
                // Handle special cases
                if (config.blocksGameplay)
                {
                    DisablePlayerControl();
                    Time.timeScale = 0f;
                }
            }
        }
    }

    void HidePanel(string panelName)
    {
        if (panelConfigs.ContainsKey(panelName))
        {
            var config = panelConfigs[panelName];
            if (config.gameObject != null)
            {
                config.gameObject.SetActive(false);
                
                // Check if we're returning to game state
                if (currentPanel == GAME_STATE)
                {
                    Time.timeScale = 1f;
                    EnablePlayerControl();
                }
                else if (config.blocksGameplay)
                {
                    // Check if any other blocking panel is still active
                    bool anyBlockingPanelActive = false;
                    foreach (var kvp in panelConfigs)
                    {
                        if (kvp.Key != panelName && kvp.Value.blocksGameplay && 
                            kvp.Value.gameObject != null && kvp.Value.gameObject.activeSelf)
                        {
                            anyBlockingPanelActive = true;
                            break;
                        }
                    }
                    
                    if (!anyBlockingPanelActive)
                    {
                        Time.timeScale = 1f;
                        EnablePlayerControl();
                    }
                }
            }
        }
    }

    void DisablePlayerControl()
    {
        var player = FindObjectOfType<PlayerControllerCC>();
        if (player != null)
        {
            player.EnableControls(false);
        }
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        var cursorManager = FindObjectOfType<SmartCursorManager>();
        if (cursorManager != null)
        {
            cursorManager.enabled = false;
        }
    }

    void EnablePlayerControl()
    {
        var player = FindObjectOfType<PlayerControllerCC>();
        if (player != null)
        {
            player.enabled = true;
            player.EnableControls(true);
            Time.timeScale = 1f;
        }
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        var cursorManager = FindObjectOfType<SmartCursorManager>();
        if (cursorManager != null)
        {
            cursorManager.enabled = true;
        }
    }
    
    IEnumerator RestoreCursorStateDelayed()
    {
        yield return null;
        
        var cursorManager = FindObjectOfType<SmartCursorManager>();
        if (cursorManager != null)
        {
            cursorManager.enabled = true;
        }
    }

    void ShowModalDialog(string dialogName)
    {
        Debug.Log($"[UnifiedUIManager] ShowModalDialog called for: {dialogName}");
        
        if (panelConfigs.ContainsKey(dialogName))
        {
            var config = panelConfigs[dialogName];
            if (config.gameObject != null)
            {
                Debug.Log($"[UnifiedUIManager] Activating panel: {dialogName}");
                config.gameObject.SetActive(true);
                config.gameObject.transform.SetAsLastSibling();
                currentPanel = dialogName;
            }
            else
            {
                Debug.LogError($"[UnifiedUIManager] Panel GameObject is NULL for: {dialogName}");
            }
        }
        else
        {
            Debug.LogError($"[UnifiedUIManager] No config found for panel: {dialogName}");
        }
    }
    
    /// <summary>
    /// Close a modal dialog
    /// </summary>
    public void CloseModalDialog(string dialogName)
    {
        if (panelConfigs.ContainsKey(dialogName) && currentPanel == dialogName)
        {
            var config = panelConfigs[dialogName];
            if (config.gameObject != null)
            {
                config.gameObject.SetActive(false);
                
                if (navigationStack.Count > 0)
                {
                    currentPanel = navigationStack.Peek();
                }
                else
                {
                    currentPanel = GAME_STATE;
                }
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Open a panel (legacy support - redirects to NavigateTo)
    /// </summary>
    public void OpenPanel(string panelName)
    {
        NavigateTo(panelName);
    }

    /// <summary>
    /// Close a panel (legacy support - calls NavigateBack if it's the current panel)
    /// </summary>
    public void ClosePanel(string panelName)
    {
        if (currentPanel == panelName)
        {
            NavigateBack();
        }
    }

    /// <summary>
    /// Check if a panel is open
    /// </summary>
    public bool IsPanelOpen(string panelName)
    {
        return currentPanel == panelName && panelConfigs.ContainsKey(panelName) && 
               panelConfigs[panelName].gameObject != null && panelConfigs[panelName].gameObject.activeSelf;
    }

    /// <summary>
    /// Check if any UI is blocking pause menu
    /// </summary>
    public bool IsBlockingPauseMenu()
    {
        return currentPanel != GAME_STATE && panelConfigs.ContainsKey(currentPanel) && panelConfigs[currentPanel].blocksPause;
    }

    /// <summary>
    /// Check if any UI is blocking gameplay
    /// </summary>
    public bool IsBlockingGameplay()
    {
        return currentPanel != GAME_STATE && panelConfigs.ContainsKey(currentPanel) && panelConfigs[currentPanel].blocksGameplay;
    }
    
    /// <summary>
    /// Check if input is currently blocked by a modal dialog
    /// </summary>
    public bool IsInputBlocked()
    {
        return IsModalDialogOpen();
    }

    /// <summary>
    /// Get current panel name
    /// </summary>
    public string GetCurrentPanel()
    {
        return currentPanel;
    }
    
    /// <summary>
    /// Close all panels
    /// </summary>
    public void CloseAllPanels()
    {
        foreach (var config in panelConfigs.Values)
        {
            if (config.gameObject != null)
            {
                config.gameObject.SetActive(false);
            }
        }
        
        navigationStack.Clear();
        currentPanel = GAME_STATE;
        
        Time.timeScale = 1f;
        EnablePlayerControl();
    }

    /// <summary>
    /// Reset to game state
    /// </summary>
    public void ResetToGame()
    {
        CloseAllPanels();
    }

    /// <summary>
    /// Fix all panel layers
    /// </summary>
    [ContextMenu("Fix All Panel Layers")]
    public void FixAllPanelLayers()
    {
        SetupCanvasSorting();
    }

    #endregion
}

/// <summary>
/// Panel names for the unified system
/// </summary>
public static class UnifiedUIPanelNames
{
    public const string MainMenu = "MainMenu";
    public const string Inventory = "Inventory";
    public const string QuestJournal = "QuestJournal";
    public const string Dialogue = "Dialogue";
    public const string DialogueHistory = "DialogueHistory";
    public const string PauseMenu = "PauseMenu";
    public const string SaveMenu = "SaveMenu";
    public const string Settings = "Settings";
    public const string AudioSettings = "AudioSettings";
    public const string Confirmation = "Confirmation";
    public const string Notification = "Notification";
}
