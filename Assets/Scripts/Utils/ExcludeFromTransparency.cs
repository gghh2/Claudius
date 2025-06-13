using UnityEngine;

/// <summary>
/// Composant pour exclure un GameObject du système de transparence de la caméra
/// Utile pour les NPCs, compagnons et autres objets qui ne doivent jamais devenir transparents
/// </summary>
public class ExcludeFromTransparency : MonoBehaviour
{
    [Header("Exclusion Settings")]
    [Tooltip("Exclut cet objet ET tous ses enfants de la transparence")]
    public bool excludeChildren = true;
    
    [Tooltip("Raison de l'exclusion (pour debug)")]
    public string exclusionReason = "NPC/Companion";
    
    [Header("Visual Indicator (Editor Only)")]
    [Tooltip("Couleur du gizmo dans l'éditeur")]
    public Color gizmoColor = Color.green;
    
    /// <summary>
    /// Vérifie si un GameObject ou ses parents ont ce composant
    /// </summary>
    public static bool IsExcluded(GameObject obj)
    {
        return obj.GetComponentInParent<ExcludeFromTransparency>() != null;
    }
    
    // Gizmo pour visualiser les objets exclus dans l'éditeur
    void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        
        // Dessine une icône au-dessus de l'objet
        Vector3 iconPos = transform.position + Vector3.up * 2f;
        Gizmos.DrawWireCube(iconPos, Vector3.one * 0.5f);
        Gizmos.DrawLine(transform.position, iconPos);
        
        // Si exclu les enfants, dessine une sphère englobante
        if (excludeChildren)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                {
                    combinedBounds.Encapsulate(r.bounds);
                }
                
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
                Gizmos.DrawCube(combinedBounds.center, combinedBounds.size);
            }
        }
    }
}
