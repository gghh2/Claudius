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
    /// Cherche un PNJ existant en scène par nom (normalisé : espaces et casse
    /// ignorés). Utilisé par TALK/DELIVERY pour réutiliser un PNJ déjà
    /// présent comme cible plutôt que d'en spawner un doublon.
    /// </summary>
    public static GameObject FindExistingNPCByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        string normalized = NormalizeName(name);

        foreach (NPC npc in Object.FindObjectsByType<NPC>(FindObjectsSortMode.None))
        {
            if (NormalizeName(npc.npcName) == normalized)
                return npc.gameObject;
        }
        return null;
    }

    static string NormalizeName(string s) =>
        (s ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");

    /// <summary>
    /// Zone la plus proche d'une position monde. Utilisé pour assigner la
    /// targetZone d'une quête qui réutilise un PNJ existant : on prend la
    /// zone enregistrée la plus proche du PNJ pour que les markers UI
    /// pointent vers le bon endroit.
    /// </summary>
    public static QuestZone FindClosestZoneTo(Vector3 worldPos)
    {
        var zones = Object.FindObjectsByType<QuestZone>(FindObjectsSortMode.None);
        QuestZone best = null;
        float bestSq = float.MaxValue;
        foreach (var z in zones)
        {
            float d = (z.transform.position - worldPos).sqrMagnitude;
            if (d < bestSq) { bestSq = d; best = z; }
        }
        return best;
    }

    /// <summary>
    /// Nettoie un PNJ existant qui était réutilisé comme cible de quête :
    /// retire son QuestObject (sans détruire le GameObject) pour qu'il
    /// reprenne son rôle initial.
    /// </summary>
    public static void DetachReusedNPC(GameObject obj)
    {
        if (obj == null) return;
        QuestObject qo = obj.GetComponent<QuestObject>();
        if (qo != null) Object.Destroy(qo);
    }

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
            // Filtre par zoneType ET par supportedObjects pour ne pas tirer une
            // zone du bon type mais qui ne supporte pas l'objet à spawn (sinon
            // SpawnQuestObject retourne null et la quête est perdue).
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByTypeAndObject(
                token.zoneType.Value, requiredType);
        }

        // Fallback 1 : n'importe quelle zone supportant le type d'objet, peu importe son zoneType.
        if (targetZone == null)
        {
            if (debugMode)
                Debug.LogWarning($"[QUEST] Aucune zone de type {token.zoneType} supportant {requiredType} — fallback sur n'importe quelle zone qui supporte {requiredType}.");

            targetZone = QuestZoneManager.Instance?.GetRandomZoneForObject(requiredType);
        }

        if (targetZone == null)
        {
            Debug.LogError($"[QUEST] Aucune zone supportant {requiredType} trouvée (token demandait: {token.zoneName}/{token.zoneType}).");
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
