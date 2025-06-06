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
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        
        if (debugMode)
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
            if (debugMode)
                Debug.LogWarning("⚠️ QuestJournal ou PlayerInventory manquant");
            return;
        }
        
        // Cherche une quête active avec ce NPC
        var activeQuests = QuestJournal.Instance.GetActiveQuests();
        var npcQuest = activeQuests.FirstOrDefault(q => q.giverNPCName == npcScript.npcName);
        
        if (npcQuest != null && npcQuest.questType == QuestType.FETCH)
        {
            string objectName = ExtractObjectNameFromDescription(npcQuest.description);
            
            if (debugMode)
                Debug.Log($"🔍 Vérification quête: {npcQuest.questTitle} - Objet: {objectName} x{npcQuest.maxProgress}");
            
            // Vérifie si le joueur a les objets requis
            bool hasAllItems = PlayerInventory.Instance.HasItemsForQuest(
                objectName, 
                npcQuest.maxProgress, 
                npcQuest.questId
            );
            
            if (hasAllItems)
            {
                hasCompletableQuest = true;
                currentCompletableQuest = npcQuest;
                ShowTurnInPrompt(npcQuest);
                
                if (debugMode)
                    Debug.Log($"✅ Quête peut être rendue: {npcQuest.questTitle}");
            }
            else
            {
                hasCompletableQuest = false;
                currentCompletableQuest = null;
                HideTurnInPrompt();
                
                if (debugMode)
                {
                    int currentCount = PlayerInventory.Instance.GetItemQuantity(objectName, npcQuest.questId);
                    Debug.Log($"❌ Objets insuffisants: {currentCount}/{npcQuest.maxProgress} {objectName}");
                }
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
            promptText.text = $"🎯 [F] Rendre la quête\n\"{quest.questTitle}\"";
            promptText.color = Color.green;
        }
        
        if (promptDisplay != null)
        {
            promptDisplay.SetActive(true);
        }
        
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
        
        string objectName = ExtractObjectNameFromDescription(currentCompletableQuest.description);
        
        if (debugMode)
            Debug.Log($"🎯 Tentative de rendu: {objectName} x{currentCompletableQuest.maxProgress}");
        
        // Retire les objets de l'inventaire
        bool removed = PlayerInventory.Instance.RemoveItem(
            objectName, 
            currentCompletableQuest.maxProgress, 
            currentCompletableQuest.questId
        );
        
        if (removed)
        {
            // Complete la quête dans le journal
            QuestJournal.Instance.CompleteQuest(currentCompletableQuest.questId);
            
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
            Debug.Log($"💬 {completionMessage}");
        }
    }
    
    string GetCompletionMessage(JournalQuest quest)
    {
        switch (npcScript.npcRole.ToLower())
        {
            case "marchand":
                return $"{npcScript.npcName}: Parfait ! Vous avez récupéré tout ce que je demandais. " +
                       $"Voici votre récompense bien méritée ! Ces {ExtractObjectNameFromDescription(quest.description)} " +
                       $"vont me rapporter gros sur le marché.";
            
            case "scientifique":
                return $"{npcScript.npcName}: Excellent travail ! Ces spécimens de {ExtractObjectNameFromDescription(quest.description)} " +
                       $"vont révolutionner mes recherches. La science vous remercie ! " +
                       $"Vos efforts contribuent à l'avancement de nos connaissances.";
            
            case "garde impérial":
                return $"{npcScript.npcName}: Mission accomplie avec brio, voyageur ! " +
                       $"Vous avez récupéré les {ExtractObjectNameFromDescription(quest.description)} comme demandé. " +
                       $"L'Empire reconnaît votre efficacité et votre dévouement.";
            
            default:
                return $"{npcScript.npcName}: Merci infiniment ! Vous avez accompli exactement ce que je demandais. " +
                       $"Ces {ExtractObjectNameFromDescription(quest.description)} me sont très précieux. " +
                       $"C'est un travail formidable !";
        }
    }
    
    // Extrait le nom de l'objet de la description de la quête
    string ExtractObjectNameFromDescription(string description)
    {
        // Format attendu: "Trouvez X objet_name dans zone"
        // Exemple: "Trouvez 3 cristal_energie dans laboratory"
        
        string[] words = description.Split(' ');
        for (int i = 0; i < words.Length - 2; i++)
        {
            if (words[i].ToLower() == "trouvez" && int.TryParse(words[i + 1], out _))
            {
                return words[i + 2];
            }
        }
        
        // Fallback si le format n'est pas reconnu
        if (debugMode)
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