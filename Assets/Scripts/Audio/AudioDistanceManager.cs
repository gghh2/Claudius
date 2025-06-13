using UnityEngine;

public class AudioDistanceManager : MonoBehaviour
{
    public static AudioDistanceManager Instance { get; private set; }
    
    [Header("===== CAMERA ZOOM AUDIO CONTROL =====")]
    
    [Header("References")]
    [Tooltip("The camera to track (auto-finds main camera if null)")]
    public Camera targetCamera;
    
    [Tooltip("The player transform to measure distance from")]
    public Transform playerTransform;
    
    [Header("Zoom Settings (Orthographic Camera)")]
    [Tooltip("Camera size at which volume is at 100% (zoomed in)")]
    public float minZoomSize = 2f;
    
    [Tooltip("Camera size at which volume reaches minimum (zoomed out)")]
    public float maxZoomSize = 15f;
    
    [Tooltip("Minimum volume multiplier at max distance")]
    [Range(0f, 1f)]
    public float minVolumeMultiplier = 0.2f;
    
    [Header("Volume Categories")]
    [Tooltip("Apply to ambient sounds")]
    public bool affectAmbientSounds = true;
    
    [Tooltip("Apply to sound effects")]
    public bool affectSoundEffects = true;
    
    [Tooltip("Apply to NPC voices")]
    public bool affectNPCVoices = true;
    
    [Tooltip("Apply to player sounds")]
    public bool affectPlayerSounds = true;
    
    [Tooltip("Keep music at full volume")]
    public bool excludeMusic = true;
    
    [Header("Smoothing")]
    [Tooltip("Smooth volume changes")]
    public bool smoothTransition = true;
    
    [Tooltip("Smoothing speed")]
    public float smoothSpeed = 2f;
    
    // Private
    private float currentVolumeMultiplier = 1f;
    private float targetVolumeMultiplier = 1f;
    private bool usingOrthographicMode = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Auto-find references if not set
        if (targetCamera == null)
            targetCamera = Camera.main;
            
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
        
        if (targetCamera == null || playerTransform == null)
        {
            Debug.LogError("AudioDistanceManager: Missing camera or player reference!");
            enabled = false;
            return;
        }
        
        // Check if camera is orthographic
        usingOrthographicMode = targetCamera.orthographic;
    }
    
    void Update()
    {
        if (targetCamera == null || playerTransform == null)
            return;
        
        float currentValue;
        
        // Use orthographic size for orthographic cameras
        if (usingOrthographicMode)
        {
            currentValue = targetCamera.orthographicSize;
            
            // Calculate volume multiplier based on zoom size
            if (currentValue <= minZoomSize)
            {
                targetVolumeMultiplier = 1f;
            }
            else if (currentValue >= maxZoomSize)
            {
                targetVolumeMultiplier = minVolumeMultiplier;
            }
            else
            {
                // Linear interpolation between min and max zoom
                float t = (currentValue - minZoomSize) / (maxZoomSize - minZoomSize);
                targetVolumeMultiplier = Mathf.Lerp(1f, minVolumeMultiplier, t);
            }
        }
        else
        {
            // Fallback to distance-based for perspective cameras
            float distance = Vector3.Distance(targetCamera.transform.position, playerTransform.position);
            currentValue = distance;
            
            // Use zoom values as distance thresholds for perspective cameras
            if (distance <= minZoomSize * 5f) // Rough conversion
            {
                targetVolumeMultiplier = 1f;
            }
            else if (distance >= maxZoomSize * 5f)
            {
                targetVolumeMultiplier = minVolumeMultiplier;
            }
            else
            {
                float t = (distance - minZoomSize * 5f) / ((maxZoomSize - minZoomSize) * 5f);
                targetVolumeMultiplier = Mathf.Lerp(1f, minVolumeMultiplier, t);
            }
        }
        
        // Apply smoothing if enabled
        if (smoothTransition)
        {
            currentVolumeMultiplier = Mathf.Lerp(currentVolumeMultiplier, targetVolumeMultiplier, Time.deltaTime * smoothSpeed);
        }
        else
        {
            currentVolumeMultiplier = targetVolumeMultiplier;
        }
        
        // Apply volume changes
        ApplyVolumeMultiplier();
    }
    
    void ApplyVolumeMultiplier()
    {
        // Apply to ambient sounds
        if (affectAmbientSounds)
        {
            AmbientSoundZone[] ambientZones = FindObjectsOfType<AmbientSoundZone>();
            foreach (var zone in ambientZones)
            {
                zone.SetDistanceMultiplier(currentVolumeMultiplier);
            }
        }
        
        // Apply to sound effects manager
        if (affectSoundEffects && SoundEffectsManager.Instance != null)
        {
            SoundEffectsManager.Instance.SetDistanceMultiplier(currentVolumeMultiplier);
        }
    }
    
    public float GetCurrentMultiplier()
    {
        return currentVolumeMultiplier;
    }
    
    public void SetZoomRange(float min, float max)
    {
        minZoomSize = Mathf.Max(0.1f, min);
        maxZoomSize = Mathf.Max(minZoomSize + 0.1f, max);
    }
}
