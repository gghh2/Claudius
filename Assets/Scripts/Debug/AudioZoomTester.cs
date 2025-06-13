using UnityEngine;

/// <summary>
/// Debug tool to test audio zoom functionality
/// </summary>
public class AudioZoomTester : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("Show debug UI")]
    public bool showDebugUI = true;
    
    [Tooltip("Test different zoom levels")]
    public bool enableQuickZoomTests = true;
    
    private CameraFollow cameraFollow;
    private AudioDistanceManager audioManager;
    private FootstepSystem footstepSystem;
    
    void Start()
    {
        // Find components
        cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        audioManager = AudioDistanceManager.Instance;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            footstepSystem = player.GetComponent<FootstepSystem>();
        }
        
        if (cameraFollow == null)
        {
            Debug.LogError("AudioZoomTester: CameraFollow not found on main camera!");
        }
        
        if (audioManager == null)
        {
            Debug.LogError("AudioZoomTester: AudioDistanceManager instance not found!");
        }
    }
    
    void Update()
    {
        if (!enableQuickZoomTests) return;
        
        // Quick zoom tests with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetZoomLevel(2f); // Close zoom
            Debug.Log("🔍 Zoom set to CLOSE (2) - Volume should be 100%");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetZoomLevel(5f); // Default zoom
            Debug.Log("🔍 Zoom set to DEFAULT (5)");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetZoomLevel(10f); // Medium zoom
            Debug.Log("🔍 Zoom set to MEDIUM (10)");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetZoomLevel(15f); // Far zoom
            Debug.Log("🔍 Zoom set to FAR (15) - Volume should be at minimum");
        }
        
        // Force footstep for testing
        if (Input.GetKeyDown(KeyCode.F) && footstepSystem != null)
        {
            footstepSystem.ForceFootstep();
            Debug.Log("🦶 Forced footstep sound");
        }
    }
    
    void SetZoomLevel(float size)
    {
        if (cameraFollow != null)
        {
            cameraFollow.SetZoom(size);
        }
    }
    
    void OnGUI()
    {
        if (!showDebugUI) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 300, 300));
        
        GUI.color = Color.black;
        GUI.Box(new Rect(0, 0, 300, 300), "");
        GUI.color = Color.white;
        
        GUILayout.Label("=== AUDIO ZOOM DEBUG ===");
        
        if (cameraFollow != null && Camera.main != null)
        {
            GUILayout.Label($"Current Zoom: {Camera.main.orthographicSize:F1}");
            GUILayout.Label($"Target Zoom: {cameraFollow.GetCurrentZoom():F1}");
        }
        
        if (audioManager != null)
        {
            float volumePercent = audioManager.GetCurrentMultiplier() * 100f;
            GUILayout.Label($"Audio Volume: {volumePercent:F0}%");
            
            // Visual volume bar
            GUI.Box(new Rect(10, 80, 280, 20), "");
            GUI.color = Color.green;
            GUI.Box(new Rect(10, 80, 280 * audioManager.GetCurrentMultiplier(), 20), "");
            GUI.color = Color.white;
        }
        
        GUILayout.Space(40);
        
        GUILayout.Label("--- CONTROLS ---");
        GUILayout.Label("1-4: Set zoom levels");
        GUILayout.Label("Mouse Wheel: Zoom in/out");
        GUILayout.Label("F: Force footstep sound");
        GUILayout.Label("R: Reset zoom");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Zoom Close (100% volume)"))
        {
            SetZoomLevel(2f);
        }
        
        if (GUILayout.Button("Zoom Default"))
        {
            SetZoomLevel(5f);
        }
        
        if (GUILayout.Button("Zoom Far (20% volume)"))
        {
            SetZoomLevel(15f);
        }
        
        GUILayout.EndArea();
    }
}
