using UnityEngine;

/// <summary>
/// Script de test minimal pour vérifier la transparence en build
/// </summary>
public class TransparencyTest : MonoBehaviour
{
    public Renderer targetRenderer;
    public float alphaValue = 0.5f;
    
    void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
            
        if (targetRenderer != null)
        {
            Material mat = targetRenderer.material;
            Color color = mat.color;
            color.a = alphaValue;
            mat.color = color;
            
            Debug.Log($"[TransparencyTest] Set alpha to {alphaValue} on {targetRenderer.name}");
        }
    }
}
