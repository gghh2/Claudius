using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Centralized confirmation dialog manager
/// Works with UnifiedUIManager to show modal confirmation dialogs
/// </summary>
public class ConfirmationDialogManager : MonoBehaviour
{
    #region Singleton
    private static ConfirmationDialogManager instance;
    public static ConfirmationDialogManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ConfirmationDialogManager>();
            }
            return instance;
        }
    }
    #endregion
    
    [Header("Dialog Elements")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private TextMeshProUGUI cancelButtonText;
    
    // Current dialog state
    private Action onConfirmAction;
    private Action onCancelAction;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Setup button listeners
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        // Hide dialog at start
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show a confirmation dialog with custom parameters
    /// </summary>
    public void ShowDialog(
        string message, 
        Action onConfirm, 
        Action onCancel = null,
        string title = "Confirmation",
        string confirmText = "Yes",
        string cancelText = "No")
    {
        // Set dialog content
        if (messageText != null)
            messageText.text = message;
            
        if (titleText != null)
            titleText.text = title;
            
        if (confirmButtonText != null)
            confirmButtonText.text = confirmText;
            
        if (cancelButtonText != null)
            cancelButtonText.text = cancelText;
        
        // Store callbacks
        onConfirmAction = onConfirm;
        onCancelAction = onCancel;
        
        // FORCE DIRECT ACTIVATION - Skip UnifiedUIManager if it doesn't work
        if (dialogPanel != null)
        {
            Debug.Log("[ConfirmationDialog] Force activating dialog panel directly");
            dialogPanel.SetActive(true);
            dialogPanel.transform.SetAsLastSibling();
            
            // Ensure it has proper canvas sorting
            Canvas canvas = dialogPanel.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = dialogPanel.AddComponent<Canvas>();
                dialogPanel.AddComponent<GraphicRaycaster>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100; // High value to be on top
        }
    }
    
    /// <summary>
    /// Show a simple yes/no dialog
    /// </summary>
    public void ShowYesNoDialog(string message, Action onYes, Action onNo = null)
    {
        ShowDialog(message, onYes, onNo, "Confirmation", "Yes", "No");
    }
    
    /// <summary>
    /// Show a delete confirmation dialog
    /// </summary>
    public void ShowDeleteDialog(string itemName, Action onDelete)
    {
        ShowDialog(
            $"Are you sure you want to delete {itemName}?", 
            onDelete, 
            null,
            "Delete Confirmation",
            "Delete",
            "Cancel"
        );
    }
    
    /// <summary>
    /// Show an overwrite confirmation dialog
    /// </summary>
    public void ShowOverwriteDialog(string itemName, Action onOverwrite)
    {
        ShowDialog(
            $"Are you sure you want to overwrite {itemName}?", 
            onOverwrite,
            null,
            "Overwrite Confirmation",
            "Overwrite",
            "Cancel"
        );
    }
    
    void OnConfirmClicked()
    {
        // Close dialog
        CloseDialog();
        
        // Execute confirm action
        onConfirmAction?.Invoke();
        
        // Clear callbacks
        ClearCallbacks();
    }
    
    void OnCancelClicked()
    {
        // Close dialog
        CloseDialog();
        
        // Execute cancel action if provided
        onCancelAction?.Invoke();
        
        // Clear callbacks
        ClearCallbacks();
    }
    
    void CloseDialog()
    {
        // Direct close - simpler and more reliable
        if (dialogPanel != null)
        {
            Debug.Log("[ConfirmationDialog] Closing dialog panel");
            dialogPanel.SetActive(false);
        }
    }
    
    void ClearCallbacks()
    {
        onConfirmAction = null;
        onCancelAction = null;
    }
    
    /// <summary>
    /// Force close the dialog without executing any callbacks
    /// </summary>
    public void ForceClose()
    {
        ClearCallbacks();
        CloseDialog();
    }
    
    void OnDestroy()
    {
        // Clean up listeners
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}
