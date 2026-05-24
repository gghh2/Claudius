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
        // NOUVEAU: Formate la description pour le titre
        questTitle = "Mission: " + TextFormatter.FormatDescription(token.description);
        description = TextFormatter.FormatDescription(token.description);
        giverNPCName = npcName; // IMPORTANT: Ne PAS formater ici, garder le nom original
        status = QuestStatus.InProgress;
        questType = token.questType;
        zoneName = TextFormatter.FormatName(token.zoneName);
        currentProgress = 0;
        maxProgress = token.quantity;
    }
    
    public string GetProgressText()
    {
        // Handle special statuses first
        switch (status)
        {
            case QuestStatus.Completed:
                return "Terminé";
            case QuestStatus.Cancelled:
                return "Annulée";
            case QuestStatus.Failed:
                return "Échouée";
            case QuestStatus.InProgress:
                if (maxProgress > 1)
                    return $"{currentProgress}/{maxProgress}";
                else
                    return "En cours";
            default:
                return "Inconnu";
        }
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
    
    [Header("Active Tracking")]
    [SerializeField] private string trackedQuestId = null;
    
    [Header("Debug")]
    public bool debugMode = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Initialisation
    }
    
    public void AddQuest(QuestToken token, string npcName)
    {
        JournalQuest newQuest = new JournalQuest(token, npcName);
        allQuests.Add(newQuest);
        
        // NOUVEAU : Toujours suivre automatiquement la nouvelle quête
        SetTrackedQuest(newQuest.questId);
        
        // Force le rafraîchissement de l'UI si elle est ouverte
        if (QuestJournalUI.Instance != null && QuestJournalUI.Instance.IsJournalOpen())
        {
            QuestJournalUI.Instance.RefreshCurrentTab();
        }
        
        if (debugMode)
        {
            Debug.Log($"📔 Quête ajoutée au journal: {newQuest.questTitle} (de {npcName})");
            Debug.Log($"📍 Nouvelle quête automatiquement suivie: {newQuest.questTitle}");
        }
    }


    public void UpdateQuestProgress(string questId, int newProgress)
    {
        JournalQuest quest = allQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            quest.currentProgress = newProgress;
            
            if (debugMode)
                Debug.Log($"📊 Progression mise à jour: {quest.questTitle} ({newProgress}/{quest.maxProgress})");
            
            // Notification visuelle si objectifs accomplis — message au joueur.
            if (newProgress >= quest.maxProgress && quest.status == QuestStatus.InProgress)
            {
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.ShowSuccess(
                        $"Objectifs accomplis — retournez voir {TextFormatter.FormatName(quest.giverNPCName)}");
                if (debugMode) Debug.Log($"[Quest] Objectifs accomplis pour: {quest.questTitle}");
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
        if (quest == null)
        {
            Debug.LogError($"[QuestJournal.CompleteQuest] Aucune JournalQuest trouvée pour questId={questId} — pas de récompense crédits. Liste actuelle ({allQuests.Count}) : {string.Join(", ", allQuests.Select(q => q.questId))}");
            return;
        }

        quest.status = QuestStatus.Completed;
        quest.currentProgress = quest.maxProgress;

        Debug.Log($"[QuestJournal.CompleteQuest] questId={questId} type={quest.questType} maxProg={quest.maxProgress}");

        if (debugMode)
            Debug.Log($"✅ Quête terminée: {quest.questTitle}");

        // Log la complétion de la quête dans le Journal d'Aventure
        AdventureJournalExtensions.LogQuestCompleted(quest.description);

        // Récompense en crédits (barème fixé côté jeu, pas par l'IA).
        if (PlayerWallet.Instance != null)
        {
            int reward = QuestRewardScale.GetReward(quest.questType, quest.maxProgress);
            Debug.Log($"[QuestJournal.CompleteQuest] reward={reward} pour {quest.questType}/{quest.maxProgress}");
            if (reward > 0) PlayerWallet.Instance.AddCredits(reward);
        }
        else
        {
            Debug.LogError("[QuestJournal.CompleteQuest] PlayerWallet.Instance est NULL — pas de récompense possible.");
        }

        // Rumeur : les PNJ alentours apprennent que le voyageur a accompli
        // une mission. À la prochaine convo, l'un d'eux peut le mentionner.
        if (RumorPool.Instance != null)
        {
            string giver = TextFormatter.FormatName(quest.giverNPCName ?? "un mystérieux contact");
            RumorPool.Instance.AddRumor("quest_" + quest.questId,
                $"Le voyageur a terminé une mission pour {giver} : {quest.description}.");
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
    
    public JournalQuest GetQuest(string questId)
    {
        return allQuests.FirstOrDefault(q => q.questId == questId);
    }
    
    public void CancelQuest(string questId)
    {
        JournalQuest quest = allQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null && quest.status == QuestStatus.InProgress)
        {
            quest.status = QuestStatus.Cancelled;
            
            if (debugMode)
                Debug.Log($"❌ Quête annulée: {quest.questTitle}");
            
            // Nettoie la quête active dans le QuestManager
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CancelQuest(questId);
            }
            
            // Met à jour la quête suivie si c'était celle-ci
            if (trackedQuestId == questId)
            {
                UpdateTrackedQuestAfterCompletion(questId);
            }
            
            // Force le rafraîchissement de l'UI si elle est ouverte
            if (QuestJournalUI.Instance != null && QuestJournalUI.Instance.IsJournalOpen())
            {
                QuestJournalUI.Instance.RefreshCurrentTab();
            }
        }
    }
    
    [ContextMenu("Show All Quests")]
    public void ShowAllQuests()
    {
        Debug.Log($"=== JOURNAL DE QUÊTES ({allQuests.Count} total) ===");
        foreach (JournalQuest quest in allQuests)
        {
            string tracked = quest.questId == trackedQuestId ? " [SUIVIE]" : "";
            Debug.Log($"{quest.questTitle} - {quest.GetStatusText()} - {quest.GetProgressText()} (de {quest.giverNPCName}){tracked}");
        }
    }
    
    /// <summary>
    /// Check if there's an active quest with a specific NPC
    /// </summary>
    public bool HasActiveQuestWithNPC(string npcName)
    {
        return allQuests.Any(q => q.giverNPCName == npcName && q.status == QuestStatus.InProgress);
    }
    
    /// <summary>
    /// Clear all quests from the journal
    /// </summary>
    public void ClearAllQuests()
    {
        allQuests.Clear();
        trackedQuestId = null;
        
        if (debugMode)
            Debug.Log("📔 Journal de quêtes vidé");
    }
    
    // Nouvelle méthode pour définir la quête suivie
    public void SetTrackedQuest(string questId)
    {
        JournalQuest quest = allQuests.FirstOrDefault(q => q.questId == questId && q.status == QuestStatus.InProgress);
        if (quest != null)
        {
            trackedQuestId = questId;
            if (debugMode)
                Debug.Log($"Quête suivie: {quest.questTitle}");
            
            // Rafraîchir les marqueurs
            if (QuestMarkerSystem.Instance != null)
                QuestMarkerSystem.Instance.RefreshMarkers();
        }
    }
    
    // Récupérer la quête actuellement suivie
    public JournalQuest GetTrackedQuest()
    {
        if (string.IsNullOrEmpty(trackedQuestId))
            return null;
            
        return allQuests.FirstOrDefault(q => q.questId == trackedQuestId && q.status == QuestStatus.InProgress);
    }
    
    // Vérifier si une quête est suivie
    public bool IsQuestTracked(string questId)
    {
        return trackedQuestId == questId;
    }
    
    /// <summary>
    /// Met à jour automatiquement la quête suivie après qu'une quête soit terminée
    /// </summary>
    public void UpdateTrackedQuestAfterCompletion(string completedQuestId)
    {
        // Si la quête terminée était celle suivie
        if (trackedQuestId == completedQuestId)
        {
            // Trouve la première quête active pour la suivre automatiquement
            JournalQuest nextQuest = allQuests.FirstOrDefault(q => 
                q.status == QuestStatus.InProgress && 
                q.questId != completedQuestId);
            
            if (nextQuest != null)
            {
                SetTrackedQuest(nextQuest.questId);
                
                if (debugMode)
                    Debug.Log($"📍 Quête suivante automatiquement suivie: {nextQuest.questTitle}");
            }
            else
            {
                // Plus aucune quête active
                trackedQuestId = null;
                
                if (debugMode)
                    Debug.Log("📍 Plus aucune quête active à suivre");
            }
            
            // Rafraîchir les marqueurs
            if (QuestMarkerSystem.Instance != null)
                QuestMarkerSystem.Instance.RefreshMarkers();
        }
    }


}