using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DialogueUI : MonoBehaviour
{
    [Header("===== UI ELEMENTS - Main Interface =====")]
    
    [Header("Main Panels")]
    [Tooltip("UI Element - Main dialogue panel")]
    public GameObject dialoguePanel;
    
    [Tooltip("UI Element - NPC name display")]
    public TextMeshProUGUI npcNameText;
    
    [Tooltip("UI Element - Dialogue text display")]
    public TextMeshProUGUI dialogueText;
    
    [Tooltip("UI Element - Close button")]
    public Button closeButton;
    
    [Header("AI Conversation")]
    [Tooltip("AI SYSTEM - Loading indicator for AI responses")]
    public GameObject loadingIndicator;
    
    [Tooltip("AI SYSTEM - Player input field for AI conversations")]
    public TMP_InputField playerInputField;
    
    [Tooltip("AI SYSTEM - Send button for AI messages")]
    public Button sendButton;
    
    [Header("History")]
    [Tooltip("History button")]
    public Button historyButton;
    
    [Tooltip("History panel")]
    public GameObject historyPanel;
    
    [Tooltip("History text display")]
    public TextMeshProUGUI historyText;
    
    [Tooltip("Close history button")]
    public Button closeHistoryButton;
    
    [Header("Quest Confirmation")]
    [Tooltip("Accept quest button")]
    public Button acceptQuestButton;
    
    [Tooltip("Decline quest button")]
    public Button declineQuestButton;
    
    [Header("Delivery System")]
    [Tooltip("Deliver item button")]
    public Button deliverButton;
    
    [Header("Settings")]
    [Tooltip("Text typing speed")]
    public float typingSpeed = 0.03f;
    
    // Private variables
    private string currentFullText = "";
    private NPCData currentNPC;
    private bool isCurrentlyDisplaying = false;
    private bool isSendingMessage = false;
    
    // Quest confirmation system
    private List<QuestToken> pendingQuests = new List<QuestToken>();
    private string questGiverName = "";
    
    // Delivery system
    private string pendingDeliveryQuestId = "";
    private string pendingDeliveryPackage = "";
    
    // Fetch quest completion system
    private string pendingFetchQuestId = "";
    private string pendingFetchObjectName = "";
    private int pendingFetchQuantity = 0;
    
    public static DialogueUI Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Auto-attache l'autocompletion si AUCUNE instance n'est deja
            // wired en scene (designer peut l'avoir mise sur DialoguePanel
            // ou ailleurs). FindFirstObjectByType inclut les inactifs si on
            // passe FindObjectsInactive.Include — pertinent car DialoguePanel
            // demarre inactif.
            if (FindFirstObjectByType<DialogueAutocomplete>(FindObjectsInactive.Include) == null)
                gameObject.AddComponent<DialogueAutocomplete>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        dialoguePanel.SetActive(false);
        closeButton.onClick.AddListener(CloseDialogue);
        
        // Setup pour l'IA
        if (sendButton != null)
            sendButton.onClick.AddListener(SendPlayerMessage);
            
        // Configure l'InputField pour gérer Enter correctement
        if (playerInputField != null)
        {
            // Configure le submit à la validation (Enter)
            playerInputField.onSubmit.RemoveAllListeners();
            playerInputField.onSubmit.AddListener((text) => {
                if (!string.IsNullOrEmpty(text.Trim()) && !isSendingMessage)
                {
                    SendPlayerMessage();
                }
            });
            
            // Optionnel : Configure aussi onEndEdit si besoin
            playerInputField.onEndEdit.RemoveAllListeners();
            playerInputField.onEndEdit.AddListener((text) => {
                // Garde le focus sur l'input
                if (dialoguePanel.activeInHierarchy)
                {
                    StartCoroutine(RefocusInput());
                }
            });
        }
        
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
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log("[UI] Accept button configured and hidden");
        }
        else
        {
            Debug.LogError("[UI] AcceptQuestButton not assigned in Inspector!");
        }
        
        if (declineQuestButton != null)
        {
            declineQuestButton.onClick.AddListener(DeclineQuests);
            declineQuestButton.gameObject.SetActive(false);
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log("[UI] Decline button configured and hidden");
        }
        else
        {
            Debug.LogError("[UI] DeclineQuestButton not assigned in Inspector!");
        }
        
        // Setup pour les livraisons
        if (deliverButton != null)
        {
            deliverButton.onClick.AddListener(DeliverPackage);
            deliverButton.gameObject.SetActive(false);
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log("[UI] Deliver button configured and hidden");
        }
        else
        {
            Debug.LogError("[UI] DeliverButton not assigned in Inspector!");
        }
    }
    
    void Update()
    {
        // Gestion ENTER pour envoyer les messages
        if (!isSendingMessage && dialoguePanel.activeInHierarchy)
        {
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                if (playerInputField != null && playerInputField.gameObject.activeInHierarchy)
                {
                    if (!string.IsNullOrEmpty(playerInputField.text.Trim()))
                    {
                        SendPlayerMessage();
                        Event.current?.Use();
                    }
                }
            }
        }
        
        // Gestion des raccourcis pour les quêtes
        if (pendingQuests.Count > 0 && acceptQuestButton != null && acceptQuestButton.gameObject.activeInHierarchy)
        {
            // A pour Accepter
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log("[UI] Touche A pressée - Acceptation des quêtes");
                StartCoroutine(AnimateButtonPress(acceptQuestButton));
                AcceptQuests();
            }
            // R pour Refuser
            else if (Input.GetKeyDown(KeyCode.R))
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log("[UI] Touche R pressée - Refus des quêtes");
                StartCoroutine(AnimateButtonPress(declineQuestButton));
                DeclineQuests();
            }
        }
    }
    
    /// <summary>
    /// Démarre un dialogue avec un NPC (toujours en IA)
    /// </summary>
    public void StartDialogue(NPCData npcData)
    {
        currentNPC = npcData;

        // Vérifie que l'IA est configurée
        if (AIDialogueManager.Instance == null || !AIDialogueManager.Instance.IsConfigured())
        {
            ShowAPIError();
            return;
        }

        // Log la première rencontre avec ce NPC dans le Journal d'Aventure
        if (npcData != null && AdventureJournalIntegration.IsFirstTime("npc_" + npcData.name))
        {
            AdventureJournalExtensions.LogFirstMeeting(npcData);
            AdventureJournalIntegration.MarkAsDone("npc_" + npcData.name);
        }
        
        // IMPORTANT : on vide AVANT d'activer le panel, sinon le panel
        // s'affiche brièvement avec l'ancien dialogue (le clear arrive trop
        // tard, après la 1re layout pass de Unity).
        dialogueText.text = "";
        currentFullText = "";
        isCurrentlyDisplaying = false;

        // Affiche le nom du NPC avec sa couleur (avant activation aussi).
        npcNameText.text = TextFormatter.FormatName(currentNPC.name);
        npcNameText.color = GetNPCColor(currentNPC.name);

        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.Dialogue);
        }
        else
        {
            dialoguePanel.SetActive(true);
        }

        // Affiche le bouton historique si on a déjà parlé à ce NPC
        UpdateHistoryButtonVisibility();
        
        // Active les éléments d'interface
        SetAIElementsVisibility(true);
        SetQuestButtonsVisibility(false);
        
        // Reset le flag d'envoi
        isSendingMessage = false;
        
        // Focus sur l'input
        if (playerInputField != null)
        {
            playerInputField.Select();
            playerInputField.ActivateInputField();
        }
        
        // Désactive les contrôles du joueur
        FindFirstObjectByType<PlayerControllerCC>()?.EnableControls(false);
        
        // Démarre la conversation IA
        AIDialogueManager.Instance.StartAIConversation(npcData);
    }
    
    /// <summary>
    /// Appelé par l'AIDialogueManager pour afficher le message de bienvenue
    /// </summary>
    public void StartAIDialogue(NPCData npcData, string aiWelcomeMessage)
    {
        // Plus besoin de cette distinction, on utilise toujours StartDialogue
        // Mais on garde cette méthode pour la compatibilité avec AIDialogueManager
        ShowText(aiWelcomeMessage);
    }
    
    /// <summary>
    /// Affiche une erreur API
    /// </summary>
    void ShowAPIError()
    {
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.Dialogue);
        }
        else
        {
            dialoguePanel.SetActive(true);
        }
        
        npcNameText.text = "ERREUR SYSTÈME";
        npcNameText.color = Color.red;
        
        dialogueText.text = "<color=red>ERREUR API : Le système de dialogue IA n'est pas configuré correctement.\n\n" +
                          "Vérifiez que la clé API OpenAI est configurée dans APIConfig.cs</color>";
        
        // Cache tous les boutons sauf fermer
        SetAIElementsVisibility(false);
        SetQuestButtonsVisibility(false);
        if (deliverButton != null) deliverButton.gameObject.SetActive(false);
        if (historyButton != null) historyButton.gameObject.SetActive(false);
    }
    
    void SetAIElementsVisibility(bool visible)
    {
        if (playerInputField != null)
            playerInputField.gameObject.SetActive(visible);
        if (sendButton != null)
        {
            sendButton.gameObject.SetActive(visible);
            // S'assurer que le bouton est interactable quand il est visible
            if (visible)
                sendButton.interactable = true;
        }
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }
    
    void SetQuestButtonsVisibility(bool visible)
    {
        if (acceptQuestButton != null)
            acceptQuestButton.gameObject.SetActive(visible);
        
        if (declineQuestButton != null)
            declineQuestButton.gameObject.SetActive(visible);
    }
    
    public void ShowText(string text)
    {
        if (isCurrentlyDisplaying)
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log("[UI] ShowText ignoré - déjà en cours d'affichage");
            return;
        }

        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log($"[UI] ShowText appelé avec: {text}");
        
        StopAllCoroutines();
        isCurrentlyDisplaying = true;
        
        currentFullText = text;
        
        // Colore le texte si c'est une réplique du NPC
        string coloredText = ColorizeNPCText(text);
        dialogueText.text = coloredText;
        
        isCurrentlyDisplaying = false;

        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log($"[UI] Texte affiché: {coloredText}");
    }
    
    string ColorizeNPCText(string text)
    {
        if (currentNPC != null && text.Contains($"{currentNPC.name}:"))
        {
            string formattedName = TextFormatter.FormatName(currentNPC.name);
            Color npcColor = GetNPCColor(currentNPC.name);
            string hexColor = ColorUtility.ToHtmlStringRGB(npcColor);
            
            string coloredText = text.Replace(
                $"{currentNPC.name}:", 
                $"<color=#{hexColor}>{formattedName}:</color>"
            );
            
            return coloredText;
        }
        
        return text;
    }
    
    public void ShowLoadingState(bool isLoading)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(isLoading);
        
        // Gestion du bouton et de l'input pendant le chargement
        if (isLoading)
        {
            // Désactiver pendant le chargement
            if (sendButton != null)
                sendButton.interactable = false;
            
            if (playerInputField != null)
                playerInputField.interactable = false;
        }
        else
        {
            // Réactiver après le chargement
            isSendingMessage = false; // Reset le flag
            
            if (sendButton != null)
                sendButton.interactable = true;
                
            if (playerInputField != null)
            {
                playerInputField.interactable = true;
                // Remettre le focus sur l'input
                playerInputField.Select();
                playerInputField.ActivateInputField();
            }
        }
    }
    
    void SendPlayerMessage()
    {
        if (isSendingMessage) return;
        
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
                isSendingMessage = false;
            }
            
            // NOTE: Le flag isSendingMessage sera réinitialisé par ShowLoadingState(false)
            // quand l'IA aura répondu
        }
    }
    
    IEnumerator RefocusInput()
    {
        yield return null;
        
        if (playerInputField != null && dialoguePanel.activeInHierarchy)
        {
            playerInputField.Select();
            playerInputField.ActivateInputField();
        }
    }
    
    public void ShowAIResponse(string aiResponse)
    {
        ShowText(aiResponse);
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log($"[UI] Affichage réponse IA: {aiResponse}");
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
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log($"[UI] Quêtes en attente de confirmation: {quests.Count}");
            
            // Affiche un message d'information
            string questInfo = "\n\n--- MISSION PROPOSÉE ---\n";
            foreach (QuestToken quest in quests)
            {
                string formattedDescription = TextFormatter.FormatName(quest.description);
                questInfo += $"• {formattedDescription}\n";
            }
            questInfo += "\nAcceptez-vous cette mission ?\n[A] Accepter - [R] Refuser";
            
            ShowText(currentFullText + questInfo);
        }
    }
    
    void AcceptQuests()
    {
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log("[UI] ✅ Joueur accepte les quêtes");
        
        // Crée les quêtes
        if (QuestManager.Instance != null)
        {
            foreach (QuestToken quest in pendingQuests)
            {
                bool success = QuestManager.Instance.CreateQuestFromToken(quest, questGiverName);
                if (success)
                {
                    if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest)) Debug.Log($"[QUEST] ✅ Quête créée: {quest.description}");

                    // Log l'acceptation de la quête dans le Journal d'Aventure
                    AdventureJournalExtensions.LogQuestAccepted(quest.description, questGiverName);
                }
            }
        }
        
        ClearPendingQuests();
        
        // REAFFICHE les éléments IA après acceptation
        SetAIElementsVisibility(true);
        
        ShowText(currentNPC.name + ": Parfait ! Vos missions sont maintenant actives. Bonne chance !");
        
        // Reset le flag
        isSendingMessage = false;
        
        // Remet le focus sur l'input
        if (playerInputField != null)
        {
            playerInputField.Select();
            playerInputField.ActivateInputField();
        }
    }
    
    void DeclineQuests()
    {
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log("[UI] ❌ Joueur refuse les quêtes");
        ClearPendingQuests();
        
        // REAFFICHE les éléments IA après refus
        SetAIElementsVisibility(true);
        
        ShowText(currentNPC.name + ": Très bien, revenez me voir si vous changez d'avis.");
        
        // Reset le flag
        isSendingMessage = false;
        
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
    
    Color GetNPCColor(string npcName)
    {
        NPC[] allNPCs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        
        foreach (NPC npc in allNPCs)
        {
            if (npc.npcName == npcName)
            {
                return npc.npcColor;
            }
        }
        
        return Color.white;
    }
    
    // SYSTEME DE LIVRAISON
    public void ShowDeliveryButton(string questId, string packageName)
    {
        pendingDeliveryQuestId = questId;
        pendingDeliveryPackage = packageName;
        
        if (deliverButton != null)
        {
            deliverButton.gameObject.SetActive(true);
        }
        
        // Cache les éléments IA
        SetAIElementsVisibility(false);
    }
    
    public void ShowFetchQuestButton(string questId, string objectName, int quantity)
    {
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.UI)) Debug.Log($"[UI] ShowFetchQuestButton appelé: {objectName} x{quantity}");
        
        pendingFetchQuestId = questId;
        pendingFetchObjectName = objectName;
        pendingFetchQuantity = quantity;
        
        if (deliverButton != null)
        {
            deliverButton.gameObject.SetActive(true);
            var buttonText = deliverButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Remettre les objets";
        }
        
        // Cache les éléments IA
        SetAIElementsVisibility(false);
        SetQuestButtonsVisibility(false);
    }
    
    void DeliverPackage()
    {
        // Gestion des quêtes DELIVERY
        if (!string.IsNullOrEmpty(pendingDeliveryQuestId))
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest)) Debug.Log($"[QUEST] 🚚 Livraison du colis via UI: {pendingDeliveryPackage}");
            
            QuestObject[] allQuestObjects = FindObjectsByType<QuestObject>(FindObjectsSortMode.None);
            foreach (QuestObject qo in allQuestObjects)
            {
                if (qo.questId == pendingDeliveryQuestId && qo.isDeliveryTarget)
                {
                    qo.HandleDeliveryFromUI();
                    break;
                }
            }
            
            ShowText($"{currentNPC.name}: Parfait ! Merci pour cette livraison rapide. Votre travail est apprécié !");
        }
        // Gestion des quêtes FETCH
        else if (!string.IsNullOrEmpty(pendingFetchQuestId))
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest)) Debug.Log($"[QUEST] 📦 Remise des objets via UI: {pendingFetchObjectName} x{pendingFetchQuantity}");
            
            bool success = PlayerInventory.Instance.RemoveItem(
                pendingFetchObjectName, 
                pendingFetchQuantity, 
                pendingFetchQuestId
            );
            
            if (success)
            {
                QuestJournal.Instance.CompleteQuest(pendingFetchQuestId);
                
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.PlayQuestCompleteSoundPublic();
                    QuestManager.Instance.CleanupCompletedQuest(pendingFetchQuestId);
                }
                
                ShowText(GetFetchQuestCompletionMessage());
            }
            else
            {
                Debug.LogError("❌ Erreur lors du retrait des objets de l'inventaire");
                ShowText($"{currentNPC.name}: Il semble y avoir un problème... Avez-vous bien tous les objets ?");
            }
        }
        
        // Cache le bouton et réaffiche l'input
        if (deliverButton != null)
            deliverButton.gameObject.SetActive(false);
        
        SetAIElementsVisibility(true);
        
        // Reset
        pendingDeliveryQuestId = "";
        pendingDeliveryPackage = "";
        pendingFetchQuestId = "";
        pendingFetchObjectName = "";
        pendingFetchQuantity = 0;
        
        // Reset le flag
        isSendingMessage = false;
        
        // Remet le focus sur l'input
        if (playerInputField != null)
        {
            playerInputField.Select();
            playerInputField.ActivateInputField();
        }
    }
    
    string GetFetchQuestCompletionMessage()
    {
        if (currentNPC == null) return "Merci !";
        
        string formattedNPCName = TextFormatter.FormatName(currentNPC.name);
        string formattedObjectName = TextFormatter.FormatName(pendingFetchObjectName);
        
        // L'IA devrait gérer ces messages, mais on garde un fallback minimal
        return $"{formattedNPCName}: Excellent ! Vous avez récupéré tous les {formattedObjectName}. Merci !";
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
                if (UnifiedUIManager.Instance != null)
                {
                    UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.DialogueHistory);
                }
                else
                {
                    dialoguePanel.SetActive(false);
                    historyPanel.SetActive(true);
                }
                
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
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateBack();
        }
        else
        {
            if (historyPanel != null)
                historyPanel.SetActive(false);
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);
        }
    }
    
    IEnumerator AnimateButtonPress(Button button)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            Color normalColor = colors.normalColor;
            
            button.targetGraphic.color = colors.pressedColor;
            yield return new WaitForSeconds(0.1f);
            button.targetGraphic.color = normalColor;
        }
    }
    
    public bool IsDialogueOpen()
    {
        return dialoguePanel != null && dialoguePanel.activeInHierarchy;
    }
    
    public void CloseDialogue()
    {
        ClearPendingQuests();
        
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateBack();
        }
        else
        {
            dialoguePanel.SetActive(false);
            FindFirstObjectByType<PlayerControllerCC>()?.EnableControls(true);
        }
        
        // REPREND le mouvement et réaffiche les noms
        NPCMovement[] allNPCMovements = FindObjectsByType<NPCMovement>(FindObjectsSortMode.None);
        NPCNameDisplay[] allNameDisplays = FindObjectsByType<NPCNameDisplay>(FindObjectsSortMode.None);

        foreach (NPCMovement movement in allNPCMovements)
        {
            movement.ResumeMovement();
        }

        foreach (NPCNameDisplay nameDisplay in allNameDisplays)
        {
            nameDisplay.ShowName();
        }
        
        // Reset
        SetAIElementsVisibility(false);
        SetQuestButtonsVisibility(false);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        // Reset le flag
        isSendingMessage = false;

        // Vide le texte pour qu'a la prochaine ouverture le panel ne flashe
        // pas avec l'ancien dialogue. Ceinture+bretelles avec le clear en
        // tete de StartDialogue.
        if (dialogueText != null) dialogueText.text = "";
        currentFullText = "";
    }
}
