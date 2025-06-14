using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

/// <summary>
/// Quest marker system that displays directional arrows on screen edges
/// Points to quest zones where objectives are located
/// </summary>
public class QuestMarkerSystem : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Configuration asset for quest markers")]
    public QuestMarkerConfig config;
    
    [Header("Runtime Settings (Override Config)")]
    [Tooltip("Prefab for the UI arrow indicator on screen edges")]
    public GameObject uiArrowPrefab;
    
    // Config cache
    private float markerSize;
    private float edgeOffset;
    private Color markerColor;
    private bool showDistance;
    private float hideDistance;
    private bool enablePulse;
    private float pulseSpeed;
    private float pulseAmount;
    private bool debugMode;
    
    // Private variables
    private Camera mainCamera;
    private Transform player;
    private Dictionary<string, GameObject> activeMarkers = new Dictionary<string, GameObject>();
    private Canvas markerCanvas;
    
    // Singleton
    public static QuestMarkerSystem Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CreateMarkerCanvas();
            LoadConfiguration();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void LoadConfiguration()
    {
        // Try to load config from Resources if not assigned
        if (config == null)
        {
            config = Resources.Load<QuestMarkerConfig>("QuestMarkerConfig");
        }
        
        // If still no config, use default values
        if (config != null)
        {
            markerSize = config.markerSize;
            edgeOffset = config.edgeOffset;
            markerColor = config.markerColor;
            showDistance = config.showDistance;
            hideDistance = config.hideDistance;
            enablePulse = config.enablePulse;
            pulseSpeed = config.pulseSpeed;
            pulseAmount = config.pulseAmount;
            debugMode = config.debugMode;
            
            if (debugMode)
                Debug.Log("[QuestMarkerSystem] Configuration loaded from asset");
        }
        else
        {
            // Default values
            markerSize = 60f;
            edgeOffset = 50f;
            markerColor = Color.yellow;
            showDistance = true;
            hideDistance = 10f;
            enablePulse = true;
            pulseSpeed = 2f;
            pulseAmount = 0.2f;
            debugMode = false;
            
            Debug.LogWarning("[QuestMarkerSystem] No configuration found in Resources/QuestMarkerConfig");
        }
    }
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Find player
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        else
        {
            Debug.LogError("[QuestMarkerSystem] PlayerController not found!");
        }
        
        // Create default UI arrow prefab if none assigned
        if (uiArrowPrefab == null)
        {
            CreateDefaultUIArrowPrefab();
        }
        
        // Check if QuestZone tag exists, create it if not
        CheckAndCreateQuestZoneTag();
        
        // Auto-tag quest zones if needed
        AutoTagQuestZones();
    }
    
    void CheckAndCreateQuestZoneTag()
    {
        // Unity doesn't allow creating tags at runtime through normal API
        // But we can check if zones exist
        GameObject[] existingTagged = GameObject.FindGameObjectsWithTag("QuestZone");
        if (existingTagged.Length == 0)
        {
            Debug.LogWarning("[QuestMarkerSystem] No GameObjects with 'QuestZone' tag found. Please ensure your quest zones have the 'QuestZone' tag.");
        }
    }
    
    void AutoTagQuestZones()
    {
        // Find all QuestZone components and check their tags
        QuestZone[] allZones = FindObjectsOfType<QuestZone>();
        int taggedCount = 0;
        
        foreach (QuestZone zone in allZones)
        {
            if (zone.gameObject.CompareTag("QuestZone"))
            {
                taggedCount++;
            }
            else if (debugMode)
            {
                Debug.LogWarning($"[QuestMarkerSystem] Zone '{zone.zoneName}' needs 'QuestZone' tag");
            }
        }
        
        if (debugMode)
            Debug.Log($"[QuestMarkerSystem] Found {allZones.Length} zones, {taggedCount} properly tagged");
    }
    
    void CreateMarkerCanvas()
    {
        // Create a separate canvas for markers
        GameObject canvasGO = new GameObject("QuestMarkerCanvas");
        canvasGO.transform.SetParent(transform);
        
        markerCanvas = canvasGO.AddComponent<Canvas>();
        markerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        markerCanvas.sortingOrder = 100; // Make sure it's on top
        
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
    }
    
    void CreateDefaultUIArrowPrefab()
    {
        // Create a simple arrow UI indicator prefab
        uiArrowPrefab = new GameObject("UIArrowPrefab");
        uiArrowPrefab.SetActive(false);
        
        // Add RectTransform
        RectTransform rectTransform = uiArrowPrefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(markerSize, markerSize);
        
        // Add image component for arrow
        Image arrow = uiArrowPrefab.AddComponent<Image>();
        
        // Create a simple arrow sprite programmatically
        Texture2D arrowTexture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        // Create a more visible arrow shape
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                pixels[y * 64 + x] = Color.clear;
            }
        }
        
        // Draw arrow pointing up (triangle shape)
        for (int y = 16; y < 48; y++)
        {
            int width = (48 - y) / 2;
            int centerX = 32;
            
            for (int x = centerX - width; x <= centerX + width; x++)
            {
                if (x >= 0 && x < 64)
                {
                    pixels[y * 64 + x] = Color.white;
                }
            }
        }
        
        // Add arrow tail
        for (int y = 8; y < 24; y++)
        {
            for (int x = 28; x < 36; x++)
            {
                pixels[y * 64 + x] = Color.white;
            }
        }
        
        arrowTexture.SetPixels(pixels);
        arrowTexture.Apply();
        arrowTexture.filterMode = FilterMode.Point; // Keep it crisp
        
        // Convert to sprite
        arrow.sprite = Sprite.Create(arrowTexture, 
            new Rect(0, 0, 64, 64), 
            new Vector2(0.5f, 0.5f));
        
        arrow.color = markerColor; // Use the configured color
        
        // Add outline for better visibility
        Outline outline = uiArrowPrefab.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);
        
        // Add distance text
        GameObject distanceGO = new GameObject("DistanceText");
        distanceGO.transform.SetParent(uiArrowPrefab.transform, false);
        
        RectTransform textRect = distanceGO.AddComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, -40);
        textRect.sizeDelta = new Vector2(100, 30);
        
        TextMeshProUGUI distanceText = distanceGO.AddComponent<TextMeshProUGUI>();
        distanceText.text = "0m";
        distanceText.fontSize = 18;
        distanceText.alignment = TextAlignmentOptions.Center;
        distanceText.color = Color.white;
        distanceText.fontStyle = FontStyles.Bold;
        
        // Add outline to text for better readability
        distanceText.outlineWidth = 0.3f;
        distanceText.outlineColor = Color.black;
    }
    
    void Update()
    {
        if (player == null || mainCamera == null)
            return;
        
        UpdateQuestMarkers();
        AnimateMarkers();
    }
    
    void UpdateQuestMarkers()
    {
        if (player == null || mainCamera == null) return;
        
        // Get all active quest objects
        QuestObject[] questObjects = FindObjectsOfType<QuestObject>();
        
        // Track which zones have active quests (use HashSet to avoid duplicates)
        HashSet<QuestZone> zonesWithActiveQuests = new HashSet<QuestZone>();
        
        // Track objects without zones
        List<QuestObject> orphanObjects = new List<QuestObject>();
        
        foreach (QuestObject questObj in questObjects)
        {
            // Skip if already collected
            if (questObj.isCollected) continue;
            
            // Check if this quest object belongs to an active quest
            if (!IsQuestActive(questObj.questId))
                continue;
            
            // Find which zone this object is in
            QuestZone zone = questObj.GetComponentInParent<QuestZone>();
            if (zone == null)
            {
                // Try to find zone by proximity
                zone = FindNearestQuestZone(questObj.transform.position);
            }
            
            if (zone != null)
            {
                // Add zone to set (automatically handles duplicates)
                zonesWithActiveQuests.Add(zone);
            }
            else
            {
                // Track orphan objects
                orphanObjects.Add(questObj);
            }
        }
        
        // Update ONE marker per zone
        foreach (QuestZone zone in zonesWithActiveQuests)
        {
            UpdateMarkerForZone(zone);
        }
        
        // Update markers for orphan objects
        foreach (QuestObject orphan in orphanObjects)
        {
            UpdateMarkerForObject(orphan);
        }
        
        // Remove markers for completed quests
        CleanupInactiveMarkers(zonesWithActiveQuests, orphanObjects);
    }
    
    bool IsQuestActive(string questId)
    {
        if (QuestJournal.Instance == null) return false;
        
        var activeQuests = QuestJournal.Instance.GetActiveQuests();
        foreach (var quest in activeQuests)
        {
            if (quest.questId == questId)
                return true;
        }
        
        return false;
    }
    
    QuestZone FindNearestQuestZone(Vector3 position)
    {
        QuestZone[] allZones = FindObjectsOfType<QuestZone>();
        QuestZone nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (QuestZone zone in allZones)
        {
            float distance = Vector3.Distance(position, zone.transform.position);
            // Use spawnRadius instead of collider bounds
            if (distance < nearestDistance && distance <= zone.spawnRadius)
            {
                nearest = zone;
                nearestDistance = distance;
            }
        }
        
        return nearest;
    }
    
    void UpdateMarkerForObject(QuestObject questObj)
    {
        string markerId = "Obj_" + questObj.questId + "_" + questObj.objectName;
        
        // Pour les marqueurs d'exploration, utilise la position au sol
        Vector3 targetPosition;
        if (questObj.objectType == QuestObjectType.Marker)
        {
            targetPosition = GetGroundPosition(questObj.transform.position);
        }
        else
        {
            targetPosition = questObj.transform.position;
        }
        
        // Calculate distance
        float distance = Vector3.Distance(player.position, targetPosition);
        
        // Get or create marker
        GameObject marker = GetOrCreateMarker(markerId);
        
        // Hide marker if very close
        if (distance < hideDistance)
        {
            marker.SetActive(false);
            return;
        }
        
        marker.SetActive(true);
        UpdateMarkerPosition(marker, targetPosition, distance);
    }
    
    Vector3 GetGroundPosition(Vector3 position)
    {
        // Raycast down from position to find ground
        Vector3 rayStart = new Vector3(position.x, position.y + 50f, position.z);
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f))
        {
            // Return ground position with small offset
            return hit.point + Vector3.up * 0.5f;
        }
        
        // If no ground found from above, try from the position itself
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out RaycastHit hit2, 50f))
        {
            return hit2.point + Vector3.up * 0.5f;
        }
        
        // Final fallback: assume ground is at y=0
        Debug.LogWarning($"[QuestMarkerSystem] Could not find ground for marker at {position}. Using y=0.");
        return new Vector3(position.x, 0f, position.z);
    }
    
    void UpdateMarkerForZone(QuestZone zone)
    {
        string markerId = "Zone_" + zone.zoneName;
        
        // Pour les zones, utilise une position au sol plutôt que transform.position
        Vector3 targetPosition = GetGroundPositionForZone(zone);
        
        // Calculate distance to zone center
        float distance = Vector3.Distance(player.position, targetPosition);
        
        // Get or create marker
        GameObject marker = GetOrCreateMarker(markerId);
        
        // Hide marker if very close to zone
        if (distance < hideDistance)
        {
            marker.SetActive(false);
            return;
        }
        
        marker.SetActive(true);
        UpdateMarkerPosition(marker, targetPosition, distance);
    }
    
    Vector3 GetGroundPositionForZone(QuestZone zone)
    {
        // Start from zone position
        Vector3 zonePos = zone.transform.position;
        
        // Raycast down from high above to find ground
        Vector3 rayStart = new Vector3(zonePos.x, zonePos.y + 50f, zonePos.z);
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f))
        {
            // Return ground position with small offset
            return hit.point + Vector3.up * 0.5f;
        }
        
        // If no ground found, try from zone position
        if (Physics.Raycast(zonePos + Vector3.up * 2f, Vector3.down, out RaycastHit hit2, 10f))
        {
            return hit2.point + Vector3.up * 0.5f;
        }
        
        // Fallback: use zone position but lower it
        return new Vector3(zonePos.x, zonePos.y - 2f, zonePos.z);
    }
    
    void UpdateMarkerPosition(GameObject marker, Vector3 targetPosition, float distance)
    {
        // Calculate screen position
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPosition);
        
        // Check if object is behind camera
        bool isBehind = screenPos.z < 0;
        if (isBehind)
        {
            // Flip position if behind
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
            screenPos.z = -screenPos.z;
        }
        
        // Clamp to screen edges - ALWAYS show on edge
        Vector2 clampedPos = new Vector2(screenPos.x, screenPos.y);
        
        // Calculate center of screen
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        
        // Direction from center to target
        Vector2 direction = (clampedPos - screenCenter).normalized;
        
        // If direction is zero (target at screen center), use a default direction
        if (direction.magnitude < 0.01f)
        {
            direction = Vector2.up;
        }
        
        // Calculate the edge position
        float halfWidth = Screen.width * 0.5f - edgeOffset;
        float halfHeight = Screen.height * 0.5f - edgeOffset;
        
        // Find intersection with screen edge
        float tX = Mathf.Abs(direction.x) > 0.01f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
        float tY = Mathf.Abs(direction.y) > 0.01f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
        float t = Mathf.Min(tX, tY);
        
        // Position on edge
        clampedPos = screenCenter + direction * t;
        
        // Apply to marker
        marker.transform.position = clampedPos;
        
        // Rotate arrow to point toward target
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        marker.transform.rotation = Quaternion.Euler(0, 0, angle - 90); // -90 because arrow points up
        
        // Update distance text
        if (showDistance)
        {
            TextMeshProUGUI distanceText = marker.GetComponentInChildren<TextMeshProUGUI>();
            if (distanceText != null)
            {
                distanceText.text = $"{Mathf.RoundToInt(distance)}m";
                distanceText.color = Color.white;
            }
        }
    }
    

    GameObject GetOrCreateMarker(string markerId)
    {
        if (!activeMarkers.ContainsKey(markerId))
        {
            // Create new UI arrow indicator
            GameObject marker = Instantiate(uiArrowPrefab, markerCanvas.transform);
            marker.name = "QuestMarker_" + markerId;
            
            // Set size
            RectTransform rect = marker.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(markerSize, markerSize);
            
            // Set color
            Image arrow = marker.GetComponent<Image>();
            arrow.color = markerColor;
            
            activeMarkers[markerId] = marker;
        }
        
        return activeMarkers[markerId];
    }
    
    void AnimateMarkers()
    {
        if (!enablePulse) return;
        
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        
        foreach (var marker in activeMarkers.Values)
        {
            if (marker.activeSelf)
            {
                marker.transform.localScale = Vector3.one * scale;
            }
        }
    }
    
    void CleanupInactiveMarkers(HashSet<QuestZone> activeZones, List<QuestObject> orphanObjects)
    {
        List<string> markersToRemove = new List<string>();
        
        foreach (var kvp in activeMarkers)
        {
            bool shouldKeep = false;
            
            // Check if it's a zone marker
            if (kvp.Key.StartsWith("Zone_"))
            {
                // Check if this zone is still active
                foreach (QuestZone zone in activeZones)
                {
                    string zoneMarkerId = "Zone_" + zone.zoneName;
                    if (kvp.Key == zoneMarkerId)
                    {
                        shouldKeep = true;
                        break;
                    }
                }
            }
            // Check if it's an object marker
            else if (kvp.Key.StartsWith("Obj_"))
            {
                // Check if this object is still in orphan list
                foreach (QuestObject obj in orphanObjects)
                {
                    string objMarkerId = "Obj_" + obj.questId + "_" + obj.objectName;
                    if (kvp.Key == objMarkerId)
                    {
                        shouldKeep = true;
                        break;
                    }
                }
            }
            
            if (!shouldKeep)
            {
                markersToRemove.Add(kvp.Key);
            }
        }
        
        // Remove inactive markers
        foreach (string markerId in markersToRemove)
        {
            if (activeMarkers[markerId] != null)
            {
                Destroy(activeMarkers[markerId]);
            }
            activeMarkers.Remove(markerId);
        }
    }
    
    /// <summary>
    /// Reload configuration from asset (useful for runtime testing)
    /// </summary>
    [ContextMenu("Reload Configuration")]
    public void ReloadConfiguration()
    {
        LoadConfiguration();
        
        // Update existing markers with new config
        foreach (var marker in activeMarkers.Values)
        {
            if (marker != null)
            {
                // Update size
                RectTransform rect = marker.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(markerSize, markerSize);
                
                // Update color
                Image arrow = marker.GetComponent<Image>();
                if (arrow != null)
                    arrow.color = markerColor;
            }
        }
        
        Debug.Log("[QuestMarkerSystem] Configuration reloaded and applied to existing markers");
    }
    
    /// <summary>
    /// Force refresh all markers (useful after quest updates)
    /// </summary>
    public void RefreshMarkers()
    {
        // Clear all existing markers
        foreach (var marker in activeMarkers.Values)
        {
            if (marker != null)
                Destroy(marker);
        }
        activeMarkers.Clear();
    }
    
    /// <summary>
    /// Toggle marker visibility
    /// </summary>
    public void SetMarkersVisible(bool visible)
    {
        if (markerCanvas != null)
        {
            markerCanvas.gameObject.SetActive(visible);
        }
    }
}
