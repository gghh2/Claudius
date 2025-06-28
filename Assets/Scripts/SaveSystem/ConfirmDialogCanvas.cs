using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Force ConfirmDialog to appear on top by creating a separate Canvas
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class ConfirmDialogCanvas : MonoBehaviour
{
    private Canvas dialogCanvas;
    private Canvas parentCanvas;
    
    void Awake()
    {
        // Get or add Canvas component
        dialogCanvas = GetComponent<Canvas>();
        if (dialogCanvas == null)
        {
            dialogCanvas = gameObject.AddComponent<Canvas>();
        }
        
        // Get or add GraphicRaycaster
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
        
        // Find parent canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas != dialogCanvas)
        {
            // Setup as overlay canvas
            dialogCanvas.overrideSorting = true;
            dialogCanvas.sortingOrder = parentCanvas.sortingOrder + 100;
        }
    }
    
    void OnEnable()
    {
        // Ensure we're on top when shown
        if (dialogCanvas != null && dialogCanvas.overrideSorting)
        {
            // Set high sorting order
            if (parentCanvas != null)
            {
                dialogCanvas.sortingOrder = parentCanvas.sortingOrder + 100;
            }
            else
            {
                dialogCanvas.sortingOrder = 999;
            }
        }
        
        // Also move to last sibling
        transform.SetAsLastSibling();
    }
}
