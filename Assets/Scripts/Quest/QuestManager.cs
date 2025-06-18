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

/// <summary>
/// Gestionnaire principal du système de quêtes
/// Version factorisée et optimisée
/// </summary>
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
    
    [Header("Quest Sounds")]
    [Tooltip("Sound played when a quest starts")]
    public AudioClip questStartSound;
    
    [Tooltip("Sound played when a quest is completed")]
    public AudioClip questCompleteSound;
    
    [Tooltip("Sound played when collecting a quest item")]
    public AudioClip questItemCollectSound;
    
    [Tooltip("Sound played when cancelling a quest")]
    public AudioClip questCancelSound;
    
    [Header("Sound Volume Settings")]
    [Tooltip("Volume for quest start sound")]
    [Range(0f, 1f)]
    public float questStartVolume = QuestSystemConfig.DefaultQuestStartVolume;
    
    [Tooltip("Volume for quest complete sound")]
    [Range(0f, 1f)]
    public float questCompleteVolume = QuestSystemConfig.DefaultQuestCompleteVolume;
    
    [Tooltip("Volume for quest item collect sound")]
    [Range(0f, 1f)]
    public float questItemCollectVolume = QuestSystemConfig.DefaultQuestItemCollectVolume;
    
    [Tooltip("Volume for quest cancel sound")]
    [Range(0f, 1f)]
    public float questCancelVolume = QuestSystemConfig.DefaultQuestCancelVolume;
    
    [Header("Quest Management")]
    [Tooltip("Technical - Active quest list")]
    public List<ActiveQuest> activeQuests = new List<ActiveQuest>();
    
    [Tooltip("Technical - Maximum concurrent quests")]
    public int maxActiveQuests = QuestSystemConfig.DefaultMaxActiveQuests;
    
    [Header("Debug")]
    [Tooltip("Debug - Show detailed logs")]
    public bool debugMode = true;
    
    // Audio source for playing sounds
    private AudioSource audioSource;
    
    #region Unity Lifecycle
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    #endregion
    
    #region Quest Creation
    
    /// <summary>
    /// Crée une nouvelle quête à partir d'un token
    /// </summary>
    public bool CreateQuestFromToken(QuestToken token, string giverNPCName)
    {
        if (!CanCreateNewQuest())
            return false;
        
        debugMode.LogQuest("[QUEST] Création de quête: {0}", token.description);
        
        ActiveQuest newQuest = new ActiveQuest(token, giverNPCName);
        
        bool success = CreateQuestByType(newQuest);
        
        if (success)
        {
            FinalizeQuestCreation(newQuest, token, giverNPCName);
        }
        
        return success;
    }
    
    bool CanCreateNewQuest()
    {
        if (activeQuests.Count >= maxActiveQuests)
        {
            Debug.LogWarning("Nombre maximum de quêtes actives atteint !");
            return false;
        }
        return true;
    }
    
    bool CreateQuestByType(ActiveQuest quest)
    {
        switch (quest.questData.questType)
        {
            case QuestType.FETCH:
                return CreateFetchQuest(quest);
            case QuestType.DELIVERY:
                return CreateDeliveryQuest(quest);
            case QuestType.EXPLORE:
                return CreateExploreQuest(quest);
            case QuestType.TALK:
                return CreateTalkQuest(quest);
            case QuestType.INTERACT:
                return CreateInteractQuest(quest);
            case QuestType.ESCORT:
                return CreateEscortQuest(quest);
            default:
                Debug.LogError($"Type de quête non supporté: {quest.questData.questType}");
                return false;
        }
    }
    
    void FinalizeQuestCreation(ActiveQuest quest, QuestToken token, string giverNPCName)
    {
        activeQuests.Add(quest);
        
        if (QuestJournal.Instance != null)
        {
            QuestJournal.Instance.AddQuest(token, giverNPCName);
        }
        else
        {
            Debug.LogError("[QUEST] QuestJournal.Instance est NULL !");
        }
        
        debugMode.LogQuest(QuestSystemConfig.QuestCreatedMessage, token.questId);
        PlaySound(questStartSound, questStartVolume);
    }
    
    #endregion
    
    #region Quest Type Creation Methods - Factorisées
    
    bool CreateFetchQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        // Validation de la quantité
        QuestManagerHelper.ValidateQuantity(token, debugMode);
        
        // Trouve la zone
        QuestZone targetZone = QuestManagerHelper.GetQuestZone(token, QuestObjectType.Item, debugMode);
        if (targetZone == null) return false;
        
        quest.SetTargetZone(targetZone);
        
        // Spawn les objets
        debugMode.LogQuest("[FETCH] Spawning {0} items of type {1}", token.quantity, token.objectName);
        
        for (int i = 0; i < token.quantity; i++)
        {
            GameObject spawnedItem = targetZone.SpawnQuestObject(itemPrefab, QuestObjectType.Item);
            if (spawnedItem != null)
            {
                QuestManagerHelper.ConfigureQuestObject(spawnedItem, quest, token.objectName, QuestObjectType.Item);
                debugMode.LogQuest("[FETCH] Item {0}/{1} spawned successfully", i + 1, token.quantity);
            }
        }
        
        return quest.spawnedObjects.Count > 0;
    }
    
    bool CreateDeliveryQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        debugMode.LogQuest("[DELIVERY] Création quête livraison: {0} à {1}", token.objectName, token.targetName);
        
        // Ajoute l'objet à l'inventaire
        if (!AddItemToInventory(token.objectName, 1, quest.questId))
            return false;
        
        // Trouve la zone
        QuestZone targetZone = QuestManagerHelper.GetQuestZone(token, QuestObjectType.NPC, debugMode);
        if (targetZone == null) return false;
        
        quest.SetTargetZone(targetZone);
        
        // Spawn le NPC destinataire
        GameObject deliveryNPC = targetZone.SpawnQuestObject(npcPrefab, QuestObjectType.NPC);
        if (deliveryNPC != null)
        {
            QuestManagerHelper.ConfigureQuestObject(deliveryNPC, quest, token.targetName, QuestObjectType.NPC, true);
            
            string description = $"Attend la livraison de {token.objectName} de la part de {quest.giverNPCName}";
            QuestManagerHelper.ConfigureNPCComponent(deliveryNPC, token.targetName, 
                QuestSystemConfig.DeliveryNPCRole, description, debugMode);
            
            // Ajoute un indicateur visuel
            NPCNameDisplay nameDisplay = deliveryNPC.GetComponent<NPCNameDisplay>();
            if (nameDisplay != null)
            {
                nameDisplay.SetDisplayName($"{token.targetName} (Destinataire)");
            }
            
            return true;
        }
        
        return false;
    }
    
    bool CreateExploreQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        debugMode.LogQuest("[EXPLORE] Création quête exploration: {0}", token.zoneName);
        
        if (markerPrefab == null)
        {
            Debug.LogError("[EXPLORE] markerPrefab est NULL !");
            return false;
        }
        
        QuestZone targetZone = QuestManagerHelper.GetQuestZone(token, QuestObjectType.Marker, debugMode);
        if (targetZone == null) return false;
        
        quest.SetTargetZone(targetZone);
        
        GameObject marker = targetZone.SpawnQuestObject(markerPrefab, QuestObjectType.Marker);
        if (marker != null)
        {
            QuestManagerHelper.ConfigureQuestObject(marker, quest, token.zoneName, QuestObjectType.Marker);
            return true;
        }
        
        return false;
    }
    
    bool CreateTalkQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        debugMode.LogQuest("[TALK] Création quête dialogue: parler à {0}", token.targetName);
        
        QuestZone targetZone = QuestManagerHelper.GetQuestZone(token, QuestObjectType.NPC, debugMode);
        if (targetZone == null) return false;
        
        quest.SetTargetZone(targetZone);
        
        GameObject npc = targetZone.SpawnQuestObject(npcPrefab, QuestObjectType.NPC);
        if (npc != null)
        {
            QuestManagerHelper.ConfigureQuestObject(npc, quest, token.targetName, QuestObjectType.NPC);
            
            string description = $"Une personne importante pour votre quête. {quest.giverNPCName} vous a demandé de lui parler.";
            QuestManagerHelper.ConfigureNPCComponent(npc, token.targetName, 
                QuestSystemConfig.TalkNPCRole, description, debugMode);
            
            return true;
        }
        
        return false;
    }
    
    bool CreateInteractQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        debugMode.LogQuest("[INTERACT] Création quête interaction: {0}", token.objectName);
        
        QuestZone targetZone = QuestManagerHelper.GetQuestZone(token, QuestObjectType.InteractableObject, debugMode);
        if (targetZone == null) return false;
        
        quest.SetTargetZone(targetZone);
        
        GameObject terminal = targetZone.SpawnQuestObject(terminalPrefab, QuestObjectType.InteractableObject);
        if (terminal != null)
        {
            QuestManagerHelper.ConfigureQuestObject(terminal, quest, token.objectName, QuestObjectType.InteractableObject);
            
            // Si c'est un NPC interactif
            if (terminal.GetComponent<NPC>() != null)
            {
                string description = "Un terminal ou objet interactif pour votre quête.";
                QuestManagerHelper.ConfigureNPCComponent(terminal, token.objectName, 
                    QuestSystemConfig.InteractNPCRole, description, debugMode);
            }
            
            return true;
        }
        
        return false;
    }
    
    bool CreateEscortQuest(ActiveQuest quest)
    {
        // TODO: Implémentation spécifique pour l'escorte
        return CreateTalkQuest(quest);
    }
    
    #endregion
    
    #region Quest Events
    
    /// <summary>
    /// Appelé quand un objet de quête est collecté
    /// </summary>
    public void OnObjectCollected(string questId, string objectName)
    {
        ActiveQuest quest = GetActiveQuest(questId);
        if (quest == null) return;
        
        quest.currentProgress++;
        debugMode.LogQuest(QuestSystemConfig.QuestProgressMessage, questId, quest.currentProgress, quest.questData.quantity);
        
        UpdateQuestProgress(quest);
        
        if (IsQuestObjectivesComplete(quest))
        {
            HandleQuestObjectivesComplete(quest);
        }
    }
    
    /// <summary>
    /// Appelé quand un objet de quête est interagi
    /// </summary>
    public void OnObjectInteracted(string questId, string objectName)
    {
        ActiveQuest quest = GetActiveQuest(questId);
        if (quest == null) return;
        
        switch (quest.questData.questType)
        {
            case QuestType.DELIVERY:
                Debug.Log("[QUEST] Quête DELIVERY - En attente de livraison via UI");
                break;
            case QuestType.TALK:
                CompleteQuestWithoutDestruction(quest);
                break;
            case QuestType.FETCH:
                OnObjectCollected(questId, objectName);
                break;
            default:
                CompleteQuest(quest);
                break;
        }
    }
    
    /// <summary>
    /// Appelé quand un marqueur est exploré
    /// </summary>
    public void OnMarkerExplored(string questId, string objectName)
    {
        ActiveQuest quest = GetActiveQuest(questId);
        if (quest != null)
        {
            CompleteQuest(quest);
        }
    }
    
    #endregion
    
    #region Quest Completion
    
    /// <summary>
    /// Complète une quête et nettoie les ressources
    /// </summary>
    void CompleteQuest(ActiveQuest quest)
    {
        quest.isCompleted = true;
        Debug.Log($"[QUEST] Quête terminée: {quest.questData.description}");
        
        UpdateJournalAndUI(quest.questId);
        PlaySound(questCompleteSound, questCompleteVolume);
        CleanupCompletedQuest(quest.questId);
    }
    
    /// <summary>
    /// Complète une quête sans détruire les objets (pour TALK)
    /// </summary>
    void CompleteQuestWithoutDestruction(ActiveQuest quest)
    {
        quest.isCompleted = true;
        Debug.Log($"[QUEST] Quête terminée (sans destruction): {quest.questData.description}");
        
        UpdateJournalAndUI(quest.questId);
        PlaySound(questCompleteSound, questCompleteVolume);
        
        // Retire de la liste sans détruire
        activeQuests.Remove(quest);
        
        // Met à jour le suivi
        if (QuestJournal.Instance != null)
        {
            QuestJournal.Instance.UpdateTrackedQuestAfterCompletion(quest.questId);
        }
    }
    
    /// <summary>
    /// Nettoie une quête complétée
    /// </summary>
    public void CleanupCompletedQuest(string questId)
    {
        ActiveQuest quest = GetActiveQuest(questId);
        if (quest == null) return;
        
        // Nettoie les objets
        foreach (GameObject obj in quest.spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        
        // Nettoie le mapping
        ActiveQuestExtensions.ClearZoneMapping(questId);
        
        // Retire de la liste
        activeQuests.Remove(quest);
        
        // Met à jour le suivi
        if (QuestJournal.Instance != null)
        {
            QuestJournal.Instance.UpdateTrackedQuestAfterCompletion(questId);
        }
        
        debugMode.LogQuest(QuestSystemConfig.QuestCleanedMessage, questId);
    }
    
    #endregion
    
    #region Quest Management
    
    /// <summary>
    /// Annule une quête active
    /// </summary>
    public void CancelQuest(string questId)
    {
        ActiveQuest quest = GetActiveQuest(questId);
        if (quest == null) return;
        
        // Nettoie les objets
        foreach (GameObject obj in quest.spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        
        // Nettoie le mapping
        ActiveQuestExtensions.ClearZoneMapping(questId);
        
        // Retire les objets de l'inventaire si nécessaire
        if (quest.questData.questType == QuestType.DELIVERY && PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.RemoveQuestItem(quest.questData.objectName, questId);
        }
        
        activeQuests.Remove(quest);
        debugMode.LogQuest("[QUEST] Quête annulée: {0}", questId);
        PlaySound(questCancelSound, questCancelVolume);
    }
    
    /// <summary>
    /// Nettoie toutes les quêtes actives
    /// </summary>
    public void ClearAllQuests()
    {
        foreach (ActiveQuest quest in activeQuests)
        {
            foreach (GameObject obj in quest.spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
        }
        
        activeQuests.Clear();
        Debug.Log("Toutes les quêtes ont été nettoyées");
    }
    
    #endregion
    
    #region Helper Methods
    
    ActiveQuest GetActiveQuest(string questId)
    {
        return activeQuests.FirstOrDefault(q => q.questId == questId);
    }
    
    bool AddItemToInventory(string itemName, int quantity, string questId)
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddItem(itemName, quantity, questId);
            debugMode.LogQuest("[INVENTORY] Objet '{0}' ajouté", itemName);
            return true;
        }
        
        Debug.LogError("[INVENTORY] PlayerInventory.Instance non trouvé !");
        return false;
    }
    
    void UpdateQuestProgress(ActiveQuest quest)
    {
        if (QuestJournal.Instance != null)
        {
            QuestJournal.Instance.UpdateQuestProgress(quest.questId, quest.currentProgress);
        }
        
        PlaySound(questItemCollectSound, questItemCollectVolume);
        
        if (QuestMarkerSystem.Instance != null)
        {
            QuestMarkerSystem.Instance.RefreshMarkers();
        }
    }
    
    bool IsQuestObjectivesComplete(ActiveQuest quest)
    {
        return quest.currentProgress >= quest.questData.quantity;
    }
    
    void HandleQuestObjectivesComplete(ActiveQuest quest)
    {
        Debug.Log(string.Format(QuestSystemConfig.QuestCompletedMessage, quest.giverNPCName));
        
        // Pour les quêtes FETCH à 1 objet, ajoute à l'inventaire
        if (quest.questData.questType == QuestType.FETCH && quest.questData.quantity == 1)
        {
            AddItemToInventory(quest.questData.objectName, 1, quest.questId);
        }
    }
    
    void UpdateJournalAndUI(string questId)
    {
        if (QuestJournal.Instance != null)
        {
            QuestJournal.Instance.CompleteQuest(questId);
        }
        
        if (QuestMarkerSystem.Instance != null)
        {
            QuestMarkerSystem.Instance.RefreshMarkers();
        }
    }
    
    void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    #endregion
    
    #region Public Sound Methods
    
    public void PlayQuestCompleteSoundPublic()
    {
        PlaySound(questCompleteSound, questCompleteVolume);
    }
    
    #endregion
    
    #region Debug
    
    [ContextMenu("Debug AI Fields")]
    public void DebugAIFields()
    {
        Debug.Log("=== AI PREFABS for QuestManager ===");
        Debug.Log($"Item Prefab: {(itemPrefab != null ? itemPrefab.name : "NOT SET")}");
        Debug.Log($"NPC Prefab: {(npcPrefab != null ? npcPrefab.name : "NOT SET")}");
        Debug.Log($"Terminal Prefab: {(terminalPrefab != null ? terminalPrefab.name : "NOT SET")}");
        Debug.Log($"Marker Prefab: {(markerPrefab != null ? markerPrefab.name : "NOT SET")}");
        Debug.Log("====================================");
    }
    
    #endregion
}
