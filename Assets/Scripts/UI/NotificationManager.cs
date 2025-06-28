using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Centralized notification system
/// Works with UnifiedUIManager for proper layering
/// </summary>
public class NotificationManager : MonoBehaviour
{
    #region Singleton
    private static NotificationManager instance;
    public static NotificationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NotificationManager>();
                if (instance == null)
                {
                    Debug.LogError("[NotificationManager] No instance found in scene!");
                }
            }
            return instance;
        }
    }
    #endregion
    
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Image backgroundImage;
    
    [Header("Settings")]
    [SerializeField] private float defaultDuration = 2f;
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float fadeOutTime = 0.3f;
    
    [Header("Notification Types")]
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    [SerializeField] private Color errorColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color infoColor = new Color(0.2f, 0.5f, 0.8f, 0.9f);
    [SerializeField] private Color warningColor = new Color(0.8f, 0.6f, 0.2f, 0.9f);
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private Coroutine currentNotification;
    private bool isInitialized = false;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            ValidateReferences();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Double check initialization
        if (!isInitialized)
        {
            ValidateReferences();
        }
        
        // Hide notification at start
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Validate all required references
    /// </summary>
    private void ValidateReferences()
    {
        bool hasErrors = false;
        
        // Check notification panel
        if (notificationPanel == null)
        {
            // Try to find it by name (including inactive objects)
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allTransforms)
            {
                if (t.name == "NotificationPanel" && t.gameObject.scene.name != null)
                {
                    notificationPanel = t.gameObject;
                    Debug.LogWarning("[NotificationManager] NotificationPanel was not assigned, found it in scene (was inactive)");
                    break;
                }
            }
            
            // Alternative: try to find through Canvas
            if (notificationPanel == null)
            {
                Canvas mainCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
                if (mainCanvas != null)
                {
                    Transform panel = mainCanvas.transform.Find("NotificationPanel");
                    if (panel != null)
                    {
                        notificationPanel = panel.gameObject;
                        Debug.LogWarning("[NotificationManager] NotificationPanel found through Canvas hierarchy");
                    }
                }
            }
            
            if (notificationPanel == null)
            {
                Debug.LogError("[NotificationManager] NotificationPanel is not assigned and couldn't be found! Please assign it manually in the inspector.");
                hasErrors = true;
            }
        }
        
        // Check notification text
        if (notificationText == null && notificationPanel != null)
        {
            notificationText = notificationPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (notificationText == null)
            {
                Debug.LogError("[NotificationManager] No TextMeshProUGUI found in NotificationPanel!");
                hasErrors = true;
            }
            else
            {
                Debug.LogWarning("[NotificationManager] NotificationText was not assigned, found it in children");
            }
        }
        
        // Check background image
        if (backgroundImage == null && notificationPanel != null)
        {
            backgroundImage = notificationPanel.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = notificationPanel.GetComponentInChildren<Image>();
            }
            
            if (backgroundImage == null)
            {
                Debug.LogWarning("[NotificationManager] No background Image found, notifications will work without background color changes");
            }
        }
        
        isInitialized = !hasErrors;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NotificationManager] Initialization status: {(isInitialized ? "SUCCESS" : "FAILED")}");
            Debug.Log($"[NotificationManager] Panel: {(notificationPanel != null ? "OK" : "MISSING")}");
            Debug.Log($"[NotificationManager] Text: {(notificationText != null ? "OK" : "MISSING")}");
            Debug.Log($"[NotificationManager] Background: {(backgroundImage != null ? "OK" : "MISSING")}");
        }
    }
    
    /// <summary>
    /// Show a notification with custom settings
    /// </summary>
    public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = -1)
    {
        if (!isInitialized)
        {
            Debug.LogError("[NotificationManager] System not properly initialized! Check references.");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NotificationManager] ShowNotification called: '{message}' (type: {type})");
        }
        
        // Cancel any existing notification
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
        }
        
        // Set duration
        if (duration < 0)
        {
            duration = defaultDuration;
        }
        
        // Start new notification
        currentNotification = StartCoroutine(ShowNotificationCoroutine(message, type, duration));
    }
    
    IEnumerator ShowNotificationCoroutine(string message, NotificationType type, float duration)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[NotificationManager] Starting notification coroutine");
        }
        
        // Set message
        notificationText.text = message;
        
        // Set color based on type
        Color targetColor = GetColorForType(type);
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }
        
        // Activate the panel
        notificationPanel.SetActive(true);
        
        // Ensure it's on top using UnifiedUIManager if available
        if (UnifiedUIManager.Instance != null)
        {
            // Force notification to be on top
            Canvas canvas = notificationPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Canvas notifCanvas = notificationPanel.GetComponent<Canvas>();
                if (notifCanvas == null)
                {
                    notifCanvas = notificationPanel.AddComponent<Canvas>();
                    notificationPanel.AddComponent<GraphicRaycaster>();
                }
                notifCanvas.overrideSorting = true;
                notifCanvas.sortingOrder = 100; // Very high to be on top
            }
        }
        else
        {
            // Fallback: just set as last sibling
            notificationPanel.transform.SetAsLastSibling();
        }
        
        // Force canvas update
        Canvas.ForceUpdateCanvases();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NotificationManager] Panel activated, active state: {notificationPanel.activeSelf}");
        }
        
        // Fade in
        yield return FadePanel(0f, 1f, fadeInTime);
        
        // Wait
        yield return new WaitForSecondsRealtime(duration);
        
        // Fade out
        yield return FadePanel(1f, 0f, fadeOutTime);
        
        // Hide panel
        notificationPanel.SetActive(false);
        
        if (enableDebugLogs)
        {
            Debug.Log("[NotificationManager] Notification hidden");
        }
        
        currentNotification = null;
    }
    
    IEnumerator FadePanel(float from, float to, float duration)
    {
        // Skip fade if duration is 0
        if (duration <= 0)
        {
            yield break;
        }
        
        CanvasGroup canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = notificationPanel.AddComponent<CanvasGroup>();
        }
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        
        canvasGroup.alpha = to;
    }
    
    Color GetColorForType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success:
                return successColor;
            case NotificationType.Error:
                return errorColor;
            case NotificationType.Warning:
                return warningColor;
            case NotificationType.Info:
            default:
                return infoColor;
        }
    }
    
    /// <summary>
    /// Quick success notification
    /// </summary>
    public void ShowSuccess(string message, float duration = -1)
    {
        ShowNotification(message, NotificationType.Success, duration);
    }
    
    /// <summary>
    /// Quick error notification
    /// </summary>
    public void ShowError(string message, float duration = -1)
    {
        ShowNotification(message, NotificationType.Error, duration);
    }
    
    /// <summary>
    /// Quick info notification
    /// </summary>
    public void ShowInfo(string message, float duration = -1)
    {
        ShowNotification(message, NotificationType.Info, duration);
    }
    
    /// <summary>
    /// Quick warning notification
    /// </summary>
    public void ShowWarning(string message, float duration = -1)
    {
        ShowNotification(message, NotificationType.Warning, duration);
    }
    
    /// <summary>
    /// Hide current notification immediately
    /// </summary>
    public void HideNotification()
    {
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
            currentNotification = null;
        }
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Test notification system
    /// </summary>
    [ContextMenu("Test Success Notification")]
    public void TestSuccessNotification()
    {
        ShowSuccess("Test: Success notification!");
    }
    
    [ContextMenu("Test Error Notification")]
    public void TestErrorNotification()
    {
        ShowError("Test: Error notification!");
    }
    
    [ContextMenu("Test Info Notification")]
    public void TestInfoNotification()
    {
        ShowInfo("Test: Info notification!");
    }
    
    [ContextMenu("Test Warning Notification")]
    public void TestWarningNotification()
    {
        ShowWarning("Test: Warning notification!");
    }
    
    /// <summary>
    /// Helper method to find and assign UI references
    /// </summary>
    [ContextMenu("Auto-Assign References")]
    public void AutoAssignReferences()
    {
        Debug.Log("[NotificationManager] Starting auto-assignment...");
        
        // Find NotificationPanel
        if (notificationPanel == null)
        {
            // Search in Canvas
            Canvas mainCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            if (mainCanvas != null)
            {
                Transform panel = mainCanvas.transform.Find("NotificationPanel");
                if (panel != null)
                {
                    notificationPanel = panel.gameObject;
                    Debug.Log("[NotificationManager] NotificationPanel found and assigned!");
                }
            }
            
            // Search everywhere including inactive
            if (notificationPanel == null)
            {
                NotificationPanel[] allPanels = Resources.FindObjectsOfTypeAll<NotificationPanel>();
                if (allPanels.Length > 0)
                {
                    notificationPanel = allPanels[0].gameObject;
                    Debug.Log("[NotificationManager] NotificationPanel found in resources!");
                }
            }
        }
        
        // Find Text component
        if (notificationText == null && notificationPanel != null)
        {
            notificationText = notificationPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (notificationText != null)
            {
                Debug.Log("[NotificationManager] NotificationText found and assigned!");
            }
        }
        
        // Find Background Image
        if (backgroundImage == null && notificationPanel != null)
        {
            backgroundImage = notificationPanel.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = notificationPanel.GetComponentInChildren<Image>(true);
            }
            if (backgroundImage != null)
            {
                Debug.Log("[NotificationManager] Background Image found and assigned!");
            }
        }
        
        Debug.Log("[NotificationManager] Auto-assignment complete. Please check the references in the inspector.");
    }
}

public enum NotificationType
{
    Info,
    Success,
    Error,
    Warning
}