using UnityEngine;
using System.Linq;
using TMPro;

public class NPCQuestTurnIn : MonoBehaviour
{
    [Header("Quest Turn-In Settings")]
    public float interactionRange = 3f;
    public KeyCode turnInKey = KeyCode.F;
    
    [Header("Visual Feedback")]
    public GameObject turnInPromptPrefab; // Optionnel : UI custom
    public Vector3 promptOffset = new Vector3(0, 3f, 0);
    
    [Header("Debug")]
    public bool debugMode = true;
    
    private NPC npcScript;
    private Transform player;
    private bool playerInRange = false;
    private bool hasCompletableQuest = false;
    private JournalQuest currentCompletableQuest = null;
    
    // UI Prompt
    private GameObject promptDisplay;
    private TextMeshPro promptText;
    private Camera mainCamera;
    
    void Start()
    {
        npcScript = GetComponent<NPC>();
        mainCamera = Camera.main;
        
        // Trouve le joueur
        PlayerControllerCC playerController = FindFirstObjectByType<PlayerControllerCC>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
            Debug.Log($"🔄 NPCQuestTurnIn configuré pour {npcScript.npcName}");
    }
    
    void Update()
    {
        CheckPlayerDistance();
        
        // Interaction avec F pour rendre la quête
        if (playerInRange && hasCompletableQuest && Input.GetKeyDown(turnInKey))
        {
            TryTurnInQuest();
        }
    }
    
    void CheckPlayerDistance()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= interactionRange && !playerInRange)
        {
            playerInRange = true;
            CheckForCompletableQuest();
        }
        else if (distance > interactionRange && playerInRange)
        {
            playerInRange = false;
            HideTurnInPrompt();
            hasCompletableQuest = false;
            currentCompletableQuest = null;
        }
    }
    
    void CheckForCompletableQuest()
    {
        if (QuestJournal.Instance == null || PlayerInventory.Instance == null) 
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                Debug.LogWarning("⚠️ QuestJournal ou PlayerInventory manquant");
            return;
        }
        
        // Cherche une quête active avec ce NPC
        var activeQuests = QuestJournal.Instance.GetActiveQuests();
        var npcQuest = activeQuests.FirstOrDefault(q => q.giverNPCName == npcScript.npcName);
        
        if (npcQuest != null)
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                Debug.Log($"🔍 Vérification quête: {npcQuest.questTitle} - Type: {npcQuest.questType}");
            
            bool canTurnIn = false;
            
            // Vérifie selon le type de quête
            switch (npcQuest.questType)
            {
                case QuestType.FETCH:
                    // NOUVEAU: Les quêtes FETCH sont gérées par le dialogue avec bouton
                    // On n'affiche plus le prompt [F] pour ces quêtes
                    canTurnIn = false;
                    
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                    {
                        string objectName = ExtractObjectNameFromDescription(npcQuest.description);
                        int currentCount = PlayerInventory.Instance.GetItemQuantity(objectName, npcQuest.questId);
                        Debug.Log($"📦 Quête FETCH détectée - Gérée par dialogue: {currentCount}/{npcQuest.maxProgress} {objectName}");
                    }
                    break;
                    
                case QuestType.EXPLORE:
                case QuestType.TALK:
                case QuestType.INTERACT:
                    // Ces quêtes sont complétées automatiquement lors de l'action
                    // Vérifier la progression
                    canTurnIn = npcQuest.currentProgress >= npcQuest.maxProgress;
                    
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                        Debug.Log($"📍 Progression: {npcQuest.currentProgress}/{npcQuest.maxProgress}");
                    break;
                    
                case QuestType.DELIVERY:
                case QuestType.ESCORT:
                    // Pour l'instant, traiter comme FETCH
                    string deliveryItem = ExtractObjectNameFromDescription(npcQuest.description);
                    canTurnIn = PlayerInventory.Instance.HasItemsForQuest(
                        deliveryItem, 
                        1, 
                        npcQuest.questId
                    );
                    break;
            }
            
            if (canTurnIn)
            {
                hasCompletableQuest = true;
                currentCompletableQuest = npcQuest;
                ShowTurnInPrompt(npcQuest);
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                    Debug.Log($"✅ Quête peut être rendue: {npcQuest.questTitle}");
            }
            else
            {
                hasCompletableQuest = false;
                currentCompletableQuest = null;
                HideTurnInPrompt();
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                    Debug.Log($"❌ Quête non complétée: {npcQuest.questTitle}");
            }
        }
        else
        {
            hasCompletableQuest = false;
            currentCompletableQuest = null;
            HideTurnInPrompt();
        }
    }
    
    void ShowTurnInPrompt(JournalQuest quest)
    {
        // Crée le prompt visuel si pas encore fait
        if (promptDisplay == null)
        {
            CreateTurnInPrompt();
        }
        
        if (promptText != null)
        {
            // NOUVEAU: Formate le titre de la quête
            string formattedTitle = TextFormatter.FormatName(quest.questTitle);
            promptText.text = $"🎯 [F] Rendre la quête\n\"{formattedTitle}\"";
            promptText.color = Color.green;
        }
        
        if (promptDisplay != null)
        {
            promptDisplay.SetActive(true);
        }
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
            Debug.Log($"💫 [F] pour rendre: {quest.questTitle}");
    }
    
    void HideTurnInPrompt()
    {
        if (promptDisplay != null)
        {
            promptDisplay.SetActive(false);
        }
    }
    
    void CreateTurnInPrompt()
    {
        promptDisplay = new GameObject($"{gameObject.name}_TurnInPrompt");
        promptDisplay.transform.SetParent(transform);
        promptDisplay.transform.localPosition = promptOffset;
        
        promptText = promptDisplay.AddComponent<TextMeshPro>();
        promptText.text = "[F] Rendre la quête";
        promptText.fontSize = 4f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        promptText.verticalAlignment = VerticalAlignmentOptions.Middle;
        promptText.color = Color.green;
        promptText.fontStyle = FontStyles.Bold;
        promptText.outlineWidth = 0.3f;
        promptText.outlineColor = Color.black;
        
        promptDisplay.SetActive(false);
    }
    
    void TryTurnInQuest()
    {
        if (currentCompletableQuest == null)
        {
            Debug.LogWarning("⚠️ Pas de quête à rendre");
            return;
        }
        
        // NOUVEAU: Vérification supplémentaire - ignore les quêtes FETCH
        if (currentCompletableQuest.questType == QuestType.FETCH)
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                Debug.Log("🚫 Les quêtes FETCH sont gérées par le dialogue avec bouton");
            return;
        }
        
        bool success = false;
        
        // Traite selon le type de quête
        switch (currentCompletableQuest.questType)
        {
            case QuestType.FETCH:
            case QuestType.DELIVERY:
                string objectName = ExtractObjectNameFromDescription(currentCompletableQuest.description);
                int quantity = currentCompletableQuest.questType == QuestType.FETCH ? 
                    currentCompletableQuest.maxProgress : 1;
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                    Debug.Log($"🎯 Tentative de rendu: {objectName} x{quantity}");
                
                // Retire les objets de l'inventaire
                success = PlayerInventory.Instance.RemoveItem(
                    objectName, 
                    quantity, 
                    currentCompletableQuest.questId
                );
                break;
                
            case QuestType.EXPLORE:
            case QuestType.TALK:
            case QuestType.INTERACT:
                // Ces quêtes n'ont pas d'objets à retirer
                success = true;
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                    Debug.Log($"🎯 Rendu de quête {currentCompletableQuest.questType}");
                break;
                
            case QuestType.ESCORT:
                // TODO: Vérifier que l'escorte est terminée
                success = true;
                break;
        }
        
        if (success)
        {
            // Complete la quête dans le journal
            QuestJournal.Instance.CompleteQuest(currentCompletableQuest.questId);
            
            // Play quest complete sound
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.PlayQuestCompleteSoundPublic();
            }
            
            // Nettoie la quête active dans le QuestManager
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CleanupCompletedQuest(currentCompletableQuest.questId);
            }
            
            // Affiche le message de succès
            ShowQuestCompletionMessage(currentCompletableQuest);
            
            // Reset
            hasCompletableQuest = false;
            currentCompletableQuest = null;
            HideTurnInPrompt();
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                Debug.Log($"🎉 QUÊTE RENDUE AVEC SUCCÈS: {currentCompletableQuest.questTitle}");
        }
        else
        {
            Debug.LogError("❌ Erreur lors du retrait des objets de l'inventaire");
        }
    }
    
    void ShowQuestCompletionMessage(JournalQuest quest)
    {
        string completionMessage = GetCompletionMessage(quest);
        
        // Ouvre automatiquement le dialogue pour afficher le message
        if (DialogueUI.Instance != null)
        {
            if (!DialogueUI.Instance.IsDialogueOpen())
            {
                DialogueUI.Instance.StartDialogue(npcScript.GetNPCData());
            }
            
            DialogueUI.Instance.ShowText(completionMessage);
        }
        else
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                Debug.Log($"💬 {completionMessage}");
        }
    }
    
    string GetCompletionMessage(JournalQuest quest)
    {
        // NOUVEAU: Formate les noms pour l'affichage
        string formattedNPCName = TextFormatter.FormatName(npcScript.npcName);
        string objectName = ExtractObjectNameFromDescription(quest.description);
        string formattedObjectName = TextFormatter.FormatName(objectName);
        
        switch (npcScript.npcRole.ToLower())
        {
            case "marchand":
                return $"{formattedNPCName}: Parfait ! Vous avez récupéré tout ce que je demandais. " +
                       $"Voici votre récompense bien méritée ! Ces {formattedObjectName} " +
                       $"vont me rapporter gros sur le marché.";
            
            case "scientifique":
                return $"{formattedNPCName}: Excellent travail ! Ces spécimens de {formattedObjectName} " +
                       $"vont révolutionner mes recherches. La science vous remercie ! " +
                       $"Vos efforts contribuent à l'avancement de nos connaissances.";
            
            case "garde impérial":
                return $"{formattedNPCName}: Mission accomplie avec brio, voyageur ! " +
                       $"Vous avez récupéré les {formattedObjectName} comme demandé. " +
                       $"L'Empire reconnaît votre efficacité et votre dévouement.";
            
            default:
                return $"{formattedNPCName}: Merci infiniment ! Vous avez accompli exactement ce que je demandais. " +
                       $"Ces {formattedObjectName} me sont très précieux. " +
                       $"C'est un travail formidable !";
        }
    }
    
    // Extrait le nom de l'objet de la description de la quête
    string ExtractObjectNameFromDescription(string description)
    {
        // La description est maintenant formatée, donc on doit chercher avec une casse insensible
        // Format attendu: "Trouvez X objet name dans zone"
        string[] words = description.Split(' ');
        
        for (int i = 0; i < words.Length - 2; i++)
        {
            if (words[i].ToLower() == "trouvez" && int.TryParse(words[i + 1], out _))
            {
                // Reconstitue le nom de l'objet (peut être sur plusieurs mots)
                string objectName = "";
                for (int j = i + 2; j < words.Length; j++)
                {
                    if (words[j].ToLower() == "dans")
                        break;
                    
                    if (!string.IsNullOrEmpty(objectName))
                        objectName += "_";
                    objectName += words[j].ToLower();
                }
                
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
                    Debug.Log($"[EXTRACT] Description: '{description}' -> Objet: '{objectName}'");
                return objectName;
            }
        }
        
        // Fallback si le format n'est pas reconnu
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest))
            Debug.LogWarning($"⚠️ Format de description non reconnu: {description}");
        
        return "objet_inconnu";
    }
    
    // Visualisation dans l'éditeur
    void OnDrawGizmosSelected()
    {
        // Rayon d'interaction
        Gizmos.color = hasCompletableQuest ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Position du prompt
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + promptOffset, Vector3.one * 0.3f);
    }
}