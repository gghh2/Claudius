using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component to setup UI navigation buttons easily in the Unity Editor
/// Attach this to any button that needs to navigate between UI panels
/// </summary>
public class UINavigationButton : MonoBehaviour
{
    [Header("Navigation Configuration")]
    [Tooltip("Type of navigation action")]
    [SerializeField] private NavigationType navigationType = NavigationType.NavigateTo;
    
    [Tooltip("Target panel (for NavigateTo)")]
    [SerializeField] private string targetPanel = "";
    
    [Tooltip("Show dropdown of available panels")]
    [SerializeField] private PanelChoice panelChoice = PanelChoice.Custom;
    
    public enum NavigationType
    {
        NavigateTo,
        NavigateBack,
        CloseCurrentPanel
    }
    
    public enum PanelChoice
    {
        Custom,
        PauseMenu,
        Settings,
        SaveMenu,
        Inventory,
        QuestJournal,
        Dialogue,
        DialogueHistory
    }
    
    private Button button;
    private float lastClickTime = 0f;
    private const float CLICK_COOLDOWN = 0.5f;
    
    void Start()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[UINavigationButton] No Button component found on {gameObject.name}");
            return;
        }
        
        // Setup button click listener
        button.onClick.AddListener(OnButtonClick);
        
        // Auto-set target panel from enum if not custom
        if (panelChoice != PanelChoice.Custom)
        {
            targetPanel = GetPanelNameFromChoice(panelChoice);
        }
    }
    
    void OnButtonClick()
    {
        // Prevent double clicks
        if (Time.unscaledTime - lastClickTime < CLICK_COOLDOWN)
        {
            return;
        }
        lastClickTime = Time.unscaledTime;
        
        if (UnifiedUIManager.Instance == null)
        {
            Debug.LogError("[UINavigationButton] UnifiedUIManager not found!");
            return;
        }
        
        switch (navigationType)
        {
            case NavigationType.NavigateTo:
                if (!string.IsNullOrEmpty(targetPanel))
                {
                    UnifiedUIManager.Instance.NavigateTo(targetPanel);
                }
                break;
                
            case NavigationType.NavigateBack:
                UnifiedUIManager.Instance.NavigateBack();
                break;
                
            case NavigationType.CloseCurrentPanel:
                UnifiedUIManager.Instance.NavigateBack();
                break;
        }
    }
    
    string GetPanelNameFromChoice(PanelChoice choice)
    {
        switch (choice)
        {
            case PanelChoice.PauseMenu: return UnifiedUIPanelNames.PauseMenu;
            case PanelChoice.Settings: return UnifiedUIPanelNames.Settings;
            case PanelChoice.SaveMenu: return UnifiedUIPanelNames.SaveMenu;
            case PanelChoice.Inventory: return UnifiedUIPanelNames.Inventory;
            case PanelChoice.QuestJournal: return UnifiedUIPanelNames.QuestJournal;
            case PanelChoice.Dialogue: return UnifiedUIPanelNames.Dialogue;
            case PanelChoice.DialogueHistory: return UnifiedUIPanelNames.DialogueHistory;
            default: return "";
        }
    }
    
    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
    
    #if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-update target panel when choice changes in editor
        if (panelChoice != PanelChoice.Custom)
        {
            targetPanel = GetPanelNameFromChoice(panelChoice);
        }
    }
    #endif
}
