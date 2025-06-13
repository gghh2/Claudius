using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class AmbientSoundZone : MonoBehaviour
{
    [Header("===== AMBIENT SOUND CONFIGURATION =====")]
    
    [Header("Audio Settings")]
    [Tooltip("The ambient sound to play in this zone")]
    public AudioClip ambientSound;
    
    [Tooltip("Volume of the ambient sound")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    
    [Tooltip("Is this a 3D sound (true) or 2D sound (false)")]
    public bool is3DSound = true;
    
    [Tooltip("Fade in/out duration")]
    public float fadeDuration = 2f;
    
    [Header("3D Sound Settings")]
    [Tooltip("Minimum distance for 3D sound (only used if useDistanceInZone is true)")]
    public float minDistance = 1f;
    
    [Tooltip("Maximum distance for 3D sound (only used if useDistanceInZone is true)")]
    public float maxDistance = 50f;
    
    [Tooltip("Apply 3D distance attenuation inside the zone (false = constant volume in zone)")]
    public bool useDistanceInZone = false;
    
    [Header("Trigger Settings")]
    [Tooltip("Tag required to trigger the sound (usually Player)")]
    public string triggerTag = "Player";
    
    [Header("Options")]
    [Tooltip("Play sound on awake (without trigger)")]
    public bool playOnAwake = false;
    
    [Tooltip("Destroy the audio source when leaving zone")]
    public bool destroyOnExit = true;
    
    [Header("Visual")]
    [Tooltip("Show zone in editor")]
    public bool showGizmos = true;
    
    [Tooltip("Gizmo color")]
    public Color gizmoColor = new Color(0f, 1f, 0.5f, 0.3f);
    
    [Header("Debug")]
    public bool debugMode = true;
    
    // Private
    private AudioSource audioSource;
    private Coroutine fadeCoroutine;
    private bool isPlayerInZone = false;
    private float baseVolume = 1f; // Store the base volume
    private float distanceMultiplier = 1f; // Multiplier from camera distance
    
    void Start()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // Play on awake if requested
        if (playOnAwake && ambientSound != null)
        {
            CreateAndPlaySound();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerTag) && !isPlayerInZone)
        {
            isPlayerInZone = true;
            
            if (debugMode)
                Debug.Log($"🔊 Player entered ambient zone: {gameObject.name}");
            
            StartAmbientSound();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(triggerTag) && isPlayerInZone)
        {
            isPlayerInZone = false;
            
            if (debugMode)
                Debug.Log($"🔇 Player left ambient zone: {gameObject.name}");
            
            StopAmbientSound();
        }
    }
    
    void StartAmbientSound()
    {
        if (ambientSound == null) return;
        
        // Create audio source if needed
        if (audioSource == null)
        {
            CreateAndPlaySound();
        }
        else
        {
            // Resume if it exists
            audioSource.Play();
        }
        
        // Fade in
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        fadeCoroutine = StartCoroutine(FadeIn());
    }
    
    void StopAmbientSound()
    {
        if (audioSource == null) return;
        
        // Fade out
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        fadeCoroutine = StartCoroutine(FadeOut());
    }
    
    void CreateAndPlaySound()
    {
        // Create audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio source
        audioSource.clip = ambientSound;
        audioSource.volume = 0f; // Start at 0 for fade in
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        
        // 2D or 3D sound
        if (is3DSound && useDistanceInZone)
        {
            // True 3D sound with distance attenuation
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
        else if (is3DSound && !useDistanceInZone)
        {
            // 3D positioned but constant volume in zone
            audioSource.spatialBlend = 1f;
            // Set min distance to a huge value so volume stays constant
            audioSource.minDistance = 1000f;
            audioSource.maxDistance = 1001f;
        }
        else
        {
            // 2D sound
            audioSource.spatialBlend = 0f;
        }
        
        // Get master volume if available
        float masterVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
        
        audioSource.Play();
        
        if (!playOnAwake)
        {
            // Only fade in if not playing on awake
            StartCoroutine(FadeIn());
        }
        else
        {
            // Set volume immediately if playing on awake
            audioSource.volume = volume * masterVolume;
        }
    }
    
    IEnumerator FadeIn()
    {
        if (audioSource == null) yield break;
        
        float masterVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
        baseVolume = volume * masterVolume;
        float targetVolume = baseVolume * distanceMultiplier;
        float startVolume = audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / fadeDuration);
            yield return null;
        }
        
        audioSource.volume = targetVolume;
    }
    
    IEnumerator FadeOut()
    {
        if (audioSource == null) yield break;
        
        float startVolume = audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }
        
        audioSource.volume = 0f;
        audioSource.Stop();
        
        // Destroy audio source if requested
        if (destroyOnExit && audioSource != null)
        {
            Destroy(audioSource);
            audioSource = null;
        }
    }
    
    // Update volume when settings change
    public void UpdateVolume()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            float masterVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
            baseVolume = volume * masterVolume;
            audioSource.volume = baseVolume * distanceMultiplier;
        }
    }
    
    // Set the distance multiplier from AudioDistanceManager
    public void SetDistanceMultiplier(float multiplier)
    {
        distanceMultiplier = multiplier;
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.volume = baseVolume * distanceMultiplier;
        }
    }
    
    void OnDestroy()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
    }
    
    // Editor visualization
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        
        // Draw trigger zone
        Gizmos.color = gizmoColor;
        
        if (col is BoxCollider box)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
        }
        
        // Draw 3D sound range if applicable
        if (is3DSound && useDistanceInZone)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, maxDistance);
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, minDistance);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Show label when selected
        Vector3 labelPos = transform.position + Vector3.up * 2f;
        string label = $"Ambient Zone: {gameObject.name}";
        
        if (ambientSound != null)
        {
            label += $"\nSound: {ambientSound.name}";
            label += $"\nVolume: {Mathf.RoundToInt(volume * 100)}%";
            label += is3DSound ? " (3D)" : " (2D)";
        }
        else
        {
            label += "\nNo sound assigned!";
        }
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, label);
        #endif
    }
}
