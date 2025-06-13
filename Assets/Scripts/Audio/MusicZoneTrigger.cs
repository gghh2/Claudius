using UnityEngine;

public class MusicZoneTrigger : MonoBehaviour
{
    [Header("Zone Configuration")]
    [Tooltip("Type of music zone")]
    public MusicZoneType zoneType = MusicZoneType.Laboratory;
    
    [Tooltip("Override with specific track name (optional)")]
    public string specificTrackName = "";
    
    [Header("Trigger Settings")]
    [Tooltip("Only trigger for objects with this tag")]
    public string triggerTag = "Player";
    
    [Header("Visual Settings")]
    [Tooltip("Show zone boundaries in editor")]
    public bool showGizmos = true;
    
    [Tooltip("Gizmo color")]
    public Color gizmoColor = new Color(0.5f, 0.5f, 1f, 0.3f);
    
    [Header("Debug")]
    public bool debugMode = true;
    
    private bool playerInZone = false;
    
    void Start()
    {
        // Ensure we have a collider set as trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError($"MusicZoneTrigger on {gameObject.name} needs a Collider component!");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerTag) && !playerInZone)
        {
            playerInZone = true;
            
            if (debugMode)
                Debug.Log($"🎵 Player entered music zone: {zoneType}");
            
            if (MusicManager.Instance != null)
            {
                // If specific track is set, play it
                if (!string.IsNullOrEmpty(specificTrackName))
                {
                    MusicManager.Instance.PlayTrackByName(specificTrackName);
                }
                else
                {
                    // Otherwise, let the manager choose based on zone type
                    MusicManager.Instance.SetZone(zoneType);
                }
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(triggerTag) && playerInZone)
        {
            playerInZone = false;
            
            if (debugMode)
                Debug.Log($"🎵 Player left music zone: {zoneType}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
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
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw zone name when selected
        Vector3 labelPos = transform.position + Vector3.up * 2f;
        string label = $"Music Zone: {zoneType}";
        if (!string.IsNullOrEmpty(specificTrackName))
        {
            label += $"\nTrack: {specificTrackName}";
        }
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, label);
        #endif
    }
}
