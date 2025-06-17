using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extension de ActiveQuest pour stocker la zone cible
/// </summary>
public static class ActiveQuestExtensions
{
    private static Dictionary<string, QuestZone> questZoneMapping = new Dictionary<string, QuestZone>();
    
    public static void SetTargetZone(this ActiveQuest quest, QuestZone zone)
    {
        if (zone != null)
        {
            questZoneMapping[quest.questId] = zone;
        }
    }
    
    public static QuestZone GetTargetZone(this ActiveQuest quest)
    {
        return questZoneMapping.ContainsKey(quest.questId) ? questZoneMapping[quest.questId] : null;
    }
    
    public static void ClearZoneMapping(string questId)
    {
        questZoneMapping.Remove(questId);
    }
}

/// <summary>
/// Système de marqueurs de quête - Pointe vers les zones de quête
/// </summary>
public class QuestMarkerSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float hideDistance = QuestSystemConfig.MarkerHideDistance;
    [SerializeField] private float edgeOffset = QuestSystemConfig.MarkerEdgeOffset;
    [SerializeField] private float markerSize = QuestSystemConfig.DefaultMarkerSize;
    [SerializeField] private Color markerColor = QuestSystemConfig.TrackedButtonColor;
    [SerializeField] private bool showDistance = true;
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = QuestSystemConfig.MarkerPulseSpeed;
    [SerializeField] private float pulseAmount = QuestSystemConfig.MarkerPulseAmount;
    
    [Header("Marker Visuals")]
    [SerializeField] private Sprite customMarkerSprite = null;
    [SerializeField] private bool useCustomSprite = true;
    [SerializeField] private Vector2 customSpriteSize = new Vector2(QuestSystemConfig.DefaultMarkerSize, QuestSystemConfig.DefaultMarkerSize);
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Composants
    private Camera mainCamera;
    private Transform player;
    private Canvas markerCanvas;
    private Dictionary<string, QuestMarker> activeMarkers = new Dictionary<string, QuestMarker>();
    
    public static QuestMarkerSystem Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (!IsSystemReady()) return;
        
        UpdateMarkers();
        
        if (enablePulse)
            AnimateMarkers();
    }
    
    private void InitializeSystem()
    {
        mainCamera = Camera.main;
        player = GameObject.FindGameObjectWithTag(QuestSystemConfig.PlayerTag)?.transform;
        
        CreateMarkerCanvas();
        
        // Le joueur peut être initialisé plus tard
    }
    
    private void CreateMarkerCanvas()
    {
        GameObject canvasObj = new GameObject("QuestMarkerCanvas");
        canvasObj.transform.SetParent(transform);
        
        markerCanvas = canvasObj.AddComponent<Canvas>();
        markerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        markerCanvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
    }
    
    private bool IsSystemReady()
    {
        return player != null && mainCamera != null && markerCanvas != null;
    }
    
    private void UpdateMarkers()
    {
        Dictionary<string, MarkerTarget> currentTargets = GetActiveQuestTargets();
        
        foreach (var kvp in currentTargets)
        {
            UpdateOrCreateMarker(kvp.Key, kvp.Value);
        }
        
        CleanupInactiveMarkers(currentTargets.Keys);
    }
    
    private Dictionary<string, MarkerTarget> GetActiveQuestTargets()
    {
        var targets = new Dictionary<string, MarkerTarget>();
        
        if (QuestManager.Instance == null || QuestJournal.Instance == null) return targets;
        
        // Récupérer la quête suivie
        JournalQuest trackedQuest = QuestJournal.Instance.GetTrackedQuest();
        if (trackedQuest == null) return targets;
        
        // Trouver la quête active correspondante
        ActiveQuest activeQuest = QuestManager.Instance.activeQuests.FirstOrDefault(q => q.questId == trackedQuest.questId);
        if (activeQuest == null) return targets;
        
        // Quête complétée mais pas rendue : pointer vers le NPC donneur
        if (activeQuest.currentProgress >= activeQuest.questData.quantity && !activeQuest.isCompleted)
        {
            AddReturnTarget(targets, activeQuest);
            return targets;
        }
        
        // Trouver la zone de la quête
        QuestZone zone = FindQuestZone(activeQuest);
        
        if (zone != null)
        {
            // Vérifier qu'il y a des objectifs actifs
            bool hasActiveObjectives = HasActiveObjectives(activeQuest);
            
            if (hasActiveObjectives)
            {
                string zoneKey = $"zone_{zone.GetInstanceID()}";
                targets[zoneKey] = new MarkerTarget
                {
                    position = zone.transform.position,
                    displayName = GetZoneDisplayName(zone, activeQuest),
                    questType = activeQuest.questData.questType
                };
            }
        }

        
        return targets;
    }
    
    private QuestZone FindQuestZone(ActiveQuest quest)
    {
        // 1. Essayer avec l'extension
        QuestZone zone = quest.GetTargetZone();
        if (zone != null) return zone;
        
        // 2. Trouver depuis les objets spawnés
        foreach (var obj in quest.spawnedObjects)
        {
            if (obj != null)
            {
                // Chercher la zone parent
                zone = obj.GetComponentInParent<QuestZone>();
                if (zone != null) return zone;
                
                // Chercher la zone la plus proche
                float minDistance = float.MaxValue;
                QuestZone[] allZones = FindObjectsOfType<QuestZone>();
                foreach (var z in allZones)
                {
                    float dist = Vector3.Distance(obj.transform.position, z.transform.position);
                    if (dist < minDistance && dist <= z.spawnRadius * 1.5f) // Marge de sécurité
                    {
                        minDistance = dist;
                        zone = z;
                    }
                }
                
                if (zone != null) return zone;
            }
        }
        
        // 3. Dernier recours : chercher par nom
        if (!string.IsNullOrEmpty(quest.questData.zoneName))
        {
            return FindQuestZoneByName(quest.questData.zoneName);
        }
        
        return null;
    }
    
    private bool HasActiveObjectives(ActiveQuest quest)
    {
        foreach (var obj in quest.spawnedObjects)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                QuestObject questObj = obj.GetComponent<QuestObject>();
                if (questObj != null && !questObj.isCollected)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private QuestZone FindQuestZoneByName(string zoneName)
    {
        QuestZone[] allZones = FindObjectsOfType<QuestZone>();
        
        // Recherche exacte d'abord
        foreach (var zone in allZones)
        {
            if (zone.zoneName.Equals(zoneName, System.StringComparison.OrdinalIgnoreCase))
                return zone;
        }
        
        // Recherche partielle si pas de correspondance exacte
        string normalizedSearch = zoneName.ToLower().Replace(" ", "_").Replace("-", "_");
        foreach (var zone in allZones)
        {
            string normalizedZone = zone.zoneName.ToLower().Replace(" ", "_").Replace("-", "_");
            if (normalizedZone.Contains(normalizedSearch) || normalizedSearch.Contains(normalizedZone))
                return zone;
        }
        
        return null;
    }
    
    private string GetZoneDisplayName(QuestZone zone, ActiveQuest quest)
    {
        switch (quest.questData.questType)
        {
            case QuestType.FETCH:
                return $"Collecter dans {zone.zoneName}";
            case QuestType.DELIVERY:
                return $"Livraison dans {zone.zoneName}";
            case QuestType.EXPLORE:
                return $"Explorer {zone.zoneName}";
            case QuestType.TALK:
                return $"Discussion dans {zone.zoneName}";
            case QuestType.INTERACT:
                return $"Interaction dans {zone.zoneName}";
            default:
                return zone.zoneName;
        }
    }
    
    private void AddReturnTarget(Dictionary<string, MarkerTarget> targets, ActiveQuest quest)
    {
        GameObject[] npcs = GameObject.FindGameObjectsWithTag(QuestSystemConfig.NPCTag);
        foreach (var npc in npcs)
        {
            NPC npcComponent = npc.GetComponent<NPC>();
            if (npcComponent != null && npcComponent.npcName == quest.giverNPCName)
            {
                string key = $"{quest.questId}_return";
                targets[key] = new MarkerTarget
                {
                    position = npc.transform.position,
                    displayName = $"Retourner voir {quest.giverNPCName}",
                    questType = quest.questData.questType
                };
                break;
            }
        }
    }

    
    private void UpdateOrCreateMarker(string markerId, MarkerTarget target)
    {
        float distance = Vector3.Distance(player.position, target.position);
        
        if (distance < hideDistance)
        {
            if (activeMarkers.ContainsKey(markerId))
                activeMarkers[markerId].gameObject.SetActive(false);
            return;
        }
        
        QuestMarker marker = GetOrCreateMarker(markerId);
        marker.gameObject.SetActive(true);
        
        UpdateMarkerPosition(marker, target, distance);
    }
    
    private QuestMarker GetOrCreateMarker(string markerId)
    {
        if (!activeMarkers.ContainsKey(markerId))
        {
            GameObject markerObj = CreateMarkerGameObject(markerId);
            QuestMarker marker = new QuestMarker
            {
                gameObject = markerObj,
                image = markerObj.GetComponent<Image>(),
                distanceText = markerObj.GetComponentInChildren<TextMeshProUGUI>(),
                rectTransform = markerObj.GetComponent<RectTransform>()
            };
            activeMarkers[markerId] = marker;
        }
        
        return activeMarkers[markerId];
    }
    
    private GameObject CreateMarkerGameObject(string markerId)
    {
        GameObject marker = new GameObject($"Marker_{markerId}");
        marker.transform.SetParent(markerCanvas.transform);
        
        RectTransform rect = marker.AddComponent<RectTransform>();
        
        Image img = marker.AddComponent<Image>();
        img.raycastTarget = false;
        
        // Utiliser le sprite custom si disponible
        if (useCustomSprite && customMarkerSprite != null)
        {
            img.sprite = customMarkerSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white; // Pas de teinte pour le sprite custom
            rect.sizeDelta = customSpriteSize;
        }
        else
        {
            // Fallback : utiliser un carré coloré
            img.sprite = null; // Sprite blanc par défaut d'Unity
            img.color = markerColor;
            rect.sizeDelta = new Vector2(markerSize, markerSize);
            
            // Ajouter un outline seulement pour le carré par défaut
            Outline outline = marker.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);
        }
        
        if (showDistance)
        {
            GameObject textObj = new GameObject("Distance");
            textObj.transform.SetParent(marker.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, -markerSize * 0.6f);
            textRect.sizeDelta = new Vector2(100, 30);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "0m";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
        }
        
        return marker;
    }
    
    private void UpdateMarkerPosition(QuestMarker marker, MarkerTarget target, float distance)
    {
        if (mainCamera.orthographic)
        {
            UpdateMarkerPositionOrthographic(marker, target, distance);
        }
        else
        {
            UpdateMarkerPositionPerspective(marker, target, distance);
        }
    }
    
    private void UpdateMarkerPositionOrthographic(QuestMarker marker, MarkerTarget target, float distance)
    {
        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
        
        bool isOnScreen = screenPos.x > edgeOffset && screenPos.x < Screen.width - edgeOffset &&
                         screenPos.y > edgeOffset && screenPos.y < Screen.height - edgeOffset &&
                         screenPos.z > 0;
        
        if (isOnScreen)
        {
            marker.gameObject.SetActive(false);
            return;
        }
        
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 direction = ((Vector2)screenPos - screenCenter).normalized;
        
        float halfWidth = Screen.width * 0.5f - edgeOffset;
        float halfHeight = Screen.height * 0.5f - edgeOffset;
        
        float tX = Mathf.Abs(direction.x) > 0.001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
        float tY = Mathf.Abs(direction.y) > 0.001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
        float t = Mathf.Min(tX, tY);
        
        Vector2 edgePos = screenCenter + direction * t;
        
        marker.gameObject.transform.position = edgePos;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        marker.gameObject.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        
        if (marker.distanceText != null)
            marker.distanceText.text = $"{Mathf.RoundToInt(distance)}m";
    }
    
    private void UpdateMarkerPositionPerspective(QuestMarker marker, MarkerTarget target, float distance)
    {
        Vector3 screenPos = mainCamera.WorldToViewportPoint(target.position);
        
        if (screenPos.z < 0)
        {
            screenPos.x = 1 - screenPos.x;
            screenPos.y = 1 - screenPos.y;
            screenPos.z = 0;
        }
        
        bool isVisible = screenPos.x > 0.1f && screenPos.x < 0.9f && 
                        screenPos.y > 0.1f && screenPos.y < 0.9f && 
                        screenPos.z > 0;
        
        if (isVisible)
        {
            marker.gameObject.SetActive(false);
            return;
        }
        
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        Vector2 direction = new Vector2(screenPos.x - 0.5f, screenPos.y - 0.5f).normalized;
        
        float marginX = edgeOffset / Screen.width;
        float marginY = edgeOffset / Screen.height;
        
        float tX = Mathf.Abs(direction.x) > 0.001f ? (0.5f - marginX) / Mathf.Abs(direction.x) : float.MaxValue;
        float tY = Mathf.Abs(direction.y) > 0.001f ? (0.5f - marginY) / Mathf.Abs(direction.y) : float.MaxValue;
        float t = Mathf.Min(tX, tY);
        
        Vector2 edgePos = screenCenter + direction * t;
        Vector2 screenPosition = new Vector2(edgePos.x * Screen.width, edgePos.y * Screen.height);
        
        marker.gameObject.transform.position = screenPosition;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        marker.gameObject.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        
        if (marker.distanceText != null)
            marker.distanceText.text = $"{Mathf.RoundToInt(distance)}m";
    }
    
    private void AnimateMarkers()
    {
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        
        foreach (var marker in activeMarkers.Values)
        {
            if (marker.gameObject.activeSelf)
            {
                marker.rectTransform.localScale = Vector3.one * scale;
            }
        }
    }
    
    private void CleanupInactiveMarkers(IEnumerable<string> activeIds)
    {
        HashSet<string> activeSet = new HashSet<string>(activeIds);
        List<string> toRemove = new List<string>();
        
        foreach (var kvp in activeMarkers)
        {
            if (!activeSet.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (string key in toRemove)
        {
            if (activeMarkers[key].gameObject != null)
            {
                Destroy(activeMarkers[key].gameObject);
            }
            activeMarkers.Remove(key);
        }
    }
    
    public void RefreshMarkers()
    {
        foreach (var marker in activeMarkers.Values)
        {
            if (marker.gameObject != null)
                Destroy(marker.gameObject);
        }
        activeMarkers.Clear();
    }
    
    public void SetMarkersVisible(bool visible)
    {
        if (markerCanvas != null)
            markerCanvas.gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// Change le sprite du marqueur en runtime
    /// </summary>
    public void SetMarkerSprite(Sprite newSprite)
    {
        customMarkerSprite = newSprite;
        useCustomSprite = (newSprite != null);
        
        // Rafraîchir tous les marqueurs existants
        RefreshMarkers();
    }
    
    /// <summary>
    /// Change la taille du sprite custom
    /// </summary>
    public void SetCustomSpriteSize(Vector2 newSize)
    {
        customSpriteSize = newSize;
        
        // Mettre à jour les marqueurs existants si on utilise un sprite custom
        if (useCustomSprite && customMarkerSprite != null)
        {
            foreach (var marker in activeMarkers.Values)
            {
                if (marker.rectTransform != null)
                {
                    marker.rectTransform.sizeDelta = customSpriteSize;
                }
            }
        }
    }
    
    /// <summary>
    /// Active/désactive l'utilisation du sprite custom
    /// </summary>
    public void SetUseCustomSprite(bool use)
    {
        useCustomSprite = use;
        RefreshMarkers();
    }
    

    
    private class MarkerTarget
    {
        public Vector3 position;
        public string displayName;
        public QuestType questType;
    }
    
    private class QuestMarker
    {
        public GameObject gameObject;
        public Image image;
        public TextMeshProUGUI distanceText;
        public RectTransform rectTransform;
    }
}
