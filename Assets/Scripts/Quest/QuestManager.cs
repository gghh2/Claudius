using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ActiveQuest
{
    public string questId;
    public QuestToken questData;
    public bool isCompleted = false;
    public int currentProgress = 0;
    public List<GameObject> spawnedObjects = new List<GameObject>();
    public string giverNPCName;
    
    public ActiveQuest(QuestToken token, string npcName)
    {
        questId = token.questId;
        questData = token;
        giverNPCName = npcName;
    }
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    
    [Header("===== AI CONFIGURATION - Used by AI System =====")]
    
    [Header("Quest Object Prefabs (AI)")]
    [Tooltip("AI SYSTEM - Prefab for collectible items")]
    public GameObject itemPrefab;
    
    [Tooltip("AI SYSTEM - Prefab for temporary NPCs")]
    public GameObject npcPrefab;
    
    [Tooltip("AI SYSTEM - Prefab for interactive terminals")]
    public GameObject terminalPrefab;
    
    [Tooltip("AI SYSTEM - Prefab for exploration markers")]
    public GameObject markerPrefab;
    
    [Space(20)]
    [Header("===== TECHNICAL CONFIGURATION - Not used by AI =====")]
    
    [Header("Quest Management")]
    [Tooltip("Technical - Active quest list")]
    public List<ActiveQuest> activeQuests = new List<ActiveQuest>();
    
    [Tooltip("Technical - Maximum concurrent quests")]
    public int maxActiveQuests = 5;
    
    [Header("Debug")]
    [Tooltip("Debug - Show detailed logs")]
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
    
    public bool CreateQuestFromToken(QuestToken token, string giverNPCName)
    {
        if (activeQuests.Count >= maxActiveQuests)
        {
            Debug.LogWarning("Nombre maximum de quêtes actives atteint !");
            return false;
        }
        
        if (debugMode)
            Debug.Log($"[QUEST] Création de quête: {token.description}");
        
        ActiveQuest newQuest = new ActiveQuest(token, giverNPCName);
        
        // Génère la quête selon son type
        bool success = false;
        switch (token.questType)
        {
            case QuestType.FETCH:
                success = CreateFetchQuest(newQuest);
                break;
            case QuestType.DELIVERY:
                success = CreateDeliveryQuest(newQuest);
                break;
            case QuestType.EXPLORE:
                success = CreateExploreQuest(newQuest);
                break;
            case QuestType.TALK:
                success = CreateTalkQuest(newQuest);
                break;
            case QuestType.INTERACT:
                success = CreateInteractQuest(newQuest);
                break;
            case QuestType.ESCORT:
                success = CreateEscortQuest(newQuest);
                break;
        }
        
        if (success)
        {
            activeQuests.Add(newQuest);
            
            // DEBUG : Vérification du QuestJournal
            Debug.Log($"[QUEST] QuestJournal.Instance existe: {QuestJournal.Instance != null}");
            
            if (QuestJournal.Instance != null)
            {
                Debug.Log($"[QUEST] Tentative d'ajout au journal...");
                QuestJournal.Instance.AddQuest(token, giverNPCName);
            }
            else
            {
                Debug.LogError("[QUEST] QuestJournal.Instance est NULL ! Crée un GameObject QuestJournal dans la scène.");
            }
            
            if (debugMode)
                Debug.Log($"[QUEST] Quête créée avec succès: {token.questId}");
            
            return true;
        }
        
        return false;
    }
    
    bool CreateFetchQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        // Trouve une zone compatible
        QuestZone targetZone = null;
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        if (targetZone == null)
        {
            Debug.LogError($"Aucune zone trouvée pour {token.zoneName}");
            return false;
        }
        
        // Spawn les objets à collecter
        for (int i = 0; i < token.quantity; i++)
        {
            GameObject spawnedItem = targetZone.SpawnQuestObject(itemPrefab, QuestObjectType.Item);
            
            if (spawnedItem != null)
            {
                // Configure l'objet de quête
                QuestObject questObj = spawnedItem.GetComponent<QuestObject>();
                if (questObj == null)
                    questObj = spawnedItem.AddComponent<QuestObject>();
                
                questObj.questId = quest.questId;
                questObj.objectName = token.objectName;
                questObj.objectType = QuestObjectType.Item;
                
                quest.spawnedObjects.Add(spawnedItem);
            }
        }
        
        return quest.spawnedObjects.Count > 0;
    }
    
    bool CreateDeliveryQuest(ActiveQuest quest)
    {
        // Pour l'instant, crée juste un NPC de destination
        return CreateTalkQuest(quest);
    }
    
    bool CreateExploreQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        QuestZone targetZone = null;
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        if (targetZone == null)
            return false;
        
        GameObject marker = targetZone.SpawnQuestObject(markerPrefab, QuestObjectType.Marker);
        if (marker != null)
        {
            QuestObject questObj = marker.GetComponent<QuestObject>();
            if (questObj == null)
                questObj = marker.AddComponent<QuestObject>();
            
            questObj.questId = quest.questId;
            questObj.objectName = token.zoneName;
            questObj.objectType = QuestObjectType.Marker;
            
            quest.spawnedObjects.Add(marker);
            return true;
        }
        
        return false;
    }
    
    bool CreateTalkQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        QuestZone targetZone = null;
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        if (targetZone == null)
            return false;
        
        GameObject npc = targetZone.SpawnQuestObject(npcPrefab, QuestObjectType.NPC);
        if (npc != null)
        {
            // Configure le NPC temporaire
            QuestObject questObj = npc.GetComponent<QuestObject>();
            if (questObj == null)
                questObj = npc.AddComponent<QuestObject>();
            
            questObj.questId = quest.questId;
            questObj.objectName = token.targetName;
            questObj.objectType = QuestObjectType.NPC;
            
            quest.spawnedObjects.Add(npc);
            return true;
        }
        
        return false;
    }
    
    bool CreateInteractQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        QuestZone targetZone = null;
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        if (targetZone == null)
            return false;
        
        GameObject terminal = targetZone.SpawnQuestObject(terminalPrefab, QuestObjectType.InteractableObject);
        if (terminal != null)
        {
            QuestObject questObj = terminal.GetComponent<QuestObject>();
            if (questObj == null)
                questObj = terminal.AddComponent<QuestObject>();
            
            questObj.questId = quest.questId;
            questObj.objectName = token.objectName;
            questObj.objectType = QuestObjectType.InteractableObject;
            
            quest.spawnedObjects.Add(terminal);
            return true;
        }
        
        return false;
    }
    
    bool CreateEscortQuest(ActiveQuest quest)
    {
        // Implémentation plus complexe pour plus tard
        return CreateTalkQuest(quest);
    }
    
    public void OnObjectCollected(string questId, string objectName)
    {
        ActiveQuest quest = activeQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            quest.currentProgress++;
            
            if (debugMode)
                Debug.Log($"[QUEST] Progression quête {questId}: {quest.currentProgress}/{quest.questData.quantity}");
            
            // Met à jour le journal mais NE COMPLETE PAS automatiquement
            if (QuestJournal.Instance != null)
            {
                QuestJournal.Instance.UpdateQuestProgress(questId, quest.currentProgress);
            }
            
            // Message quand tous les objets sont collectés
            if (quest.currentProgress >= quest.questData.quantity)
            {
                Debug.Log($"[QUEST] OBJECTIFS ACCOMPLIS ! Retournez voir {quest.giverNPCName} pour rendre la quête.");
            }
        }
    }
    
    public void OnObjectInteracted(string questId, string objectName)
    {
        ActiveQuest quest = activeQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            // Pour les autres types de quêtes, on peut encore auto-compléter
            if (quest.questData.questType != QuestType.FETCH)
            {
                CompleteQuest(quest);
            }
            else
            {
                OnObjectCollected(questId, objectName); // Même logique que collecte
            }
        }
    }
    
    public void OnMarkerExplored(string questId, string objectName)
    {
        ActiveQuest quest = activeQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            CompleteQuest(quest); // Les quêtes d'exploration se terminent automatiquement
        }
    }
    
    public void CleanupCompletedQuest(string questId)
    {
        ActiveQuest quest = activeQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            // Nettoie les objets restants (si il y en a)
            foreach (GameObject obj in quest.spawnedObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            
            // Retire de la liste des quêtes actives
            activeQuests.Remove(quest);
            
            if (debugMode)
                Debug.Log($"[QUEST] Quête nettoyée: {questId}");
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"[QUEST] Tentative de nettoyage d'une quête introuvable: {questId}");
        }
    }
    
    void CompleteQuest(ActiveQuest quest)
    {
        quest.isCompleted = true;
        
        Debug.Log($"[QUEST] Quête terminée automatiquement: {quest.questData.description}");
        
        // Met à jour le journal
        if (QuestJournal.Instance != null)
        {
            QuestJournal.Instance.CompleteQuest(quest.questId);
        }
        
        // Nettoie
        CleanupCompletedQuest(quest.questId);
    }
    
    public void ClearAllQuests()
    {
        foreach (ActiveQuest quest in activeQuests)
        {
            foreach (GameObject obj in quest.spawnedObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
        }
        
        activeQuests.Clear();
        Debug.Log("Toutes les quêtes ont été nettoyées");
    }
    
    [ContextMenu("Debug AI Fields")]
    public void DebugAIFields()
    {
        Debug.Log($"=== AI PREFABS for QuestManager ===");
        Debug.Log($"Item Prefab: {(itemPrefab != null ? itemPrefab.name : "NOT SET")}");
        Debug.Log($"NPC Prefab: {(npcPrefab != null ? npcPrefab.name : "NOT SET")}");
        Debug.Log($"Terminal Prefab: {(terminalPrefab != null ? terminalPrefab.name : "NOT SET")}");
        Debug.Log($"Marker Prefab: {(markerPrefab != null ? markerPrefab.name : "NOT SET")}");
        Debug.Log("====================================");
    }
}