using UnityEngine;
using TMPro;

/// <summary>
/// Utilitaire pour corriger automatiquement les styles TextMeshPro
/// Enlève le gras et remet l'épaisseur à 0
/// </summary>
[ExecuteInEditMode]
public class TextMeshProStyleFixer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Appliquer les corrections au démarrage")]
    public bool applyOnStart = true;
    
    [Tooltip("Appliquer aux enfants aussi")]
    public bool applyToChildren = true;
    
    [Tooltip("Forcer la mise à jour en éditeur")]
    public bool forceUpdateInEditor = false;
    
    void Start()
    {
        if (applyOnStart)
        {
            ApplyTextStyleFixes();
        }
    }
    
    void OnValidate()
    {
        if (forceUpdateInEditor && !Application.isPlaying)
        {
            ApplyTextStyleFixes();
            forceUpdateInEditor = false;
        }
    }
    
    [ContextMenu("Fix Text Styles")]
    public void ApplyTextStyleFixes()
    {
        TextMeshProUGUI[] textComponents;
        
        if (applyToChildren)
        {
            textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
        }
        else
        {
            textComponents = GetComponents<TextMeshProUGUI>();
        }
        
        int fixedCount = 0;
        
        foreach (var tmp in textComponents)
        {
            bool changed = false;
            
            // Enlève le style Bold
            if ((tmp.fontStyle & FontStyles.Bold) != 0)
            {
                tmp.fontStyle &= ~FontStyles.Bold;
                changed = true;
            }
            
            // Remet l'épaisseur à 0 (utilise la réflexion car pas toujours exposé)
            // Note: Dans les versions récentes de TextMeshPro, c'est tmp.fontMaterial
            if (tmp.fontSharedMaterial != null)
            {
                // Clone le matériau pour ne pas affecter l'original
                if (tmp.fontSharedMaterial.HasProperty("_OutlineWidth"))
                {
                    float currentThickness = tmp.fontSharedMaterial.GetFloat("_OutlineWidth");
                    if (currentThickness != 0)
                    {
                        Material newMat = new Material(tmp.fontSharedMaterial);
                        newMat.SetFloat("_OutlineWidth", 0);
                        tmp.fontMaterial = newMat;
                        changed = true;
                    }
                }
            }
            
            if (changed)
            {
                fixedCount++;
                Debug.Log($"Fixed text style for: {tmp.gameObject.name}", tmp.gameObject);
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"TextMeshProStyleFixer: Fixed {fixedCount} text components");
        }
        else
        {
            Debug.Log("TextMeshProStyleFixer: No text components needed fixing");
        }
    }
    
    /// <summary>
    /// Méthode statique pour appliquer les corrections à un GameObject spécifique
    /// </summary>
    public static void FixTextStyles(GameObject target, bool includeChildren = true)
    {
        if (target == null) return;
        
        TextMeshProUGUI[] textComponents;
        
        if (includeChildren)
        {
            textComponents = target.GetComponentsInChildren<TextMeshProUGUI>(true);
        }
        else
        {
            textComponents = target.GetComponents<TextMeshProUGUI>();
        }
        
        foreach (var tmp in textComponents)
        {
            // Enlève le Bold
            tmp.fontStyle &= ~FontStyles.Bold;
            
            // Pour l'outline/thickness, généralement on utilise le component Outline
            var outline = tmp.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }
}
