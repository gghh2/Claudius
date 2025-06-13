using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Améliore drastiquement la qualité des ombres dans Built-in RP
/// </summary>
[ExecuteInEditMode]
public class ShadowQualityEnhancer : MonoBehaviour
{
    [Header("Shadow Quality")]
    [Range(512, 8192)]
    public int shadowResolution = 4096;
    
    [Range(10f, 300f)]
    public float shadowDistance = 100f;
    
    [Range(0f, 1f)]
    public float shadowStrength = 0.85f;
    
    [Header("Cascade Settings")]
    public bool useFourCascades = true;
    [Range(0.01f, 0.3f)]
    public float cascade1Split = 0.05f;
    [Range(0.05f, 0.5f)]
    public float cascade2Split = 0.15f;
    [Range(0.1f, 0.8f)]
    public float cascade3Split = 0.3f;
    
    [Header("Light Settings")]
    public Light directionalLight;
    [Range(0f, 0.2f)]
    public float shadowBias = 0.05f;
    [Range(0f, 1f)]
    public float shadowNormalBias = 0.4f;
    
    [Header("Advanced")]
    public bool enableShadowSoftening = true;
    public bool improveCloseUpShadows = true;
    
    [Header("Debug")]
    public bool showCurrentSettings = false;
    
    void Start()
    {
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
        }
        
        ApplySettings();
    }
    
    void OnValidate()
    {
        ApplySettings();
    }
    
    [ContextMenu("Apply Best Settings")]
    public void ApplyBestSettings()
    {
        // Meilleurs paramètres pour des ombres de qualité
        shadowResolution = 4096;
        shadowDistance = 100f;
        shadowStrength = 0.85f;
        useFourCascades = true;
        cascade1Split = 0.05f;
        cascade2Split = 0.15f;
        cascade3Split = 0.35f;
        shadowBias = 0.05f;
        shadowNormalBias = 0.4f;
        
        ApplySettings();
        Debug.Log("✅ Meilleurs paramètres d'ombres appliqués !");
    }
    
    [ContextMenu("Apply Performance Settings")]
    public void ApplyPerformanceSettings()
    {
        // Paramètres équilibrés pour la performance
        shadowResolution = 2048;
        shadowDistance = 50f;
        shadowStrength = 0.75f;
        useFourCascades = false; // 2 cascades
        shadowBias = 0.1f;
        shadowNormalBias = 0.5f;
        
        ApplySettings();
        Debug.Log("⚡ Paramètres performance appliqués !");
    }
    
    void ApplySettings()
    {
        // Quality Settings globaux
        QualitySettings.shadowResolution = GetShadowResolution();
        QualitySettings.shadowDistance = shadowDistance;
        QualitySettings.shadowProjection = ShadowProjection.CloseFit;
        QualitySettings.shadows = ShadowQuality.All;
        
        // Cascades
        if (useFourCascades)
        {
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowCascade4Split = new Vector3(cascade1Split, cascade2Split, cascade3Split);
        }
        else
        {
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowCascade2Split = cascade1Split;
        }
        
        // Light settings
        if (directionalLight != null)
        {
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.shadowStrength = shadowStrength;
            directionalLight.shadowBias = shadowBias;
            directionalLight.shadowNormalBias = shadowNormalBias;
            directionalLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
            
            if (improveCloseUpShadows)
            {
                directionalLight.shadowNearPlane = 0.1f;
            }
        }
        
        if (showCurrentSettings)
        {
            ShowDebugInfo();
        }
    }
    
    ShadowResolution GetShadowResolution()
    {
        if (shadowResolution >= 4096) return ShadowResolution.VeryHigh;
        if (shadowResolution >= 2048) return ShadowResolution.High;
        if (shadowResolution >= 1024) return ShadowResolution.Medium;
        return ShadowResolution.Low;
    }
    
    void ShowDebugInfo()
    {
        Debug.Log($"=== 🌑 SHADOW SETTINGS ===");
        Debug.Log($"Resolution: {shadowResolution}");
        Debug.Log($"Distance: {shadowDistance}");
        Debug.Log($"Cascades: {(useFourCascades ? "4" : "2")}");
        Debug.Log($"Light Shadows: {directionalLight?.shadows}");
        Debug.Log($"========================");
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualise les distances de cascade
        if (!useFourCascades) return;
        
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, shadowDistance * cascade1Split);
        
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, shadowDistance * cascade2Split);
        
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, shadowDistance * cascade3Split);
        
        Gizmos.color = new Color(0, 0, 1, 0.1f);
        Gizmos.DrawWireSphere(transform.position, shadowDistance);
    }
}
