using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Composant utilitaire pour customiser facilement les marqueurs de quête
/// </summary>
public class QuestMarkerCustomizer : MonoBehaviour
{
    [Header("Sprite Presets")]
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Sprite diamondSprite;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite exclamationSprite;
    
    [Header("Current Settings")]
    [SerializeField] private Sprite currentSprite;
    [SerializeField] private Vector2 currentSize = new Vector2(50f, 50f);
    
    [Header("Preview")]
    [SerializeField] private Image previewImage;
    
    void Start()
    {
        // Appliquer le sprite initial si défini
        if (currentSprite != null && QuestMarkerSystem.Instance != null)
        {
            QuestMarkerSystem.Instance.SetMarkerSprite(currentSprite);
            QuestMarkerSystem.Instance.SetCustomSpriteSize(currentSize);
        }
    }
    
    /// <summary>
    /// Change le sprite du marqueur
    /// </summary>
    public void SetMarkerSprite(Sprite sprite)
    {
        currentSprite = sprite;
        
        if (QuestMarkerSystem.Instance != null)
        {
            QuestMarkerSystem.Instance.SetMarkerSprite(sprite);
        }
        
        UpdatePreview();
    }
    
    /// <summary>
    /// Change la taille du marqueur
    /// </summary>
    public void SetMarkerSize(float size)
    {
        SetMarkerSize(new Vector2(size, size));
    }
    
    public void SetMarkerSize(Vector2 size)
    {
        currentSize = size;
        
        if (QuestMarkerSystem.Instance != null)
        {
            QuestMarkerSystem.Instance.SetCustomSpriteSize(size);
        }
        
        UpdatePreview();
    }
    
    // Méthodes pour les boutons UI
    public void UseDefaultSquare() => SetMarkerSprite(null);
    public void UseArrow() => TrySetSprite(arrowSprite);
    public void UseDiamond() => TrySetSprite(diamondSprite);
    public void UseCircle() => TrySetSprite(circleSprite);
    public void UseStar() => TrySetSprite(starSprite);
    public void UseExclamation() => TrySetSprite(exclamationSprite);
    
    private void TrySetSprite(Sprite sprite)
    {
        if (sprite != null)
            SetMarkerSprite(sprite);
    }
    
    /// <summary>
    /// Met à jour l'aperçu dans l'UI
    /// </summary>
    void UpdatePreview()
    {
        if (previewImage != null)
        {
            previewImage.sprite = currentSprite;
            previewImage.rectTransform.sizeDelta = currentSize;
            
            if (currentSprite == null)
            {
                // Afficher le carré jaune par défaut
                previewImage.color = Color.yellow;
            }
            else
            {
                previewImage.color = Color.white;
            }
        }
    }
    

    
    void OnValidate()
    {
        UpdatePreview();
    }
}
