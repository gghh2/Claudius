using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestZoneManager : MonoBehaviour
{
    public static QuestZoneManager Instance { get; private set; }
    
    [Header("Zone Management")]
    private List<QuestZone> allZones = new List<QuestZone>();
    
    [Header("Debug")]
    public bool debugMode = true;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void RegisterZone(QuestZone zone)
    {
        if (!allZones.Contains(zone))
        {
            allZones.Add(zone);
            if (debugMode)
                Debug.Log($"Zone enregistrée: {zone.zoneName} ({zone.zoneType})");
        }
    }
    
    public void UnregisterZone(QuestZone zone)
    {
        allZones.Remove(zone);
        if (debugMode)
            Debug.Log($"Zone désenregistrée: {zone.zoneName}");
    }
    
    // Trouve une zone compatible pour un type de quête
    public QuestZone GetRandomZoneByType(QuestZoneType zoneType)
    {
        List<QuestZone> compatibleZones = allZones.Where(z => z.zoneType == zoneType).ToList();
        
        if (compatibleZones.Count > 0)
        {
            return compatibleZones[Random.Range(0, compatibleZones.Count)];
        }
        
        return null;
    }
    
    // Trouve une zone qui supporte un type d'objet
    public QuestZone GetRandomZoneForObject(QuestObjectType objectType)
    {
        List<QuestZone> compatibleZones = allZones.Where(z => z.supportedObjects.Contains(objectType)).ToList();
        
        if (compatibleZones.Count > 0)
        {
            return compatibleZones[Random.Range(0, compatibleZones.Count)];
        }
        
        return null;
    }
    
    // Récupère toutes les zones disponibles (pour l'IA)
    public List<QuestZoneInfo> GetAllZoneInfos()
    {
        List<QuestZoneInfo> zoneInfos = new List<QuestZoneInfo>();
        
        foreach (QuestZone zone in allZones)
        {
            zoneInfos.Add(zone.GetZoneInfo());
        }
        
        return zoneInfos;
    }
    
    // Nettoie toutes les quêtes actives
    public void ClearAllQuestObjects()
    {
        foreach (QuestZone zone in allZones)
        {
            zone.ClearQuestObjects();
        }
        
        if (debugMode)
            Debug.Log("Tous les objets de quête ont été nettoyés");
    }
    
    // Debug: affiche les stats des zones
    [ContextMenu("Show Zone Stats")]
    public void ShowZoneStats()
    {
        Debug.Log($"=== ZONES DE QUÊTE ({allZones.Count}) ===");
        foreach (QuestZone zone in allZones)
        {
            Debug.Log($"{zone.zoneName} ({zone.zoneType}) - {zone.supportedObjects.Count} types d'objets supportés");
        }
    }
    [ContextMenu("List All Zones")]
	public void ListAllZones()
	{
	    Debug.Log($"=== ZONES ENREGISTRÉES ({allZones.Count}) ===");
	    foreach (QuestZone zone in allZones)
	    {
	        Debug.Log($"- {zone.zoneName} ({zone.zoneType})");
	    }
	}
}