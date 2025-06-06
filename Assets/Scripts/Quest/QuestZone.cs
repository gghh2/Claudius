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
    Item,          // Objet à ramasser
    NPC,           // PNJ temporaire
    InteractableObject, // Terminal, console, etc.
    Marker         // Simple marqueur visuel
}

public class QuestZone : MonoBehaviour
{
    [Header("Zone Configuration")]
    public string zoneName = "Zone Sans Nom";
    public QuestZoneType zoneType = QuestZoneType.Laboratory;
    public Color zoneColor = Color.cyan;
    
    [Header("Spawn Settings")]
    public float spawnRadius = 3f;
    public int maxSpawnPoints = 5;
    public bool showSpawnArea = true;
    public LayerMask obstacleLayer = 1; // Pour éviter de spawn dans les murs
    
    [Header("Supported Quest Types")]
    public List<QuestObjectType> supportedObjects = new List<QuestObjectType>();
    
    [Header("Zone Description")]
    [TextArea(2, 4)]
    public string description = "Description de la zone pour l'IA";
    
    private List<Vector3> spawnPoints = new List<Vector3>();
    private List<GameObject> spawnedObjects = new List<GameObject>();
    

      [ContextMenu("Debug Spawn Points")]
		public void DebugSpawnPoints()
		{
		    Debug.Log($"=== DEBUG ZONE {zoneName} ===");
		    Debug.Log($"Position: {transform.position}");
		    Debug.Log($"Spawn Radius: {spawnRadius}");
		    Debug.Log($"Max Spawn Points: {maxSpawnPoints}");
		    Debug.Log($"Points générés: {spawnPoints.Count}");
		    Debug.Log($"Objets supportés: {string.Join(", ", supportedObjects)}");
		    
		    // Force la régénération
		    GenerateSpawnPoints();
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
	    // NOUVELLE VERSION : Génère un point aléatoire dans une sphère 3D
	    Vector3 randomSphere = Random.insideUnitSphere * spawnRadius;
	    Vector3 worldPoint = transform.position + randomSphere;
	    
	    // Trouve la position au sol
	    Vector3 groundPosition = FindGroundPosition(worldPoint);
	    
	    return groundPosition;
	}

	// Améliore aussi cette méthode pour être plus tolérante
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

	// Améliore la validation des points
	bool IsPointValid(Vector3 point)


	{

		return true;
	    /*
	    // Vérifie qu'il n'y a pas d'obstacle à cette position (plus tolérant)
	    Collider[] obstacles = Physics.OverlapSphere(point, 0.3f, obstacleLayer);
	    bool isValid = obstacles.Length == 0;
	    
	    // Debug pour voir ce qui bloque
	    if (!isValid && debugMode)
	    {
	        Debug.Log($"Point invalide {point}: {obstacles.Length} obstacles détectés");
	    }
	    
	    return isValid;*/
	}

	// Ajoute une variable debug
	[Header("Debug")]
	public bool debugMode = false;
    
    // Remplace la méthode SpawnQuestObject existante par celle-ci :
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
            availableSpawnPoints = spawnPoints.Count
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
	}



