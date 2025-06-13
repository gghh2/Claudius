using UnityEngine;

/// <summary>
/// Ensures fog is enabled in builds
/// </summary>
public class FogBuildEnsurer : MonoBehaviour
{
    [Header("Fog Configuration")]
    public bool enableFogOnLoad = true;
    public FogMode fogMode = FogMode.Linear;
    public Color fogColor = new Color(0.7f, 0.8f, 0.9f, 1f);
    
    [Header("Linear Fog")]
    public float linearStart = 80f;
    public float linearEnd = 250f;
    
    [Header("Exponential Fog")]
    public float density = 0.02f;
    
    void Awake()
    {
        if (enableFogOnLoad)
        {
            ApplyFogSettings();
        }
    }
    
    void ApplyFogSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = fogColor;
        
        if (fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = linearStart;
            RenderSettings.fogEndDistance = linearEnd;
        }
        else
        {
            RenderSettings.fogDensity = density;
        }
    }
}
