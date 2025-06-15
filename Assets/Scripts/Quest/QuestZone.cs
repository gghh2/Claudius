using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum QuestZoneType
{
    Laboratory,     // Laboratoire scientifique
    Hangar,        // Hangar à vaisseaux
    Market,        // Zone commerciale
    Ruins,         // Ruines anciennes
    SecurityArea,  // Zone de sécurité
    Storage,       // Entrepôts
    Residential,   // Zone résidentielle
    Engineering,   // Salle des machines
    Bridge,        // Pont de commandement
    MedicalBay     // Infirmerie
}

[System.Serializable]
public enum QuestObjectType
{
    Item,               // Objet à ramasser
    NPC,                // PNJ temporaire
    InteractableObject, // Terminal, console, etc.
    Marker              // Simple marqueur visuel
}

public class QuestZone : MonoBehaviour
{
    [Header("===== AI CONFIGURATION - Used by AI System =====")]
    
    [Header("Zone Identity (AI)")]
    [Tooltip("AI SYSTEM - Zone name displayed in quests and dialogues")]
    public string zoneName = "Zone Sans Nom";
    
    [Tooltip("AI SYSTEM - Zone type for AI dialogue matching")]
    public QuestZoneType zoneType = QuestZoneType.Laboratory;
    
    [Header("Zone Description (AI)")]
    [Tooltip("AI SYSTEM - Detailed description for coherent quest generation")]
    [TextArea(3, 5)]
    public string description = "Description de la zone pour l'IA - Soyez précis sur ce qu'on peut trouver ici";
    
    [Header("Supported Quest Objects (AI)")]
    [Tooltip("AI CRITICAL - Determines which quest types can be created here")]
    public List<QuestObjectType> supportedObjects = new List<QuestObjectType>();
    
    [Space(20)]
    [Header("===== TECHNICAL CONFIGURATION - Not used by AI =====")]
    
    [Header("Visual Settings")]
    [Tooltip("Technical - Editor visualization color")]
    public Color zoneColor = Color.cyan;
    
    [Header("Spawn Settings")]
    [Tooltip("Technical - Object spawn radius")]
    [Range(1f, 20f)]
    public float spawnRadius = 3f;
    
    [Tooltip("Technical - Maximum spawn points")]
    [Range(1, 10)]
    public int maxSpawnPoints = 5;
    
    [Tooltip("Technical - Show spawn area in editor")]
    public bool showSpawnArea = true;
    
    [Tooltip("Technical - Obstacle layers to avoid")]
    public LayerMask obstacleLayer = 1;
    
    [Header("Advanced Settings")]
    [Tooltip("Technical - Zone selection priority")]
    [Range(0, 10)]
    public int spawnPriority = 5;
    
    [Tooltip("Technical - Simultaneous object limit")]
    [Range(1, 20)]
    public int maxSimultaneousQuests = 3;
    
    [Header("Debug")]
    [Tooltip("Debug - Show detailed logs")]
    public bool debugMode = false;
    
    // Variables privées (non visibles dans l'Inspector)
    private List<Vector3> spawnPoints = new List<Vector3>();
    private List<GameObject> spawnedObjects = new List<GameObject>();
    
    void Start()
    {
        GenerateSpawnPoints();
        
        // S'enregistre dans le gestionnaire global
        QuestZoneManager.Instance?.RegisterZone(this);
    }
    
    void GenerateSpawnPoints()
    {
        spawnPoints.Clear();
        
        int attempts = 0;
        int maxAttempts = maxSpawnPoints * 3; // Plus de tentatives pour assurer assez de points
        
        while (spawnPoints.Count < maxSpawnPoints && attempts < maxAttempts)
        {
            Vector3 randomPoint = GetRandomPointInZone();
            
            if (IsPointValid(randomPoint))
            {
                // Vérifie aussi la distance avec les autres points
                bool tooClose = false;
                foreach (Vector3 existingPoint in spawnPoints)
                {
                    if (Vector3.Distance(randomPoint, existingPoint) < 1.5f) // Distance minimale entre points
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (!tooClose)
                {
                    spawnPoints.Add(randomPoint);
                }
            }
            
            attempts++;
        }
        
        if (debugMode)
            Debug.Log($"Zone {zoneName}: {spawnPoints.Count} points de spawn générés en {attempts} tentatives");
    }
    
    Vector3 GetRandomPointInZone()
    {
        // Génère un point aléatoire dans un cercle horizontal
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Trouve la position au sol
        Vector3 groundPosition = FindGroundPosition(randomPoint);
        
        return groundPosition;
    }
    
    Vector3 FindGroundPosition(Vector3 position)
    {
        // NOUVEAU : Ignore le collider de cette zone
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.enabled = false; // Désactive temporairement
        }
        
        // Start higher above the position to ensure we're above any terrain
        Vector3 rayStart = new Vector3(position.x, position.y + 50f, position.z);
        
        // Layer mask pour ignorer certains layers (ajustez selon votre projet)
        int layerMask = ~0; // Tous les layers
        
        // Ignore le layer des triggers si vous en avez un spécifique
        int triggerLayer = LayerMask.NameToLayer("Triggers");
        if (triggerLayer >= 0)
        {
            layerMask &= ~(1 << triggerLayer);
        }
        
        Vector3 resultPosition = position; // Position par défaut
        
        // Raycast down to find ground - IGNORE TRIGGERS
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, layerMask, QueryTriggerInteraction.Ignore))
        {
            // Vérifie que ce n'est pas un trigger (double sécurité)
            if (!hit.collider.isTrigger)
            {
                // Return hit point with small offset above ground
                resultPosition = hit.point + Vector3.up * 0.5f; // Augmenté à 0.5f pour plus de marge
                
                if (debugMode)
                    Debug.Log($"[QuestZone] Sol trouvé à {hit.point} (collider: {hit.collider.name})");
            }
        }
        else
        {
            // Si pas de sol trouvé, essaye depuis plus bas
            rayStart = position + Vector3.up * 5f;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, layerMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.isTrigger)
                {
                    resultPosition = hit.point + Vector3.up * 0.5f;
                }
            }
            else
            {
                // Fallback : utilise la position Y du transform de la zone
                resultPosition = new Vector3(position.x, transform.position.y + 0.5f, position.z);
                Debug.LogWarning($"[QuestZone] Aucun sol trouvé, utilisation de la position de la zone + offset");
            }
        }
        
        // Réactive le collider de la zone
        if (zoneCollider != null)
        {
            zoneCollider.enabled = true;
        }
        
        return resultPosition;
    }
    
    bool IsPointValid(Vector3 point)
    {
        // Vérifie que le point est dans la zone de spawn
        float distance = Vector3.Distance(new Vector3(transform.position.x, point.y, transform.position.z), point);
        if (distance > spawnRadius)
        {
            if (debugMode)
                Debug.Log($"[QuestZone] Point invalide - hors de la zone (distance horizontale: {distance})");
            return false;
        }
        
        // Vérifie qu'il n'y a pas d'obstacles à cette position
        // SEULEMENT si un layer d'obstacle est explicitement défini
        if (obstacleLayer.value != 0) // Si un layer d'obstacle est défini
        {
            Collider[] overlaps = Physics.OverlapSphere(point, 0.5f, obstacleLayer);
            
            if (overlaps.Length > 0)
            {
                if (debugMode)
                {
                    Debug.Log($"[QuestZone] Point invalide - obstacle détecté: {overlaps[0].name} (Layer: {LayerMask.LayerToName(overlaps[0].gameObject.layer)})");
                }
                return false;
            }
        }
        
        if (debugMode)
            Debug.Log($"[QuestZone] Point valide: {point}");
        
        return true;
    }
    
    [ContextMenu("Debug AI Fields")]
    public void DebugAIFields()
    {
        Debug.Log($"=== AI FIELDS for {gameObject.name} ===");
        Debug.Log($"Zone Name: {zoneName}");
        Debug.Log($"Zone Type: {zoneType}");
        Debug.Log($"Description: {description}");
        Debug.Log($"Supported Objects: {string.Join(", ", supportedObjects)}");
        Debug.Log("=====================================");
    }
    
    [ContextMenu("Debug Technical Fields")]
    public void DebugTechnicalFields()
    {
        Debug.Log($"=== TECHNICAL FIELDS for {gameObject.name} ===");
        Debug.Log($"Spawn Radius: {spawnRadius}");
        Debug.Log($"Max Spawn Points: {maxSpawnPoints}");
        Debug.Log($"Generated Points: {spawnPoints.Count}");
        Debug.Log($"Priority: {spawnPriority}");
        Debug.Log("============================================");
    }
    
    public GameObject SpawnQuestObject(GameObject prefab, QuestObjectType objectType)
    {
        if (debugMode)
        {
            Debug.Log($"=== SPAWN DEBUG pour {zoneName} ===");
            Debug.Log($"Type demandé: {objectType}");
            Debug.Log($"Types supportés: {string.Join(", ", supportedObjects)}");
        }
        
        if (!supportedObjects.Contains(objectType))
        {
            Debug.LogWarning($"Zone {zoneName} ne supporte pas le type d'objet {objectType}");
            return null;
        }
        
        if (spawnedObjects.Count >= maxSimultaneousQuests)
        {
            Debug.LogWarning($"Zone {zoneName} a atteint sa limite de {maxSimultaneousQuests} objets simultanés");
            return null;
        }
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"Zone {zoneName} n'a pas de points de spawn disponibles");
            GenerateSpawnPoints();
        }
        
        if (spawnPoints.Count == 0)
        {
            return null;
        }
        
        Vector3 spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject spawnedObject = Instantiate(prefab, spawnPoint, Quaternion.identity);
        spawnedObjects.Add(spawnedObject);
        
        if (debugMode)
            Debug.Log($"Objet spawné avec succès: {spawnedObject.name}");
        return spawnedObject;
    }
    
    public void ClearQuestObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        
        if (debugMode)
            Debug.Log($"Zone {zoneName} nettoyée");
    }
    
    // Méthode utilisée par l'IA
    public QuestZoneInfo GetZoneInfo()
    {
        return new QuestZoneInfo
        {
            name = zoneName,
            type = zoneType,
            description = description,
            supportedObjects = supportedObjects,
            availableSpawnPoints = spawnPoints.Count,
            priority = spawnPriority
        };
    }
    
    void OnDrawGizmosSelected()
    {
        if (showSpawnArea)
        {
            // Zone de spawn
            Gizmos.color = zoneColor;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            // Centre de la zone
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            
            // Points de spawn générés
            if (Application.isPlaying && spawnPoints.Count > 0)
            {
                Gizmos.color = Color.green;
                foreach (Vector3 point in spawnPoints)
                {
                    Gizmos.DrawWireSphere(point, 0.3f);
                }
            }
        }
    }
    
    void OnDestroy()
    {
        QuestZoneManager.Instance?.UnregisterZone(this);
        ClearQuestObjects();
    }
}

[System.Serializable]
public class QuestZoneInfo
{
    public string name;
    public QuestZoneType type;
    public string description;
    public List<QuestObjectType> supportedObjects;
    public int availableSpawnPoints;
    public int priority;
}