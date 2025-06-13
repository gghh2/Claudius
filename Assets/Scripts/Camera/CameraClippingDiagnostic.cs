using UnityEngine;

/// <summary>
/// Diagnostique et corrige les problèmes de clipping de la caméra
/// </summary>
public class CameraClippingDiagnostic : MonoBehaviour
{
    private Camera cam;
    
    [Header("Diagnostic Info")]
    [SerializeField] private bool showDiagnostic = true;
    [SerializeField] private float nearPlane;
    [SerializeField] private float farPlane;
    [SerializeField] private bool isOrthographic;
    [SerializeField] private float orthographicSize;
    [SerializeField] private Vector3 cameraPosition;
    
    [Header("Auto Fix")]
    [Tooltip("Corrige automatiquement les valeurs problématiques")]
    public bool autoFix = true;
    
    [Tooltip("Distance minimale recommandée pour le near plane")]
    public float recommendedNearPlane = 0.3f;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        DiagnoseCamera();
    }
    
    void Update()
    {
        if (showDiagnostic)
        {
            UpdateDiagnosticInfo();
        }
    }
    
    void UpdateDiagnosticInfo()
    {
        nearPlane = cam.nearClipPlane;
        farPlane = cam.farClipPlane;
        isOrthographic = cam.orthographic;
        orthographicSize = cam.orthographicSize;
        cameraPosition = transform.position;
    }
    
    void DiagnoseCamera()
    {
        Debug.Log("=== DIAGNOSTIC CAMÉRA ===");
        Debug.Log($"Type: {(cam.orthographic ? "Orthographique" : "Perspective")}");
        Debug.Log($"Near Plane: {cam.nearClipPlane}");
        Debug.Log($"Far Plane: {cam.farClipPlane}");
        Debug.Log($"Position: {transform.position}");
        
        // Détecte les problèmes
        if (cam.nearClipPlane < 0 && !cam.orthographic)
        {
            Debug.LogError("❌ PROBLÈME: Near plane négatif sur caméra perspective!");
            Debug.Log("Cela cause des problèmes de rendu et de brouillard.");
            
            if (autoFix)
            {
                cam.nearClipPlane = recommendedNearPlane;
                Debug.Log($"✅ Near plane corrigé à {recommendedNearPlane}");
            }
        }
        
        if (cam.orthographic && cam.nearClipPlane < -1000)
        {
            Debug.LogWarning("⚠️ Near plane très négatif même pour une caméra orthographique");
            
            if (autoFix)
            {
                cam.nearClipPlane = -10f;
                Debug.Log("✅ Near plane ajusté à -10 pour caméra orthographique");
            }
        }
        
        // Vérifie la distance caméra-terrain
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity))
        {
            float distanceToGround = hit.distance;
            Debug.Log($"Distance au sol: {distanceToGround:F2}m");
            
            if (distanceToGround < 5f && !cam.orthographic)
            {
                Debug.LogWarning("⚠️ Caméra très proche du sol, risque de clipping");
            }
        }
    }
    
    [ContextMenu("Run Diagnostic")]
    public void RunDiagnostic()
    {
        DiagnoseCamera();
    }
    
    [ContextMenu("Fix Camera Settings")]
    public void FixCameraSettings()
    {
        if (cam == null) cam = GetComponent<Camera>();
        
        if (cam.orthographic)
        {
            // Paramètres recommandés pour orthographique
            cam.nearClipPlane = -10f;
            cam.farClipPlane = 100f;
            Debug.Log("✅ Paramètres orthographiques appliqués");
        }
        else
        {
            // Paramètres recommandés pour perspective
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            Debug.Log("✅ Paramètres perspective appliqués");
        }
        
        // Active le brouillard avec des valeurs sûres
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 80f;
        RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
        
        Debug.Log("✅ Brouillard configuré avec valeurs par défaut");
    }
}
