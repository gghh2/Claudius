using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestZoneManager : MonoBehaviour
{
    public static QuestZoneManager Instance { get; private set; }
    
    [Header("Zone Management")]
    private List<QuestZone> allZones = new List<QuestZone>();
    
    // Debug est maintenant géré par GlobalDebugManager
    
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
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                Debug.Log($"Zone enregistrée: {zone.zoneName} ({zone.zoneType})");
        }
    }
    
    public void UnregisterZone(QuestZone zone)
    {
        allZones.Remove(zone);
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
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
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
            Debug.Log("Tous les objets de quête ont été nettoyés");
    }
    
    // Get zones that support a specific object type
    public List<QuestZone> GetZonesSupportingObjectType(QuestObjectType objectType)
    {
        return allZones.Where(z => z.supportedObjects.Contains(objectType)).ToList();
    }
    
    // Get available quest types for AI generation
    public Dictionary<QuestType, List<QuestZone>> GetAvailableQuestOptions()
    {
        Dictionary<QuestType, List<QuestZone>> availableOptions = new Dictionary<QuestType, List<QuestZone>>();
        
        // Map quest types to required object types
        Dictionary<QuestType, QuestObjectType> questTypeMapping = new Dictionary<QuestType, QuestObjectType>
        {
            { QuestType.FETCH, QuestObjectType.Item },
            { QuestType.DELIVERY, QuestObjectType.NPC },
            { QuestType.EXPLORE, QuestObjectType.Marker },
            { QuestType.TALK, QuestObjectType.NPC },
            { QuestType.INTERACT, QuestObjectType.InteractableObject },
            { QuestType.ESCORT, QuestObjectType.NPC }
        };
        
        foreach (var mapping in questTypeMapping)
        {
            var supportingZones = GetZonesSupportingObjectType(mapping.Value);
            if (supportingZones.Count > 0)
            {
                availableOptions[mapping.Key] = supportingZones;
            }
        }
        
        return availableOptions;
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
    
    [ContextMenu("Debug Quest Availability")]
    public void DebugQuestAvailability()
    {
        Debug.Log("=== DISPONIBILITÉ DES QUÊTES PAR TYPE ===");
        
        var availableOptions = GetAvailableQuestOptions();
        
        if (availableOptions.Count == 0)
        {
            Debug.LogWarning("AUCUNE QUÊTE DISPONIBLE! Vérifiez la configuration des zones.");
            return;
        }
        
        foreach (var kvp in availableOptions)
        {
            Debug.Log($"\n{kvp.Key}: {kvp.Value.Count} zone(s) disponible(s)");
            foreach (var zone in kvp.Value)
            {
                Debug.Log($"  - {zone.zoneName} ({zone.zoneType})");
            }
        }
        
        // Show quest types that are NOT available
        Debug.Log("\n=== TYPES DE QUÊTES NON DISPONIBLES ===");
        foreach (QuestType questType in System.Enum.GetValues(typeof(QuestType)))
        {
            if (!availableOptions.ContainsKey(questType))
            {
                Debug.LogWarning($"{questType} - Aucune zone ne supporte ce type!");
            }
        }
    }
}
