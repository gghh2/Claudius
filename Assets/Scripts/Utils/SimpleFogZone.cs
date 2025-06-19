using UnityEngine;

/// <summary>
/// Simple fog zone using RenderSettings
/// </summary>
public class SimpleFogZone : MonoBehaviour
{
    [Header("Fog Settings")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.5f, 0.5f, 0.6f, 1f);
    public FogMode fogMode = FogMode.Linear;
    
    [Header("Linear Fog")]
    public float fogStartDistance = 5f;
    public float fogEndDistance = 30f;
    
    [Header("Exponential Fog")]
    [Range(0f, 1f)]
    public float fogDensity = 0.05f;
    
    [Header("Transition")]
    public float transitionSpeed = 2f;
    
    // Original fog settings
    private bool originalFogEnabled;
    private Color originalFogColor;
    private FogMode originalFogMode;
    private float originalFogStart;
    private float originalFogEnd;
    private float originalFogDensity;
    
    // For smooth transitions
    private bool playerInZone = false;
    private float transitionProgress = 0f;
    
    void Start()
    {
        // Save original fog settings
        SaveOriginalSettings();
    }
    
    void SaveOriginalSettings()
    {
        originalFogEnabled = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;
        originalFogDensity = RenderSettings.fogDensity;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log("🌫️ Entered fog zone");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            Debug.Log("☀️ Exited fog zone");
        }
    }
    
    void Update()
    {
        // Update transition
        if (playerInZone)
        {
            transitionProgress = Mathf.MoveTowards(transitionProgress, 1f, transitionSpeed * Time.deltaTime);
        }
        else
        {
            transitionProgress = Mathf.MoveTowards(transitionProgress, 0f, transitionSpeed * Time.deltaTime);
        }
        
        // Apply fog settings
        ApplyFogSettings();
    }
    
    void ApplyFogSettings()
    {
        if (transitionProgress <= 0f)
        {
            // Fully outside - use original settings
            RenderSettings.fog = originalFogEnabled;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogStartDistance = originalFogStart;
            RenderSettings.fogEndDistance = originalFogEnd;
            RenderSettings.fogDensity = originalFogDensity;
        }
        else if (transitionProgress >= 1f)
        {
            // Fully inside - use zone settings
            RenderSettings.fog = enableFog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
            RenderSettings.fogDensity = fogDensity;
        }
        else
        {
            // In transition - interpolate
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(originalFogColor, fogColor, transitionProgress);
            RenderSettings.fogMode = fogMode; // Can't interpolate enum
            
            if (fogMode == FogMode.Linear)
            {
                RenderSettings.fogStartDistance = Mathf.Lerp(originalFogStart, fogStartDistance, transitionProgress);
                RenderSettings.fogEndDistance = Mathf.Lerp(originalFogEnd, fogEndDistance, transitionProgress);
            }
            else
            {
                RenderSettings.fogDensity = Mathf.Lerp(originalFogDensity, fogDensity, transitionProgress);
            }
        }
    }
    
    void OnDisable()
    {
        // Restore original settings when disabled
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogStartDistance = originalFogStart;
        RenderSettings.fogEndDistance = originalFogEnd;
        RenderSettings.fogDensity = originalFogDensity;
    }
    
    void OnDrawGizmos()
    {
        // Visualize the fog zone
        Gizmos.color = new Color(fogColor.r, fogColor.g, fogColor.b, 0.3f);
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
        }
    }
}