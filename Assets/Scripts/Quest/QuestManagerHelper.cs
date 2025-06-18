using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Classe helper pour la factorisation du QuestManager
/// </summary>
public static class QuestManagerHelper
{
    /// <summary>
    /// Configure un GameObject avec un composant QuestObject
    /// </summary>
    public static void ConfigureQuestObject(GameObject obj, ActiveQuest quest, string objectName, 
        QuestObjectType type, bool isDeliveryTarget = false)
    {
        if (obj == null || quest == null) return;
        
        QuestObject questObj = obj.GetComponent<QuestObject>();
        if (questObj == null)
            questObj = obj.AddComponent<QuestObject>();
        
        questObj.questId = quest.questId;
        questObj.objectName = objectName;
        questObj.objectType = type;
        questObj.isDeliveryTarget = isDeliveryTarget;
        
        // Configuration spécifique par type
        if (type == QuestObjectType.Marker)
        {
            questObj.triggerRadius = QuestSystemConfig.DefaultTriggerRadius;
            questObj.explorationTimeRequired = QuestSystemConfig.DefaultExplorationTime;
            
            // Assure qu'il y a un collider
            if (obj.GetComponent<Collider>() == null)
            {
                SphereCollider sphere = obj.AddComponent<SphereCollider>();
                sphere.radius = 1f;
                sphere.isTrigger = false;
            }
        }
        
        quest.spawnedObjects.Add(obj);
    }
    
    /// <summary>
    /// Configure un NPC avec nom, rôle et description
    /// </summary>
    public static void ConfigureNPCComponent(GameObject npcObject, string npcName, 
        string role, string description, bool debugMode = false)
    {
        if (npcObject == null) return;
        
        // Configure le composant NPC
        NPC npcComponent = npcObject.GetComponent<NPC>();
        if (npcComponent != null)
        {
            npcComponent.npcName = npcName;
            npcComponent.npcRole = role;
            npcComponent.npcDescription = description;
            
            if (debugMode)
                Debug.Log($"[NPC] Configuré: {npcComponent.npcName} - {role}");
        }
        
        // Configure l'affichage du nom
        NPCNameDisplay nameDisplay = npcObject.GetComponent<NPCNameDisplay>();
        if (nameDisplay == null)
            nameDisplay = npcObject.AddComponent<NPCNameDisplay>();
        
        if (nameDisplay != null)
            nameDisplay.SetDisplayName(npcName);
    }
    
    /// <summary>
    /// Trouve une zone compatible pour spawner un objet de quête
    /// </summary>
    public static QuestZone GetQuestZone(QuestToken token, QuestObjectType requiredType, bool debugMode = false)
    {
        QuestZone targetZone = null;
        
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        // Fallback : chercher n'importe quelle zone du bon type
        if (targetZone == null && token.zoneType.HasValue)
        {
            if (debugMode)
                Debug.LogWarning($"[QUEST] Aucune zone de type {token.zoneType} trouvée, recherche alternative...");
            
            var allZones = Object.FindObjectsOfType<QuestZone>();
            targetZone = allZones.FirstOrDefault(z => z.zoneType == token.zoneType.Value);
        }
        
        if (targetZone == null)
        {
            Debug.LogError($"[QUEST] Aucune zone de type {token.zoneType} supportant {requiredType} trouvée pour: {token.zoneName}");
            Debug.LogError($"[QUEST] Vérifiez que les zones ont bien '{requiredType}' dans leur liste supportedObjects dans l'Inspector");
        }
        
        return targetZone;
    }
    
    /// <summary>
    /// Vérifie si la description indique une quantité de 1
    /// </summary>
    public static bool DescriptionIndicatesOne(string description)
    {
        if (string.IsNullOrEmpty(description)) return false;
        
        string lowerDesc = description.ToLower();
        return lowerDesc.Contains("un ") || 
               lowerDesc.Contains("une ") ||
               Regex.IsMatch(description, @"\btrouvez 1\b", RegexOptions.IgnoreCase) ||
               (lowerDesc.Contains("1 ") && !description.Contains("10") && !description.Contains("11"));
    }
    
    /// <summary>
    /// Valide et corrige la quantité selon la description
    /// </summary>
    public static void ValidateQuantity(QuestToken token, bool debugMode = false)
    {
        if (DescriptionIndicatesOne(token.description) && token.quantity != 1)
        {
            if (debugMode)
                Debug.LogWarning($"[QUEST] Incohérence détectée ! Description dit UN mais quantité est {token.quantity}. Correction à 1.");
            
            token.quantity = 1;
            token.description = $"Trouvez 1 {token.objectName} dans {token.zoneName}";
        }
    }
}

/// <summary>
/// Extension pour simplifier les messages de debug
/// </summary>
public static class QuestDebugExtensions
{
    public static void LogQuest(this bool debugMode, string message, params object[] args)
    {
        if (debugMode)
            Debug.Log(string.Format(message, args));
    }
    
    public static void LogQuestWarning(this bool debugMode, string message, params object[] args)
    {
        if (debugMode)
            Debug.LogWarning(string.Format(message, args));
    }
    
    public static void LogQuestError(string message, params object[] args)
    {
        Debug.LogError(string.Format(message, args));
    }
}
