using UnityEngine;
using System.Collections.Generic;

public class AudioDistanceManager : MonoBehaviour
{
    public static AudioDistanceManager Instance { get; private set; }
    
    [Header("===== CAMERA DISTANCE AUDIO CONTROL =====")]
    
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
    
    [Header("Debug")]
    public bool debugMode = true;
    public bool showDistanceGizmos = true;
    
    // Private
    private float currentVolumeMultiplier = 1f;
    private float targetVolumeMultiplier = 1f;
    private float lastZoomSize = 0f;
    private bool usingOrthographicMode = false;
    
    // Audio mixer groups (optional, for more control)
    private UnityEngine.Audio.AudioMixerGroup ambientMixerGroup;
    private UnityEngine.Audio.AudioMixerGroup sfxMixerGroup;
    private UnityEngine.Audio.AudioMixerGroup voiceMixerGroup;
    
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
        if (usingOrthographicMode)
        {
            Debug.Log("📷 AudioDistanceManager: Using orthographic zoom-based volume control");
        }
        else
        {
            Debug.LogWarning("📷 AudioDistanceManager: Camera is not orthographic! Volume control may not work as expected.");
        }
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
        
        // Debug
        if (debugMode && Mathf.Abs(currentValue - lastZoomSize) > 0.1f)
        {
            lastZoomSize = currentValue;
            if (usingOrthographicMode)
            {
                Debug.Log($"📷 Camera Zoom Size: {currentValue:F1} | Volume: {(currentVolumeMultiplier * 100):F0}%");
            }
            else
            {
                Debug.Log($"📷 Camera Distance: {currentValue:F1}m | Volume: {(currentVolumeMultiplier * 100):F0}%");
            }
        }
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
        
        // Note: For NPC voices and player sounds, you'll need to add similar methods
        // to those systems or use Audio Mixer groups
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
    
    // Keep old method for compatibility
    public void SetDistanceRange(float min, float max)
    {
        Debug.LogWarning("SetDistanceRange is deprecated for orthographic cameras. Use SetZoomRange instead.");
        SetZoomRange(min / 5f, max / 5f); // Rough conversion
    }
    
    void OnDrawGizmos()
    {
        if (!showDistanceGizmos || playerTransform == null || targetCamera == null)
            return;
        
        if (usingOrthographicMode)
        {
            // For orthographic cameras, show zoom levels as UI info
            #if UNITY_EDITOR
            Vector3 labelPos = targetCamera.transform.position + Vector3.up * 2f;
            string zoomInfo = $"Zoom Size: {targetCamera.orthographicSize:F1}\n";
            zoomInfo += $"Volume: {(currentVolumeMultiplier * 100):F0}%\n";
            zoomInfo += $"Min Zoom (100%): {minZoomSize:F1}\n";
            zoomInfo += $"Max Zoom ({(minVolumeMultiplier * 100):F0}%): {maxZoomSize:F1}";
            
            UnityEditor.Handles.Label(labelPos, zoomInfo);
            
            // Draw line from camera to player
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(playerTransform.position, targetCamera.transform.position);
            #endif
        }
        else
        {
            // Original distance-based visualization for perspective cameras
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, minZoomSize * 5f);
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, maxZoomSize * 5f);
            
            float distance = Vector3.Distance(targetCamera.transform.position, playerTransform.position);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(playerTransform.position, targetCamera.transform.position);
            
            #if UNITY_EDITOR
            Vector3 labelPos = targetCamera.transform.position + Vector3.up;
            UnityEditor.Handles.Label(labelPos, $"Distance: {distance:F1}m\nVolume: {(currentVolumeMultiplier * 100):F0}%");
            #endif
        }
    }
}
