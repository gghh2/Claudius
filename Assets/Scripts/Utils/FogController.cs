using UnityEngine;

/// <summary>
/// Simple fog controller for color and basic settings
/// </summary>
public class FogController : MonoBehaviour
{
    [Header("Fog Settings")]
    public bool enableFog = true;
    public FogMode fogMode = FogMode.Linear;
    
    [Header("Linear Fog")]
    public float linearFogStart = 20f;
    public float linearFogEnd = 80f;
    
    [Header("Exponential Fog")]
    [Range(0.001f, 0.1f)]
    public float fogDensity = 0.02f;
    
    [Header("Color")]
    public Color fogColor = new Color(0.7f, 0.8f, 0.9f, 1f);
    
    void Start()
    {
        // Skip if OrthographicFogAdapter is present
        if (GetComponent<OrthographicFogAdapter>() != null)
        {
            enabled = false;
            return;
        }
        
        ApplyFogSettings();
    }
    
    void ApplyFogSettings()
    {
        RenderSettings.fog = enableFog;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = fogColor;
        
        if (fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = linearFogStart;
            RenderSettings.fogEndDistance = linearFogEnd;
        }
        else
        {
            RenderSettings.fogDensity = fogDensity;
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && GetComponent<OrthographicFogAdapter>() == null)
        {
            ApplyFogSettings();
        }
    }
}
