using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Système de marqueurs de quête - Pointe vers les objectifs actifs
/// </summary>
public class QuestMarkerSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float hideDistance = 10f;
    [SerializeField] private float edgeOffset = 50f;
    [SerializeField] private float markerSize = 50f;
    [SerializeField] private Color markerColor = Color.yellow;
    [SerializeField] private bool showDistance = true;
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
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
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        CreateMarkerCanvas();
        
        if (player == null)
            Debug.LogWarning("[QuestMarkerSystem] Joueur avec tag 'Player' non trouvé!");
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
        
        if (QuestManager.Instance == null) return targets;
        
        foreach (var quest in QuestManager.Instance.activeQuests)
        {
            // Quête complétée mais pas rendue : pointer vers le NPC donneur
            if (quest.currentProgress >= quest.questData.quantity && !quest.isCompleted)
            {
                AddReturnTarget(targets, quest);
                continue;
            }
            
            // Sinon, pointer vers les objectifs
            foreach (var obj in quest.spawnedObjects)
            {
                if (obj != null && obj.activeInHierarchy)
                {
                    QuestObject questObj = obj.GetComponent<QuestObject>();
                    if (questObj != null && !questObj.isCollected)
                    {
                        string key = $"{quest.questId}_{obj.GetInstanceID()}";
                        targets[key] = new MarkerTarget
                        {
                            position = obj.transform.position,
                            displayName = GetObjectDisplayName(questObj, quest),
                            questType = quest.questData.questType
                        };
                    }
                }
            }
        }
        
        return targets;
    }
    
    private void AddReturnTarget(Dictionary<string, MarkerTarget> targets, ActiveQuest quest)
    {
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
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
    
    private string GetObjectDisplayName(QuestObject questObj, ActiveQuest quest)
    {
        switch (quest.questData.questType)
        {
            case QuestType.FETCH:
                return $"Collecter: {questObj.objectName}";
            case QuestType.DELIVERY:
                return $"Livrer à: {questObj.objectName}";
            case QuestType.EXPLORE:
                return $"Explorer: {questObj.objectName}";
            case QuestType.TALK:
                return $"Parler à: {questObj.objectName}";
            case QuestType.INTERACT:
                return $"Interagir: {questObj.objectName}";
            default:
                return questObj.objectName;
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
        rect.sizeDelta = new Vector2(markerSize, markerSize);
        
        Image img = marker.AddComponent<Image>();
        img.color = markerColor;
        img.raycastTarget = false;
        
        Outline outline = marker.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);
        
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
