using UnityEngine;

/// <summary>
/// Adapts linear fog settings based on orthographic camera zoom level
/// </summary>
[RequireComponent(typeof(Camera))]
public class OrthographicFogAdapter : MonoBehaviour
{
    [Header("Fog Adaptation")]
    public bool enableFogAdaptation = true;
    
    [Header("Calibration Points")]
    [Tooltip("Point 1: Zoomed out (Size 20)")]
    public CalibrationPoint calibrationPoint1 = new CalibrationPoint(20f, 20f, 250f);
    
    [Tooltip("Point 2: Normal view (Size 10) - values stay constant below this")]
    public CalibrationPoint calibrationPoint2 = new CalibrationPoint(10f, 80f, 250f);
    
    [Header("Settings")]
    [Tooltip("Fog color")]
    public Color fogColor = new Color(0.7f, 0.8f, 0.9f, 1f);
    
    [Tooltip("Use smooth interpolation")]
    public bool useSmoothInterpolation = true;
    
    [System.Serializable]
    public class CalibrationPoint
    {
        public float cameraSize;
        public float fogStart;
        public float fogEnd;
        
        public CalibrationPoint(float size, float start, float end)
        {
            cameraSize = size;
            fogStart = start;
            fogEnd = end;
        }
    }
    
    private Camera cam;
    
    void Awake()
    {
        // Force fog settings early
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
    }
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (!cam.orthographic)
        {
            enabled = false;
            return;
        }
        
        // Force initial fog settings
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = calibrationPoint2.fogStart;
        RenderSettings.fogEndDistance = calibrationPoint2.fogEnd;
        
        UpdateFogSettings();
    }
    
    void Update()
    {
        if (enableFogAdaptation && RenderSettings.fog)
        {
            UpdateFogSettings();
        }
    }
    
    void UpdateFogSettings()
    {
        float currentSize = cam.orthographicSize;
        
        // Calculate fog values
        float fogStart = CalculateFogValue(currentSize, true);
        float fogEnd = CalculateFogValue(currentSize, false);
        
        // Apply settings
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        RenderSettings.fogColor = fogColor;
    }
    
    float CalculateFogValue(float cameraSize, bool isStartDistance)
    {
        float value1 = isStartDistance ? calibrationPoint1.fogStart : calibrationPoint1.fogEnd;
        float value2 = isStartDistance ? calibrationPoint2.fogStart : calibrationPoint2.fogEnd;
        
        // Clamp below point 2
        if (cameraSize <= calibrationPoint2.cameraSize)
        {
            return value2;
        }
        
        // Clamp above point 1
        if (cameraSize >= calibrationPoint1.cameraSize)
        {
            return value1;
        }
        
        // Interpolate between points
        float t = Mathf.InverseLerp(calibrationPoint1.cameraSize, calibrationPoint2.cameraSize, cameraSize);
        
        if (useSmoothInterpolation)
        {
            t = t * t * (3f - 2f * t);
        }
        
        return Mathf.Lerp(value1, value2, t);
    }
}
