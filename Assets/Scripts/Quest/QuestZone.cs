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
    
    // Debug est maintenant géré par GlobalDebugManager
    
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
        
        for (int i = 0; i < maxSpawnPoints; i++)
        {
            Vector3 randomPoint = GetRandomPointInZone();
            
            if (IsPointValid(randomPoint))
            {
                spawnPoints.Add(randomPoint);
            }
        }
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
            Debug.Log($"Zone {zoneName}: {spawnPoints.Count} points de spawn générés");
    }
    
    Vector3 GetRandomPointInZone()
    {
        Vector3 randomSphere = Random.insideUnitSphere * spawnRadius;
        Vector3 worldPoint = transform.position + randomSphere;
        
        Vector3 groundPosition = FindGroundPosition(worldPoint);
        
        return groundPosition;
    }
    
    Vector3 FindGroundPosition(Vector3 position)
    {
        Vector3 rayStart = new Vector3(position.x, transform.position.y + 20f, position.z);
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 30f))
        {
            return hit.point + Vector3.up * 0.1f;
        }
        else
        {
            return new Vector3(position.x, transform.position.y - 1f, position.z);
        }
    }
    
    bool IsPointValid(Vector3 point)
    {
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
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
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
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
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
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
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