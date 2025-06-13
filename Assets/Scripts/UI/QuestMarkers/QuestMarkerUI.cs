using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual quest marker UI element
/// </summary>
public class QuestMarkerUI : MonoBehaviour
{
    [Header("Components")]
    public Image arrowImage;
    public TextMeshProUGUI distanceText;
    public CanvasGroup canvasGroup;
    
    private float baseSize;
    
    void Awake()
    {
        // Get components if not assigned
        if (arrowImage == null)
            arrowImage = GetComponent<Image>();
        
        if (distanceText == null)
            distanceText = GetComponentInChildren<TextMeshProUGUI>();
            
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
            baseSize = rect.sizeDelta.x;
    }
    
    public void SetMarkerColor(Color markerColor)
    {
        // Set arrow color
        if (arrowImage != null)
            arrowImage.color = markerColor;
    }
    
    public void UpdateDistance(float distance)
    {
        if (distanceText != null)
        {
            distanceText.text = $"{Mathf.RoundToInt(distance)}m";
            distanceText.color = Color.white;
        }
    }
    
    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }
    
    public void AnimatePulse(float scale)
    {
        transform.localScale = Vector3.one * scale;
    }
}
