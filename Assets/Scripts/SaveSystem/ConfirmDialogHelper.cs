using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper to ensure confirmation dialogs appear on top
/// Add this to your ConfirmDialog GameObject
/// </summary>
public class ConfirmDialogHelper : MonoBehaviour
{
    [Header("Optional Canvas Override")]
    [SerializeField] private int sortingOrderOffset = 100;
    
    private Canvas dialogCanvas;
    private int originalSortingOrder;
    
    void Awake()
    {
        // Try to get or add a Canvas component
        dialogCanvas = GetComponent<Canvas>();
        if (dialogCanvas == null)
        {
            dialogCanvas = gameObject.AddComponent<Canvas>();
            dialogCanvas.overrideSorting = true;
        }
        
        // Also need a GraphicRaycaster for UI interaction
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
        
        originalSortingOrder = dialogCanvas.sortingOrder;
    }
    
    void OnEnable()
    {
        // When dialog is shown, increase sorting order
        if (dialogCanvas != null && dialogCanvas.overrideSorting)
        {
            dialogCanvas.sortingOrder = originalSortingOrder + sortingOrderOffset;
        }
        
        // Also move to end of hierarchy
        transform.SetAsLastSibling();
    }
    
    void OnDisable()
    {
        // Reset sorting order when hidden
        if (dialogCanvas != null && dialogCanvas.overrideSorting)
        {
            dialogCanvas.sortingOrder = originalSortingOrder;
        }
    }
    
    [ContextMenu("Force To Top")]
    public void ForceToTop()
    {
        transform.SetAsLastSibling();
        
        if (dialogCanvas != null)
        {
            dialogCanvas.overrideSorting = true;
            dialogCanvas.sortingOrder = 9999;
        }
    }
}
