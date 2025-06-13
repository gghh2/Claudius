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
    public float questStartVolume = 0.5f;
    
    [Tooltip("Volume for quest complete sound")]
    [Range(0f, 1f)]
    public float questCompleteVolume = 0.5f;
    
    [Tooltip("Volume for quest item collect sound")]
    [Range(0f, 1f)]
    public float questItemCollectVolume = 0.3f;
    
    [Tooltip("Volume for quest cancel sound")]
    [Range(0f, 1f)]
    public float questCancelVolume = 0.4f;
    
    [Header("Quest Management")]
    [Tooltip("Technical - Active quest list")]
    public List<ActiveQuest> activeQuests = new List<ActiveQuest>();
    
    [Tooltip("Technical - Maximum concurrent quests")]
    public int maxActiveQuests = 5;
    
    [Header("Debug")]
    [Tooltip("Debug - Show detailed logs")]
    public bool debugMode = true;
    
    // Audio source for playing sounds
    private AudioSource audioSource;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Get or add AudioSource component
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
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
            
            // Play quest start sound
            PlayQuestStartSound();
            
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
        
        // Si on ne trouve pas de zone qui supporte le type d'objet, on prend quand même la zone
        // et on laisse SpawnQuestObject gérer l'erreur
        if (targetZone == null && token.zoneType.HasValue)
        {
            Debug.LogWarning($"[FETCH] Aucune zone de type {token.zoneType} trouvée, recherche alternative...");
            // On cherche n'importe quelle zone du bon type
            var allZones = FindObjectsOfType<QuestZone>();
            targetZone = allZones.FirstOrDefault(z => z.zoneType == token.zoneType.Value);
        }
        
        if (targetZone == null)
        {
            Debug.LogError($"[FETCH] Aucune zone de type {token.zoneType} supportant {QuestObjectType.Item} trouvée pour: {token.zoneName}");
            Debug.LogError($"[FETCH] Vérifiez que les zones ont bien 'Item' dans leur liste supportedObjects dans l'Inspector");
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
        QuestToken token = quest.questData;
        
        if (debugMode)
            Debug.Log($"[DELIVERY] Création quête livraison: {token.objectName} à {token.targetName} dans {token.zoneName}");
        
        // 1. Ajoute l'objet à livrer dans l'inventaire du joueur
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddItem(token.objectName, 1, quest.questId);
            
            if (debugMode)
                Debug.Log($"[DELIVERY] Objet '{token.objectName}' ajouté à l'inventaire du joueur");
        }
        else
        {
            Debug.LogError("[DELIVERY] PlayerInventory.Instance non trouvé !");
            return false;
        }
        
        // 2. Trouve la zone de destination
        QuestZone targetZone = null;
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        if (targetZone == null)
        {
            Debug.LogError($"[DELIVERY] Aucune zone de type {token.zoneType} supportant {QuestObjectType.NPC} trouvée pour: {token.zoneName}");
            Debug.LogError($"[DELIVERY] Vérifiez que les zones ont bien 'NPC' dans leur liste supportedObjects dans l'Inspector");
            return false;
        }
        
        // 3. Spawn le NPC destinataire
        GameObject deliveryNPC = targetZone.SpawnQuestObject(npcPrefab, QuestObjectType.NPC);
        if (deliveryNPC != null)
        {
            // Configure le NPC destinataire
            QuestObject questObj = deliveryNPC.GetComponent<QuestObject>();
            if (questObj == null)
                questObj = deliveryNPC.AddComponent<QuestObject>();
            
            questObj.questId = quest.questId;
            questObj.objectName = token.targetName;
            questObj.objectType = QuestObjectType.NPC;
            questObj.isDeliveryTarget = true; // Flag pour identifier ce NPC comme destinataire
            
            // Configure le composant NPC avec les bonnes informations
            NPC npcComponent = deliveryNPC.GetComponent<NPC>();
            if (npcComponent != null)
            {
                npcComponent.npcName = token.targetName;
                npcComponent.npcRole = "Destinataire";
                npcComponent.npcDescription = $"Attend la livraison de {token.objectName} de la part de {quest.giverNPCName}";
                
                if (debugMode)
                    Debug.Log($"[DELIVERY] NPC configuré: {npcComponent.npcName} - {npcComponent.npcDescription}");
            }
            
            // Configure le nom visible du NPC
            NPCNameDisplay nameDisplay = deliveryNPC.GetComponent<NPCNameDisplay>();
            if (nameDisplay == null)
                nameDisplay = deliveryNPC.AddComponent<NPCNameDisplay>();
            
            // Ajoute un marqueur visuel pour le destinataire
            if (nameDisplay != null)
            {
                nameDisplay.SetDisplayName(token.targetName + " (Destinataire)");
            }
            
            quest.spawnedObjects.Add(deliveryNPC);
            
            if (debugMode)
                Debug.Log($"[DELIVERY] NPC destinataire '{token.targetName}' créé dans {targetZone.zoneName}");
            
            return true;
        }
        
        return false;
    }
    
    bool CreateExploreQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        if (debugMode)
            Debug.Log($"[EXPLORE] Création quête exploration: {token.zoneName}");
        
        QuestZone targetZone = null;
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        if (targetZone == null)
        {
            Debug.LogError($"[EXPLORE] Aucune zone de type {token.zoneType} supportant {QuestObjectType.Marker} trouvée pour: {token.zoneName}");
            Debug.LogError($"[EXPLORE] Vérifiez que les zones ont bien 'Marker' dans leur liste supportedObjects dans l'Inspector");
            return false;
        }
        
        if (markerPrefab == null)
        {
            Debug.LogError("[EXPLORE] markerPrefab est NULL dans QuestManager ! Assignez le prefab dans l'Inspector.");
            return false;
        }
        
        GameObject marker = targetZone.SpawnQuestObject(markerPrefab, QuestObjectType.Marker);
        if (marker != null)
        {
            QuestObject questObj = marker.GetComponent<QuestObject>();
            if (questObj == null)
            {
                questObj = marker.AddComponent<QuestObject>();
                Debug.Log("[EXPLORE] QuestObject ajouté au marqueur");
            }
            
            questObj.questId = quest.questId;
            questObj.objectName = token.zoneName;
            questObj.objectType = QuestObjectType.Marker;
            questObj.triggerRadius = 3f; // Assure un rayon suffisant
            questObj.explorationTimeRequired = 2f; // 2 secondes pour valider
            
            // Vérifie que le GameObject a les bons composants
            Collider collider = marker.GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogWarning("[EXPLORE] Pas de collider sur le marqueur ! Ajout d'un SphereCollider...");
                SphereCollider sphere = marker.AddComponent<SphereCollider>();
                sphere.radius = 1f;
                sphere.isTrigger = false; // Le trigger sera sur l'enfant
            }
            
            quest.spawnedObjects.Add(marker);
            
            if (debugMode)
                Debug.Log($"[EXPLORE] Marqueur créé à la position: {marker.transform.position}");
            
            return true;
        }
        else
        {
            Debug.LogError("[EXPLORE] Échec de la création du marqueur");
        }
        
        return false;
    }
    
    bool CreateTalkQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        if (debugMode)
            Debug.Log($"[TALK] Création quête dialogue: parler à {token.targetName} dans {token.zoneName}");
        
        QuestZone targetZone = null;
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        if (targetZone == null)
        {
            Debug.LogError($"[TALK] Aucune zone de type {token.zoneType} supportant {QuestObjectType.NPC} trouvée pour: {token.zoneName}");
            Debug.LogError($"[TALK] Vérifiez que les zones ont bien 'NPC' dans leur liste supportedObjects dans l'Inspector");
            return false;
        }
        
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
            
            // NOUVEAU: Configure le composant NPC avec le bon nom
            NPC npcComponent = npc.GetComponent<NPC>();
            if (npcComponent != null)
            {
                npcComponent.npcName = token.targetName;
                npcComponent.npcRole = "Informateur"; // Ou un rôle approprié
                npcComponent.npcDescription = $"Une personne importante pour votre quête. {quest.giverNPCName} vous a demandé de lui parler.";
                
                if (debugMode)
                    Debug.Log($"[TALK] NPC configuré: {npcComponent.npcName}");
            }
            else
            {
                Debug.LogWarning("[TALK] Pas de composant NPC sur le prefab ! Le nom ne sera pas affiché correctement.");
            }
            
            // Configure l'affichage du nom
            NPCNameDisplay nameDisplay = npc.GetComponent<NPCNameDisplay>();
            if (nameDisplay == null)
            {
                nameDisplay = npc.AddComponent<NPCNameDisplay>();
            }
            
            // Force la mise à jour du nom affiché
            if (nameDisplay != null)
            {
                nameDisplay.SetDisplayName(token.targetName);
                if (debugMode)
                    Debug.Log($"[TALK] Nom affiché configuré: {token.targetName}");
            }
            
            quest.spawnedObjects.Add(npc);
            return true;
        }
        
        return false;
    }
    
    bool CreateInteractQuest(ActiveQuest quest)
    {
        QuestToken token = quest.questData;
        
        if (debugMode)
            Debug.Log($"[INTERACT] Création quête interaction: {token.objectName} dans {token.zoneName}");
        
        QuestZone targetZone = null;
        if (token.zoneType.HasValue)
        {
            targetZone = QuestZoneManager.Instance?.GetRandomZoneByType(token.zoneType.Value);
        }
        
        if (targetZone == null)
        {
            Debug.LogError($"[INTERACT] Aucune zone de type {token.zoneType} supportant {QuestObjectType.InteractableObject} trouvée pour: {token.zoneName}");
            Debug.LogError($"[INTERACT] Vérifiez que les zones ont bien 'InteractableObject' dans leur liste supportedObjects dans l'Inspector");
            return false;
        }
        
        GameObject terminal = targetZone.SpawnQuestObject(terminalPrefab, QuestObjectType.InteractableObject);
        if (terminal != null)
        {
            QuestObject questObj = terminal.GetComponent<QuestObject>();
            if (questObj == null)
                questObj = terminal.AddComponent<QuestObject>();
            
            questObj.questId = quest.questId;
            questObj.objectName = token.objectName;
            questObj.objectType = QuestObjectType.InteractableObject;
            
            // NOUVEAU: Si c'est un NPC interactif, configure son nom
            NPC npcComponent = terminal.GetComponent<NPC>();
            if (npcComponent != null)
            {
                npcComponent.npcName = token.objectName;
                npcComponent.npcRole = "Terminal";
                npcComponent.npcDescription = $"Un terminal ou objet interactif pour votre quête.";
                
                // Force la mise à jour de l'affichage du nom
                NPCNameDisplay nameDisplay = terminal.GetComponent<NPCNameDisplay>();
                if (nameDisplay != null)
                {
                    nameDisplay.SetDisplayName(token.objectName);
                }
            }
            
            quest.spawnedObjects.Add(terminal);
            
            if (debugMode)
                Debug.Log($"[INTERACT] Objet interactif créé: {token.objectName}");
            
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
            
            // Play quest item collect sound
            PlayQuestItemCollectSound();
            
            // Refresh quest markers
            if (QuestMarkerSystem.Instance != null)
            {
                QuestMarkerSystem.Instance.RefreshMarkers();
            }
            
            // Message quand tous les objets sont collectés
            if (quest.currentProgress >= quest.questData.quantity)
            {
                Debug.Log($"[QUEST] OBJECTIFS ACCOMPLIS ! Retournez voir {quest.giverNPCName} pour rendre la quête.");
                
                // NOUVEAU: Pour les quêtes FETCH à 1 objet, on ajoute l'objet à l'inventaire
                if (quest.questData.questType == QuestType.FETCH && quest.questData.quantity == 1)
                {
                    if (PlayerInventory.Instance != null)
                    {
                        PlayerInventory.Instance.AddItem(quest.questData.objectName, 1, quest.questId);
                        Debug.Log($"[QUEST] Objet ajouté à l'inventaire: {quest.questData.objectName}");
                    }
                }
                
                // OPTIONNEL : Afficher un message UI temporaire
                // ShowTemporaryMessage($"Tous les {objectName} collectés ! Retournez voir {quest.giverNPCName}");
            }
        }
    }
    
    public void OnObjectInteracted(string questId, string objectName)
    {
        ActiveQuest quest = activeQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            // Pour les quêtes DELIVERY, ne pas auto-compléter
            if (quest.questData.questType == QuestType.DELIVERY)
            {
                // Ne rien faire - la complétion se fera via le bouton UI
                Debug.Log($"[QUEST] Quête DELIVERY détectée - En attente de livraison via UI");
                return;
            }
            
            // Pour les quêtes TALK, compléter mais ne pas détruire le NPC
            if (quest.questData.questType == QuestType.TALK)
            {
                CompleteQuestWithoutDestruction(quest);
                return;
            }
            
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
    
    // NOUVELLE MÉTHODE: Complète la quête sans détruire les objets (pour TALK)
    void CompleteQuestWithoutDestruction(ActiveQuest quest)
    {
        quest.isCompleted = true;
        
        Debug.Log($"[QUEST] Quête terminée (sans destruction): {quest.questData.description}");
        
        // Met à jour le journal
        if (QuestJournal.Instance != null)
        {
            QuestJournal.Instance.CompleteQuest(quest.questId);
        }
        
        // Play quest complete sound
        PlayQuestCompleteSound();
        
        // Refresh quest markers
        if (QuestMarkerSystem.Instance != null)
        {
            QuestMarkerSystem.Instance.RefreshMarkers();
        }
        
        // Retire de la liste SANS détruire les objets
        activeQuests.Remove(quest);
        
        if (debugMode)
            Debug.Log($"[QUEST] Quête nettoyée (NPCs conservés): {quest.questId}");
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
        
        // Play quest complete sound
        PlayQuestCompleteSound();
        
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
    
    public void CancelQuest(string questId)
    {
        ActiveQuest quest = activeQuests.FirstOrDefault(q => q.questId == questId);
        if (quest != null)
        {
            // Détruit tous les objets spawnés pour cette quête
            foreach (GameObject obj in quest.spawnedObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            
            // Retire les objets de quête de l'inventaire si c'était une quête DELIVERY
            if (quest.questData.questType == QuestType.DELIVERY && PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.RemoveQuestItem(quest.questData.objectName, questId);
            }
            
            // Retire de la liste des quêtes actives
            activeQuests.Remove(quest);
            
            if (debugMode)
                Debug.Log($"[QUEST] Quête annulée et nettoyée: {questId}");
            
            // Play quest cancel sound
            PlayQuestCancelSound();
        }
    }
    
    void PlayQuestStartSound()
    {
        if (questStartSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(questStartSound, questStartVolume);
        }
    }
    
    void PlayQuestCompleteSound()
    {
        if (questCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(questCompleteSound, questCompleteVolume);
        }
    }
    
    void PlayQuestItemCollectSound()
    {
        if (questItemCollectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(questItemCollectSound, questItemCollectVolume);
        }
    }
    
    void PlayQuestCancelSound()
    {
        if (questCancelSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(questCancelSound, questCancelVolume);
        }
    }
    
    // Public method to play complete sound when quest is turned in
    public void PlayQuestCompleteSoundPublic()
    {
        PlayQuestCompleteSound();
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