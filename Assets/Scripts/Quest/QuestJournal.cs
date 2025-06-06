using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public enum QuestStatus
{
    InProgress,
    Completed,
    Failed,
    Cancelled
}

[System.Serializable]
public class JournalQuest
{
    public string questId;
    public string questTitle;
    public string description;
    public string giverNPCName;
    public QuestStatus status;
    public QuestType questType;
    public string zoneName;
    public int currentProgress;
    public int maxProgress;
    
    public JournalQuest(QuestToken token, string npcName)
    {
        questId = token.questId;
        questTitle = "Mission: " + token.description;
        description = token.description;
        giverNPCName = npcName;
        status = QuestStatus.InProgress;
        questType = token.questType;
        zoneName = token.zoneName;
        currentProgress = 0;
        maxProgress = token.quantity;
    }
    
    public string GetProgressText()
    {
        if (maxProgress > 1)
            return $"{currentProgress}/{maxProgress}";
        else
            return status == QuestStatus.Completed ? "Terminé" : "En cours";
    }
    
    public string GetStatusText()
    {
        switch (status)
        {
            case QuestStatus.InProgress: return "En cours";
            case QuestStatus.Completed: return "Terminée";
            case QuestStatus.Failed: return "Échouée";
            case QuestStatus.Cancelled: return "Annulée";
            default: return "Inconnu";
        }
    }
    
    public Color GetStatusColor()
    {
        switch (status)
        {
            case QuestStatus.InProgress: return Color.yellow;
            case QuestStatus.Completed: return Color.green;
            case QuestStatus.Failed: return Color.red;
            case QuestStatus.Cancelled: return Color.gray;
            default: return Color.white;
        }
    }
}

public class QuestJournal : MonoBehaviour
{
    public static QuestJournal Instance { get; private set; }
    
    [Header("Quest Tracking")]
    public List<JournalQuest> allQuests = new List<JournalQuest>();
    
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
    
    void Start()
    {
        Debug.Log($"🎯 QuestJournal Instance créée: {Instance != null}");
        Debug.Log($"🎯 Debug mode: {debugMode}");
    }
    
    public void AddQuest(QuestToken token, string npcName)
    {
        JournalQuest newQuest = new JournalQuest(token, npcName);
        allQuests.Add(newQuest);
        
        if (debugMode)
            Debug.Log($"📔 Quête ajoutée au journal: {newQuest.questTitle} (de {npcName})");
    }


    public void UpdateQuestProgress(string questId, int newProgress)
    {
        JournalQuest quest = allQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            quest.currentProgress = newProgress;
            
            if (debugMode)
                Debug.Log($"📊 Progression mise à jour: {quest.questTitle} ({newProgress}/{quest.maxProgress})");
            
            // NOUVEAU: Notification visuelle si objectifs accomplis
            if (newProgress >= quest.maxProgress && quest.status == QuestStatus.InProgress)
            {
                Debug.Log($"🎯 Objectifs accomplis pour: {quest.questTitle} - Retournez voir {quest.giverNPCName} !");
            }
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"⚠️ Quête introuvable pour mise à jour: {questId}");
        }
    }


    
    public void CompleteQuest(string questId)
    {
        JournalQuest quest = allQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            quest.status = QuestStatus.Completed;
            quest.currentProgress = quest.maxProgress;
            
            if (debugMode)
                Debug.Log($"✅ Quête terminée: {quest.questTitle}");
        }
    }
    
    public List<JournalQuest> GetActiveQuests()
    {
        return allQuests.Where(q => q.status == QuestStatus.InProgress).ToList();
    }
    
    public List<JournalQuest> GetCompletedQuests()
    {
        return allQuests.Where(q => q.status == QuestStatus.Completed).ToList();
    }
    
    public List<JournalQuest> GetCancelledQuests()
    {
        return allQuests.Where(q => q.status == QuestStatus.Cancelled).ToList();
    }
    
    public List<JournalQuest> GetQuestsByStatus(QuestStatus status)
    {
        return allQuests.Where(q => q.status == status).ToList();
    }
    
    [ContextMenu("Show All Quests")]
    public void ShowAllQuests()
    {
        Debug.Log($"=== JOURNAL DE QUÊTES ({allQuests.Count} total) ===");
        foreach (JournalQuest quest in allQuests)
        {
            Debug.Log($"{quest.questTitle} - {quest.GetStatusText()} - {quest.GetProgressText()} (de {quest.giverNPCName})");
        }
    }


}