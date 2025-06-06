using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum QuestZoneType
{
    [Header("Zones Scientifiques")]
    Laboratory,     // Laboratoire scientifique
    MedicalBay,     // Infirmerie
    
    [Header("Zones Techniques")]
    Hangar,         // Hangar à vaisseaux
    Engineering,    // Salle des machines
    
    [Header("Zones Sociales")]
    Market,         // Zone commerciale
    Residential,    // Zone résidentielle
    
    [Header("Zones Spéciales")]
    Ruins,          // Ruines anciennes
    SecurityArea,   // Zone de sécurité
    Storage,        // Entrepôts
    Bridge,         // Pont de commandement
}

[System.Serializable]
public enum QuestObjectType
{
    Item,               // Objet à ramasser
    NPC,                // PNJ temporaire
    InteractableObject, // Terminal, console, etc.
    Marker              // Simple marqueur visuel
}

// Nouvelle classe pour les présets de configuration
[System.Serializable]
public class QuestZonePreset
{
    public string presetName = "Nouveau Preset";
    public QuestZoneType zoneType;
    public Color zoneColor;
    public float spawnRadius = 3f;
    public int maxSpawnPoints = 5;
    public List<QuestObjectType> supportedObjects;
    [TextArea(2, 4)]
    public string description;
}

public class QuestZone : MonoBehaviour
{
    [Header("Zone Configuration")]
    public string zoneName = "Zone Sans Nom";
    
    [Header("Quick Setup")]
    [Tooltip("Sélectionnez un preset pour configurer rapidement la zone")]
    public ZonePresetType quickPreset = ZonePresetType.Custom;
    
    [Header("Zone Type")]
    [Tooltip("Type de zone pour la génération de quêtes")]
    public QuestZoneType zoneType = QuestZoneType.Laboratory;
    
    [Header("Visual Settings")]
    [ColorUsage(true, true)]
    public Color zoneColor = Color.cyan;
    
    [Header("Spawn Settings")]
    [Range(1f, 20f)]
    public float spawnRadius = 3f;
    [Range(1, 10)]
    public int maxSpawnPoints = 5;
    public bool showSpawnArea = true;
    public LayerMask obstacleLayer = 1;
    
    [Header("Supported Quest Types")]
    [Tooltip("Types d'objets de quête supportés dans cette zone")]
    public List<QuestObjectType> supportedObjects = new List<QuestObjectType>();
    
    [Header("Zone Description")]
    [TextArea(2, 4)]
    public string description = "Description de la zone pour l'IA";
    
    [Header("Advanced Settings")]
    [Tooltip("Priorité de spawn (zones avec priorité haute sont choisies en premier)")]
    [Range(0, 10)]
    public int spawnPriority = 5;
    
    [Tooltip("Nombre maximum d'objets de quête simultanés dans cette zone")]
    [Range(1, 20)]
    public int maxSimultaneousQuests = 3;
    
    private List<Vector3> spawnPoints = new List<Vector3>();
    private List<GameObject> spawnedObjects = new List<GameObject>();
    
    [Header("Debug")]
    public bool debugMode = false;
    
    // Enum pour les presets
    public enum ZonePresetType
    {
        Custom,
        ScientificLab,
        TechHangar,
        MarketPlace,
        SecurityPost,
        AncientRuins,
        MedicalFacility,
        StorageArea
    }
    
    void OnValidate()
    {
        // Applique automatiquement les presets
        if (quickPreset != ZonePresetType.Custom)
        {
            ApplyPreset(quickPreset);
            quickPreset = ZonePresetType.Custom; // Reset pour éviter la réapplication
        }
    }
    
    void ApplyPreset(ZonePresetType preset)
    {
        switch (preset)
        {
            case ZonePresetType.ScientificLab:
                zoneType = QuestZoneType.Laboratory;
                zoneColor = new Color(0.3f, 0.8f, 1f); // Bleu science
                spawnRadius = 4f;
                maxSpawnPoints = 5;
                supportedObjects = new List<QuestObjectType> { 
                    QuestObjectType.Item, 
                    QuestObjectType.InteractableObject 
                };
                description = "Laboratoire de recherche avec équipements scientifiques avancés";
                break;
                
            case ZonePresetType.TechHangar:
                zoneType = QuestZoneType.Hangar;
                zoneColor = new Color(1f, 0.6f, 0.2f); // Orange tech
                spawnRadius = 8f;
                maxSpawnPoints = 7;
                supportedObjects = new List<QuestObjectType> { 
                    QuestObjectType.Item, 
                    QuestObjectType.NPC,
                    QuestObjectType.InteractableObject 
                };
                description = "Hangar technique avec vaisseaux et équipements de maintenance";
                break;
                
            case ZonePresetType.MarketPlace:
                zoneType = QuestZoneType.Market;
                zoneColor = new Color(1f, 0.9f, 0.3f); // Jaune commerce
                spawnRadius = 6f;
                maxSpawnPoints = 8;
                supportedObjects = new List<QuestObjectType> { 
                    QuestObjectType.Item, 
                    QuestObjectType.NPC 
                };
                description = "Zone commerciale animée avec marchands et échanges";
                break;
                
            case ZonePresetType.SecurityPost:
                zoneType = QuestZoneType.SecurityArea;
                zoneColor = new Color(1f, 0.2f, 0.2f); // Rouge sécurité
                spawnRadius = 3f;
                maxSpawnPoints = 4;
                supportedObjects = new List<QuestObjectType> { 
                    QuestObjectType.InteractableObject,
                    QuestObjectType.NPC 
                };
                description = "Poste de sécurité avec systèmes de surveillance";
                break;
                
            case ZonePresetType.AncientRuins:
                zoneType = QuestZoneType.Ruins;
                zoneColor = new Color(0.6f, 0.5f, 0.4f); // Marron ancien
                spawnRadius = 10f;
                maxSpawnPoints = 6;
                supportedObjects = new List<QuestObjectType> { 
                    QuestObjectType.Item,
                    QuestObjectType.Marker 
                };
                description = "Ruines anciennes mystérieuses avec artefacts cachés";
                break;
                
            case ZonePresetType.MedicalFacility:
                zoneType = QuestZoneType.MedicalBay;
                zoneColor = new Color(0.9f, 1f, 0.9f); // Vert médical clair
                spawnRadius = 4f;
                maxSpawnPoints = 5;
                supportedObjects = new List<QuestObjectType> { 
                    QuestObjectType.Item,
                    QuestObjectType.InteractableObject 
                };
                description = "Centre médical avec équipements de soin avancés";
                break;
                
            case ZonePresetType.StorageArea:
                zoneType = QuestZoneType.Storage;
                zoneColor = new Color(0.5f, 0.5f, 0.5f); // Gris stockage
                spawnRadius = 5f;
                maxSpawnPoints = 10;
                supportedObjects = new List<QuestObjectType> { 
                    QuestObjectType.Item 
                };
                description = "Zone de stockage avec conteneurs et marchandises";
                break;
        }
        
        Debug.Log($"✅ Preset '{preset}' appliqué à la zone '{zoneName}'");
    }
    
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
            
            // Vérifie qu'il n'y a pas d'obstacle
            if (IsPointValid(randomPoint))
            {
                spawnPoints.Add(randomPoint);
            }
        }
        
        Debug.Log($"Zone {zoneName}: {spawnPoints.Count} points de spawn générés");
    }
    
    Vector3 GetRandomPointInZone()
    {
        // Génère un point aléatoire dans une sphère 3D
        Vector3 randomSphere = Random.insideUnitSphere * spawnRadius;
        Vector3 worldPoint = transform.position + randomSphere;
        
        // Trouve la position au sol
        Vector3 groundPosition = FindGroundPosition(worldPoint);
        
        return groundPosition;
    }
    
    Vector3 FindGroundPosition(Vector3 position)
    {
        // Lance un raycast vers le bas depuis une position haute
        Vector3 rayStart = new Vector3(position.x, transform.position.y + 20f, position.z);
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 30f))
        {
            // Si on touche le sol, retourne cette position + un petit offset
            return hit.point + Vector3.up * 0.1f;
        }
        else
        {
            // Fallback : utilise la position Y de la zone mais un peu plus bas
            return new Vector3(position.x, transform.position.y - 1f, position.z);
        }
    }
    
    bool IsPointValid(Vector3 point)
    {
        // Pour l'instant, tous les points sont valides
        // Tu peux ajouter des vérifications plus complexes ici
        return true;
    }
    
    [ContextMenu("Debug Spawn Points")]
    public void DebugSpawnPoints()
    {
        Debug.Log($"=== DEBUG ZONE {zoneName} ===");
        Debug.Log($"Type: {zoneType}");
        Debug.Log($"Position: {transform.position}");
        Debug.Log($"Spawn Radius: {spawnRadius}");
        Debug.Log($"Max Spawn Points: {maxSpawnPoints}");
        Debug.Log($"Points générés: {spawnPoints.Count}");
        Debug.Log($"Objets supportés: {string.Join(", ", supportedObjects)}");
        Debug.Log($"Priorité: {spawnPriority}");
        
        // Force la régénération
        GenerateSpawnPoints();
    }
    
    public GameObject SpawnQuestObject(GameObject prefab, QuestObjectType objectType)
    {
        Debug.Log($"=== SPAWN DEBUG pour {zoneName} ===");
        Debug.Log($"Type demandé: {objectType}");
        Debug.Log($"Types supportés: {string.Join(", ", supportedObjects)}");
        
        if (!supportedObjects.Contains(objectType))
        {
            Debug.LogWarning($"Zone {zoneName} ne supporte pas le type d'objet {objectType}");
            return null;
        }
        
        // Vérifie la limite d'objets simultanés
        if (spawnedObjects.Count >= maxSimultaneousQuests)
        {
            Debug.LogWarning($"Zone {zoneName} a atteint sa limite de {maxSimultaneousQuests} objets simultanés");
            return null;
        }
        
        Debug.Log($"Points de spawn disponibles: {spawnPoints.Count}");
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"Zone {zoneName} n'a pas de points de spawn disponibles");
            Debug.Log("Tentative de régénération des points...");
            GenerateSpawnPoints();
            Debug.Log($"Après régénération: {spawnPoints.Count} points");
        }
        
        if (spawnPoints.Count == 0)
        {
            return null;
        }
        
        // Choisit un point de spawn aléatoire
        Vector3 spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Debug.Log($"Point de spawn choisi: {spawnPoint}");
        
        // Spawn l'objet
        GameObject spawnedObject = Instantiate(prefab, spawnPoint, Quaternion.identity);
        spawnedObjects.Add(spawnedObject);
        
        Debug.Log($"Objet spawné avec succès: {spawnedObject.name}");
        return spawnedObject;
    }
    
    // Nettoie tous les objets de quête de cette zone
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
        
        Debug.Log($"Zone {zoneName} nettoyée");
    }
    
    // Récupère les infos de la zone pour l'IA
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
    
    // Visualisation dans l'éditeur
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
        // Se désenregistre du gestionnaire
        QuestZoneManager.Instance?.UnregisterZone(this);
        
        // Nettoie les objets de quête
        ClearQuestObjects();
    }
}

// Structure pour passer les infos de zone à l'IA
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