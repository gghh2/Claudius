using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum QuestZoneType
{
    Laboratory,
    Hangar,
    Market,
    Ruins,
    SecurityArea,
    Storage,
    Residential,
    Engineering,
    Bridge,
    MedicalBay
}

[System.Serializable]
public enum QuestObjectType
{
    Item,
    NPC,
    InteractableObject,
    Marker
}

public class QuestZone : MonoBehaviour
{
    [Header("Zone Identity")]
    public string zoneName = "Zone Sans Nom";
    public QuestZoneType zoneType = QuestZoneType.Laboratory;
    
    [Header("Zone Description")]
    [TextArea(3, 5)]
    public string description = "Description de la zone";
    
    [Header("Supported Quest Objects")]
    public List<QuestObjectType> supportedObjects = new List<QuestObjectType>();
    
    [Header("Spawn Settings")]
    [Range(1f, 20f)]
    public float spawnRadius = 5f;
    [Range(1, 10)]
    public int maxSpawnPoints = 5;
    public LayerMask obstacleLayer = 0;
    
    [Header("Visual")]
    public Color zoneColor = Color.cyan;
    public bool showSpawnArea = true;
    
    // Private
    private List<Vector3> spawnPoints = new List<Vector3>();
    private List<GameObject> spawnedObjects = new List<GameObject>();
    
    void Start()
    {
        GenerateSpawnPoints();
        QuestZoneManager.Instance?.RegisterZone(this);
    }
    
    void GenerateSpawnPoints()
    {
        spawnPoints.Clear();
        
        int attempts = 0;
        int maxAttempts = maxSpawnPoints * 3;
        
        while (spawnPoints.Count < maxSpawnPoints && attempts < maxAttempts)
        {
            Vector3 randomPoint = GetRandomPointInZone();
            
            if (IsPointValid(randomPoint))
            {
                // Vérifie la distance avec les autres points
                bool tooClose = false;
                foreach (Vector3 existingPoint in spawnPoints)
                {
                    if (Vector3.Distance(randomPoint, existingPoint) < 1.5f)
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
    }
    
    Vector3 GetRandomPointInZone()
    {
        // Génère un point aléatoire dans un cercle horizontal
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        return FindGroundPosition(randomPoint);
    }
    
    Vector3 FindGroundPosition(Vector3 position)
    {
        // Désactive temporairement le collider de la zone
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
            zoneCollider.enabled = false;
        
        Vector3 rayStart = new Vector3(position.x, position.y + 50f, position.z);
        Vector3 resultPosition = position;
        
        // Raycast pour trouver le sol
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.isTrigger)
            {
                resultPosition = hit.point + Vector3.up * 0.5f;
            }
        }
        else
        {
            // Fallback : utilise la position Y de la zone
            resultPosition = new Vector3(position.x, transform.position.y + 0.5f, position.z);
        }
        
        // Réactive le collider
        if (zoneCollider != null)
            zoneCollider.enabled = true;
        
        return resultPosition;
    }
    
    bool IsPointValid(Vector3 point)
    {
        // Vérifie que le point est dans la zone de spawn (distance horizontale)
        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z), 
            new Vector3(point.x, 0, point.z)
        );
        
        if (distance > spawnRadius)
            return false;
        
        // Vérifie les obstacles si un layer est défini
        if (obstacleLayer.value != 0)
        {
            Collider[] overlaps = Physics.OverlapSphere(point, 0.5f, obstacleLayer);
            if (overlaps.Length > 0)
                return false;
        }
        
        return true;
    }
    
    public GameObject SpawnQuestObject(GameObject prefab, QuestObjectType objectType)
    {
        if (!supportedObjects.Contains(objectType))
        {
            Debug.LogWarning($"Zone {zoneName} ne supporte pas le type {objectType}");
            return null;
        }
        
        if (spawnPoints.Count == 0)
        {
            GenerateSpawnPoints();
            if (spawnPoints.Count == 0)
                return null;
        }
        
        Vector3 spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject spawnedObject = Instantiate(prefab, spawnPoint, Quaternion.identity);
        spawnedObjects.Add(spawnedObject);
        
        return spawnedObject;
    }
    
    public void ClearQuestObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }
    
    public QuestZoneInfo GetZoneInfo()
    {
        return new QuestZoneInfo
        {
            name = zoneName,
            type = zoneType,
            description = description,
            supportedObjects = supportedObjects
        };
    }
    
    void OnDrawGizmosSelected()
    {
        if (showSpawnArea)
        {
            Gizmos.color = zoneColor;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            
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
}
