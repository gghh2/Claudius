using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DialogueUI : MonoBehaviour
{
    [Header("===== UI ELEMENTS - Standard Interface =====")]
    
    [Header("Main Panels")]
    [Tooltip("UI Element - Main dialogue panel")]
    public GameObject dialoguePanel;
    
    [Tooltip("UI Element - NPC name display")]
    public TextMeshProUGUI npcNameText;
    
    [Tooltip("UI Element - Dialogue text display")]
    public TextMeshProUGUI dialogueText;
    
    [Tooltip("UI Element - Continue button")]
    public Button continueButton;
    
    [Tooltip("UI Element - Close button")]
    public Button closeButton;
    
    [Space(20)]
    [Header("===== AI CONFIGURATION - Used by AI System =====")]
    
    [Header("AI Integration")]
    [Tooltip("AI SYSTEM - Loading indicator for AI responses")]
    public GameObject loadingIndicator;
    
    [Tooltip("AI SYSTEM - Player input field for AI conversations")]
    public TMP_InputField playerInputField;
    
    [Tooltip("AI SYSTEM - Send button for AI messages")]
    public Button sendButton;
    
    [Tooltip("AI SYSTEM - Button to switch to AI mode")]
    public Button switchToAIButton;
    
    [Space(20)]
    [Header("===== TECHNICAL CONFIGURATION - Not used by AI =====")]
    
    [Header("History")]
    [Tooltip("Technical - History button")]
    public Button historyButton;
    
    [Tooltip("Technical - History panel")]
    public GameObject historyPanel;
    
    [Tooltip("Technical - History text display")]
    public TextMeshProUGUI historyText;
    
    [Tooltip("Technical - Close history button")]
    public Button closeHistoryButton;
    
    [Header("Quest Confirmation")]
    [Tooltip("Technical - Accept quest button")]
    public Button acceptQuestButton;
    
    [Tooltip("Technical - Decline quest button")]
    public Button declineQuestButton;
    
    [Header("Settings")]
    [Tooltip("Technical - Text typing speed")]
    public float typingSpeed = 0.03f;
    
    // Private variables
    private bool isTyping = false;
    private string currentFullText = "";
    private NPCData currentNPC;
    private int dialogueStep = 0;
    private bool isAIMode = false;
    private bool isCurrentlyDisplaying = false;
    private bool isSendingMessage = false;
    
    // Quest confirmation system
    private List<QuestToken> pendingQuests = new List<QuestToken>();
    private string questGiverName = "";
    
    public static DialogueUI Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        dialoguePanel.SetActive(false);
        continueButton.onClick.AddListener(OnContinueClicked);
        closeButton.onClick.AddListener(CloseDialogue);
        
        // Setup pour l'IA
        if (sendButton != null)
            sendButton.onClick.AddListener(SendPlayerMessage);
            
        if (switchToAIButton != null)
            switchToAIButton.onClick.AddListener(SwitchToAIMode);
        
        // Setup pour l'historique
        if (historyButton != null)
            historyButton.onClick.AddListener(ShowConversationHistory);
        
        if (closeHistoryButton != null)
            closeHistoryButton.onClick.AddListener(CloseHistory);
            
        if (historyPanel != null)
            historyPanel.SetActive(false);
        
        // Setup pour les quêtes
        if (acceptQuestButton != null)
        {
            acceptQuestButton.onClick.AddListener(AcceptQuests);
            acceptQuestButton.gameObject.SetActive(false);
            Debug.Log("[UI] Accept button configured and hidden");
        }
        else
        {
            Debug.LogError("[UI] AcceptQuestButton not assigned in Inspector!");
        }
        
        if (declineQuestButton != null)
        {
            declineQuestButton.onClick.AddListener(DeclineQuests);
            declineQuestButton.gameObject.SetActive(false);
            Debug.Log("[UI] Decline button configured and hidden");
        }
        else
        {
            Debug.LogError("[UI] DeclineQuestButton not assigned in Inspector!");
        }
        
        // Cache les éléments au départ
        SetAIElementsVisibility(false);
    }
    
    [ContextMenu("Debug AI Fields")]
    public void DebugAIFields()
    {
        Debug.Log($"=== AI FIELDS for DialogueUI ===");
        Debug.Log($"Loading Indicator: {(loadingIndicator != null ? "SET" : "NOT SET")}");
        Debug.Log($"Player Input Field: {(playerInputField != null ? "SET" : "NOT SET")}");
        Debug.Log($"Send Button: {(sendButton != null ? "SET" : "NOT SET")}");
        Debug.Log($"Switch to AI Button: {(switchToAIButton != null ? "SET" : "NOT SET")}");
        Debug.Log("=================================");
    }
    
    
    void Update()
    {

	 // Gestion ENTER en mode IA
        if (isAIMode && !isSendingMessage)
        {
            // Vérifie si ENTER est pressé ET qu'on est dans l'input field
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                // Vérifie que l'input field a du contenu
                if (playerInputField != null && !string.IsNullOrEmpty(playerInputField.text.Trim()))
                {
                    // Empêche le comportement par défaut (select all)
                    if (playerInputField.isFocused)
                    {
                        SendPlayerMessage();
                    }
                }
            }
        }
    }
    
    void SetAIElementsVisibility(bool visible)
    {
        if (playerInputField != null)
            playerInputField.gameObject.SetActive(visible);
        if (sendButton != null)
            sendButton.gameObject.SetActive(visible);
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }
    
    void SetQuestButtonsVisibility(bool visible)
	{
	    Debug.Log($"🔧 SetQuestButtonsVisibility appelée avec: {visible}");
	    
	    if (acceptQuestButton != null)
	    {
	        acceptQuestButton.gameObject.SetActive(visible);
	        Debug.Log($"Bouton Accept: {(visible ? "AFFICHÉ" : "CACHÉ")}");
	    }
	    else
	    {
	        Debug.LogError("AcceptQuestButton est null !");
	    }
	    
	    if (declineQuestButton != null)
	    {
	        declineQuestButton.gameObject.SetActive(visible);
	        Debug.Log($"Bouton Decline: {(visible ? "AFFICHÉ" : "CACHÉ")}");
	    }
	    else
	    {
	        Debug.LogError("DeclineQuestButton est null !");
	    }
	}
    
    // Mode dialogue classique (script fixe)
    public void StartDialogue(NPCData npcData)
    {
        currentNPC = npcData;
        dialogueStep = 0;
        isAIMode = false;
        
        dialoguePanel.SetActive(true);
        
        // Applique la couleur du NPC au nom
        npcNameText.text = currentNPC.name;
        npcNameText.color = GetNPCColor(currentNPC.name);
        
        // Affiche le bouton historique SI on a déjà parlé à ce NPC
        UpdateHistoryButtonVisibility();
        
        string welcomeMessage = GetWelcomeMessage(currentNPC);
        ShowText(welcomeMessage);
        
        // Sauvegarde le message d'accueil
        if (AIDialogueManager.Instance != null)
        {
            AIDialogueManager.Instance.SaveMessageToHistory(currentNPC.name, welcomeMessage, false);
        }
        
        // Setup UI pour mode classique
        continueButton.gameObject.SetActive(true);
        if (switchToAIButton != null)
            switchToAIButton.gameObject.SetActive(true);
        SetAIElementsVisibility(false);
        SetQuestButtonsVisibility(false);
        
        FindObjectOfType<PlayerController>().enabled = false;
    }
    
    // Mode dialogue IA
    public void StartAIDialogue(NPCData npcData, string aiWelcomeMessage)
    {
        currentNPC = npcData;
        isAIMode = true;
        
        dialoguePanel.SetActive(true);
        
        // Applique la couleur du NPC
        npcNameText.text = currentNPC.name;
        npcNameText.color = GetNPCColor(currentNPC.name);
        
        UpdateHistoryButtonVisibility();
        
        ShowText(aiWelcomeMessage);
        
        // Setup UI pour mode IA
        continueButton.gameObject.SetActive(false);
        if (switchToAIButton != null)
            switchToAIButton.gameObject.SetActive(false);
        SetAIElementsVisibility(true);
        
        FindObjectOfType<PlayerController>().enabled = false;
    }
    
    void SwitchToAIMode()
    {
        if (AIDialogueManager.Instance != null)
        {
            isAIMode = true;
            
            // Change l'interface pour le mode IA
            continueButton.gameObject.SetActive(false);
            if (switchToAIButton != null)
                switchToAIButton.gameObject.SetActive(false);
            SetAIElementsVisibility(true);
            
            // Focus sur l'input
            if (playerInputField != null)
            {
                playerInputField.Select();
                playerInputField.ActivateInputField();
            }
            
            // Initialise la conversation IA avec le contexte du dialogue classique
            AIDialogueManager.Instance.InitializeConversationWithContext(currentNPC, currentFullText, dialogueStep);
        }
        else
        {
            Debug.LogError("AIDialogueManager non trouvé !");
        }
    }
    
    string GetWelcomeMessage(NPCData npcData)
{
    // NOUVEAU : Vérifie d'abord s'il y a une quête active avec ce NPC
    if (QuestJournal.Instance != null)
    {
        var activeQuests = QuestJournal.Instance.GetActiveQuests();
        var npcActiveQuest = activeQuests.FirstOrDefault(q => q.giverNPCName == npcData.name);
        
        if (npcActiveQuest != null)
        {
            // Il y a une quête active avec ce NPC - message personnalisé selon le rôle
            switch (npcData.role.ToLower())
            {
                case "marchand":
                    return $"{npcData.name}: Alors, voyageur ! Avez-vous récupéré ce que je vous ai demandé ? Ma mission : « {npcActiveQuest.description} » - Progression : {npcActiveQuest.GetProgressText()}";
                
                case "scientifique":
                    return $"{npcData.name}: Ah, vous voilà ! Comment se passent vos recherches ? Mission en cours : « {npcActiveQuest.description} » - Avancement : {npcActiveQuest.GetProgressText()}";
                
                case "garde impérial":
                    return $"{npcData.name}: Rapport de mission, voyageur ! Où en êtes-vous avec : « {npcActiveQuest.description} » ? Statut : {npcActiveQuest.GetProgressText()}";
                
                default:
                    return $"{npcData.name}: Bonjour ! Comment avance votre mission : « {npcActiveQuest.description} » ? Progression : {npcActiveQuest.GetProgressText()}";
            }
        }
        else
        {
            // Vérifie si une quête a été terminée avec ce NPC
            var completedQuests = QuestJournal.Instance.GetCompletedQuests();
            var npcCompletedQuest = completedQuests.FirstOrDefault(q => q.giverNPCName == npcData.name);
            
            if (npcCompletedQuest != null)
            {
                // Une quête a été terminée - message de félicitations selon le rôle
                switch (npcData.role.ToLower())
                {
                    case "marchand":
                        return $"{npcData.name}: Excellent travail, voyageur ! Vous avez accompli ma mission avec brio. J'ai peut-être autre chose pour vous...";
                    
                    case "scientifique":
                        return $"{npcData.name}: Fantastique ! Vos résultats dépassent mes attentes. La science vous remercie ! Auriez-vous le temps pour une nouvelle recherche ?";
                    
                    case "garde impérial":
                        return $"{npcData.name}: Mission accomplie avec succès ! L'Empire reconnaît votre efficacité. D'autres tâches vous attendent-elles ?";
                    
                    default:
                        return $"{npcData.name}: Merci beaucoup ! Vous avez parfaitement réalisé ce que je vous avais demandé. J'aurais peut-être d'autres services à vous proposer.";
                }
            }
        }
    }
    
    // ANCIEN CODE : Messages d'accueil par défaut si aucune quête
    switch (npcData.role.ToLower())
    {
        case "marchand":
            return $"Salutations, voyageur ! Je suis {npcData.name}. Cherchez-vous quelque chose de particulier dans mes marchandises ?";
        case "scientifique":
            return $"Bonjour ! {npcData.name} à votre service. Mes recherches sur les technologies alien pourraient vous intéresser.";
        case "garde impérial":
            return $"Halte ! {npcData.name}, sécurité impériale. Déclinez votre identité et vos intentions.";
        default:
            return $"Bonjour, je suis {npcData.name}. En quoi puis-je vous aider ?";
    }
}
    
    // Dialogues de suivi basiques (mode classique)
    string GetFollowUpDialogue(NPCData npcData, int step)
    {
        switch (npcData.role.ToLower())
        {
            case "marchand":
                switch (step)
                {
                    case 1: return "J'ai des artefacts rares de toute la galaxie. Des cristaux énergétiques, des cartes stellaires anciennes...";
                    case 2: return "Mes prix sont honnêtes pour un voyageur comme vous. Que diriez-vous d'un petit échange ?";
                    case 3: return "Revenez me voir quand vous aurez besoin d'équipement ! Bon voyage !";
                    default: return "Au plaisir de faire affaire avec vous !";
                }
            
            case "scientifique":
                switch (step)
                {
                    case 1: return "J'étudie les technologies des anciens. Leurs connaissances dépassent tout ce que nous comprenons.";
                    case 2: return "Avez-vous déjà vu des ruines alien ? Leurs symboles contiennent des secrets fascinants...";
                    case 3: return "Si vous trouvez des artefacts, n'hésitez pas à me les montrer. La science a besoin de données !";
                    default: return "Que la connaissance nous guide !";
                }
            
            case "garde impérial":
                switch (step)
                {
                    case 1: return "Cette zone est sous surveillance impériale. Assurez-vous de respecter les protocoles.";
                    case 2: return "Nous avons détecté une activité suspecte récemment. Restez vigilant.";
                    case 3: return "Vous pouvez circuler, mais gardez vos papiers à portée de main.";
                    default: return "Gloire à l'Empire !";
                }
            
            default:
                switch (step)
                {
                    case 1: return "Cette station spatiale est un carrefour pour de nombreux voyageurs.";
                    case 2: return "Chaque jour apporte son lot de nouvelles histoires et d'aventures.";
                    case 3: return "Prenez soin de vous dans vos voyages !";
                    default: return "À bientôt !";
                }
        }
    }
    
    public void ShowText(string text)
    {
        // Empêche les appels multiples
        if (isCurrentlyDisplaying)
        {
            Debug.Log("ShowText ignoré - déjà en cours d'affichage");
            return;
        }
        
        Debug.Log($"ShowText appelé avec: {text}");
        
        // Stop tout et reset
        StopAllCoroutines();
        isTyping = false;
        isCurrentlyDisplaying = true;
        
        currentFullText = text;
        
        // Colore le texte si c'est une réplique du NPC
        string coloredText = ColorizeNPCText(text);
        dialogueText.text = coloredText;
        
        isCurrentlyDisplaying = false;
        
        Debug.Log($"Texte affiché: {coloredText}");
    }
    
    string ColorizeNPCText(string text)
    {
        if (currentNPC != null && text.Contains($"{currentNPC.name}:"))
        {
            // Convertit la couleur en hex
            Color npcColor = GetNPCColor(currentNPC.name);
            string hexColor = ColorUtility.ToHtmlStringRGB(npcColor);
            
            // Applique la couleur au nom du NPC
            string coloredText = text.Replace(
                $"{currentNPC.name}:", 
                $"<color=#{hexColor}>{currentNPC.name}:</color>"
            );
            
            return coloredText;
        }
        
        return text;
    }
    
    void OnContinueClicked()
    {
        // Mode classique seulement
        if (isAIMode) return;
        
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentFullText;
            isTyping = false;
        }
        else
        {
            // Passe à la prochaine réplique
            dialogueStep++;
            string nextDialogue = GetFollowUpDialogue(currentNPC, dialogueStep);
            ShowText(nextDialogue);
            
            // Sauvegarde le message dans l'historique
            if (AIDialogueManager.Instance != null)
            {
                AIDialogueManager.Instance.SaveMessageToHistory(currentNPC.name, nextDialogue, false);
            }
            
            // Après 4 répliques, on peut fermer
            if (dialogueStep >= 4)
            {
                continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Terminer";
            }
            
            if (dialogueStep >= 5)
            {
                CloseDialogue();
            }
        }
    }
    
    // Méthodes pour l'IA
    public void ShowLoadingState(bool isLoading)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(isLoading);
        }
        
        if (sendButton != null)
        {
            sendButton.interactable = !isLoading;
        }
        
        if (playerInputField != null)
        {
            playerInputField.interactable = !isLoading;
        }
    }
    
    void SendPlayerMessage()
    {
        if (isSendingMessage) return; // Empêche les appels multiples
        
        if (playerInputField != null && !string.IsNullOrEmpty(playerInputField.text.Trim()))
        {
            isSendingMessage = true;
            
            string playerMessage = playerInputField.text.Trim();
            
            // Sauvegarde le message du joueur
            if (AIDialogueManager.Instance != null)
            {
                AIDialogueManager.Instance.SaveMessageToHistory(currentNPC.name, playerMessage, true);
            }
            
            // Affiche le message du joueur dans l'interface
            string displayText = $"Vous: {playerMessage}\n\n{currentNPC.name} réfléchit...";
            ShowText(displayText);
            
            // Clear input ET remet le focus
            playerInputField.text = "";
            playerInputField.Select();
            playerInputField.ActivateInputField();
            
            // Demande une réponse à l'IA
            if (AIDialogueManager.Instance != null)
            {
                AIDialogueManager.Instance.ContinueAIConversation(currentNPC, playerMessage);
            }
            else
            {
                ShowText("Erreur: Système IA non disponible.");
            }
            
            // Reset après un délai
            StartCoroutine(ResetSendingFlag());
        }
    }
    
    IEnumerator ResetSendingFlag()
    {
        yield return new WaitForSeconds(0.5f);
        isSendingMessage = false;
    }
    
    // Méthode appelée par l'IA pour afficher sa réponse
    public void ShowAIResponse(string aiResponse)
    {
        ShowText(aiResponse);
        Debug.Log($"Affichage réponse IA: {aiResponse}");
    }
    
    // SYSTÈME DE CONFIRMATION DES QUÊTES
	public void SetPendingQuests(List<QuestToken> quests, string giverName)
	{
	    pendingQuests.Clear();
	    pendingQuests.AddRange(quests);
	    questGiverName = giverName;
	    
	    if (quests.Count > 0)
	    {
	        // CACHE les éléments d'input IA et AFFICHE les boutons de quête
	        SetAIElementsVisibility(false);
	        SetQuestButtonsVisibility(true);
	        
	        Debug.Log($"Quêtes en attente de confirmation: {quests.Count}");
	        
	        // Affiche un message d'information
	        string questInfo = "\n\n--- MISSION PROPOSÉE ---\n";
	        foreach (QuestToken quest in quests)
	        {
	            questInfo += $"• {quest.description}\n";
	        }
	        questInfo += "Acceptez-vous cette mission ?";
	        
	        ShowText(currentFullText + questInfo);
	    }
	}
    

    void AcceptQuests()
	{
	    Debug.Log("✅ Joueur accepte les quêtes");
	    
	    // Crée maintenant les quêtes
	    if (QuestManager.Instance != null)
	    {
	        foreach (QuestToken quest in pendingQuests)
	        {
	            bool success = QuestManager.Instance.CreateQuestFromToken(quest, questGiverName);
	            if (success)
	            {
	                Debug.Log($"✅ Quête créée: {quest.description}");
	            }
	        }
	    }
	    
	    ClearPendingQuests();
	    
	    // REAFFICHE les éléments IA après acceptation
	    SetAIElementsVisibility(true);
	    
	    ShowText(currentNPC.name + ": Parfait ! Vos missions sont maintenant actives. Bonne chance !");
	    
	    // Remet le focus sur l'input
	    if (playerInputField != null)
	    {
	        playerInputField.Select();
	        playerInputField.ActivateInputField();
	    }
	}

	void DeclineQuests()
	{
	    Debug.Log("❌ Joueur refuse les quêtes");
	    ClearPendingQuests();
	    
	    // REAFFICHE les éléments IA après refus
	    SetAIElementsVisibility(true);
	    
	    ShowText(currentNPC.name + ": Très bien, revenez me voir si vous changez d'avis.");
	    
	    // Remet le focus sur l'input
	    if (playerInputField != null)
	    {
	        playerInputField.Select();
	        playerInputField.ActivateInputField();
	    }
	}

	void ClearPendingQuests()
	{
	    pendingQuests.Clear();
	    questGiverName = "";
	    SetQuestButtonsVisibility(false);
	}
    
    // Nouvelle méthode pour récupérer la couleur
    Color GetNPCColor(string npcName)
    {
        // Trouve le NPC dans la scène par son nom
        NPC[] allNPCs = FindObjectsOfType<NPC>();
        
        foreach (NPC npc in allNPCs)
        {
            if (npc.npcName == npcName)
            {
                return npc.npcColor;
            }
        }
        
        // Couleur par défaut si pas trouvé
        return Color.white;
    }
    
    void UpdateHistoryButtonVisibility()
    {
        if (historyButton != null && AIDialogueManager.Instance != null)
        {
            bool hasHistory = AIDialogueManager.Instance.HasSpokenToNPC(currentNPC.name);
            historyButton.gameObject.SetActive(hasHistory);
        }
    }
    
    void ShowConversationHistory()
    {
        if (AIDialogueManager.Instance != null && currentNPC != null)
        {
            ConversationHistory history = AIDialogueManager.Instance.GetConversationHistory(currentNPC.name);
            
            if (history != null && history.messages.Count > 0)
            {
                dialoguePanel.SetActive(false);
                historyPanel.SetActive(true);
                
                string historyContent = $"<size=24><color=yellow>=== Historique avec {currentNPC.name} ===</color></size>\n\n";
                
                for (int i = 0; i < history.messages.Count; i++)
                {
                    string message = history.messages[i];
                    if (i > 0)
                        historyContent += "─────────────────────\n";
                    historyContent += message + "\n\n";
                }
                
                historyText.text = historyContent;
                
                Canvas.ForceUpdateCanvases();
                
                ScrollRect scrollRect = historyPanel.GetComponentInChildren<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 1f;
                }
            }
        }
    }
    
    void CloseHistory()
    {
        if (historyPanel != null)
            historyPanel.SetActive(false);
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
    }
    
    // Méthode utile pour vérifier si l'UI est ouverte
    public bool IsDialogueOpen()
    {
        return dialoguePanel != null && dialoguePanel.activeInHierarchy;
    }
    
    public void CloseDialogue()
    {
        CloseHistory(); // Ferme l'historique aussi
        ClearPendingQuests(); // Nettoie les quêtes en attente
        
        dialoguePanel.SetActive(false);
        FindObjectOfType<PlayerController>().enabled = true;
        
        // REPREND le mouvement et réaffiche les noms
        NPCMovement[] allNPCMovements = FindObjectsOfType<NPCMovement>();
        NPCNameDisplay[] allNameDisplays = FindObjectsOfType<NPCNameDisplay>();

        foreach (NPCMovement movement in allNPCMovements)
        {
            movement.ResumeMovement();
        }

        foreach (NPCNameDisplay nameDisplay in allNameDisplays)
        {
            nameDisplay.ShowName();
        }
        
        // Reset pour la prochaine conversation
        if (continueButton != null)
        {
            var buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Continuer";
        }
        
        dialogueStep = 0;
        isAIMode = false;
        SetAIElementsVisibility(false);
        SetQuestButtonsVisibility(false);
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }
}