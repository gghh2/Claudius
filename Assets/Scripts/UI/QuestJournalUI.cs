using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuestJournalUI : MonoBehaviour
{
    public static QuestJournalUI Instance { get; private set; }
    
    [Header("UI Elements")]
    public GameObject journalPanel;
    public Button closeButton;
    
    [Header("Navigation Tabs")]
    public Button activeQuestsTab;
    public Button completedQuestsTab;
    public Button cancelledQuestsTab;
    
    [Header("Quest Display")]
    public Transform questListParent; // Parent pour la liste des quêtes
    public GameObject questItemPrefab; // Prefab pour chaque quête
    public TextMeshProUGUI questCountText; // "Quêtes actives: 3/10"
    
    [Header("Quest Details")]
    public GameObject questDetailsPanel;
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI questGiverText;
    public TextMeshProUGUI questProgressText;
    public TextMeshProUGUI questStatusText;
    public Button cancelQuestButton;
    
    private QuestStatus currentTab = QuestStatus.InProgress;
    private bool isJournalOpen = false;
    private JournalQuest selectedQuest = null;
    
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
        // Cache le journal au départ
        if (journalPanel != null)
            journalPanel.SetActive(false);
        
        if (questDetailsPanel != null)
            questDetailsPanel.SetActive(false);
        else
            Debug.LogError("[QuestJournalUI] questDetailsPanel n'est pas assigné ! Créez-le dans l'éditeur.");
        
        // Setup des boutons
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseJournal);
            
        if (activeQuestsTab != null)
            activeQuestsTab.onClick.AddListener(() => SwitchTab(QuestStatus.InProgress));
            
        if (completedQuestsTab != null)
            completedQuestsTab.onClick.AddListener(() => SwitchTab(QuestStatus.Completed));
            
        if (cancelledQuestsTab != null)
            cancelledQuestsTab.onClick.AddListener(() => SwitchTab(QuestStatus.Cancelled));
            
        if (cancelQuestButton != null)
            cancelQuestButton.onClick.AddListener(CancelSelectedQuest);
    }
    
    void Update()
	{
	    // Vérifie qu'on n'est pas en train de taper dans un InputField
	    bool isTyping = false;
	    
	    // Vérifie si DialogueUI est ouvert et si l'input field est focus
	    if (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueOpen())
	    {
	        // Vérifie si le joueur tape dans l'input field
	        if (DialogueUI.Instance.playerInputField != null && 
	            DialogueUI.Instance.playerInputField.isFocused)
	        {
	            isTyping = true;
	        }
	    }
	    
	    // Touche J pour ouvrir/fermer le journal SEULEMENT si pas en train de taper
	    if (Input.GetKeyDown(KeyCode.J) && !isTyping)
	    {
	        if (isJournalOpen)
	            CloseJournal();
	        else
	            OpenJournal();
	    }
	    
	    // Échap pour fermer si ouvert
	    if (Input.GetKeyDown(KeyCode.Escape) && isJournalOpen)
	    {
	        CloseJournal();
	    }
	}
    
    public void OpenJournal()
    {
        if (journalPanel != null)
        {
            journalPanel.SetActive(true);
            isJournalOpen = true;
            
            // Notify UIManager
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPanelState(UIPanelNames.QuestJournal, true);
            }
            
            // Désactive le mouvement du joueur
            PlayerControllerCC player = FindObjectOfType<PlayerControllerCC>();
            if (player != null)
                player.enabled = false;
            
            // Affiche les quêtes actives par défaut
            SwitchTab(QuestStatus.InProgress);
            
            Debug.Log("📖 Journal de quêtes ouvert");
        }
    }
    
    public void CloseJournal()
    {
        if (journalPanel != null)
        {
            journalPanel.SetActive(false);
            isJournalOpen = false;
            
            // Notify UIManager
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPanelState(UIPanelNames.QuestJournal, false);
            }
            
            // Réactive le mouvement du joueur
            PlayerControllerCC player = FindObjectOfType<PlayerControllerCC>();
            if (player != null)
                player.enabled = true;
            
            // Cache les détails
            if (questDetailsPanel != null)
                questDetailsPanel.SetActive(false);
            
            Debug.Log("📖 Journal de quêtes fermé");
        }
    }
    
    public void SwitchTab(QuestStatus status)
    {
        currentTab = status;
        RefreshQuestList();
        UpdateTabAppearance();
        
        // Cache les détails quand on change d'onglet
        if (questDetailsPanel != null)
            questDetailsPanel.SetActive(false);
    }
    
    public void RefreshCurrentTab()
    {
        RefreshQuestList();
    }
    
    void RefreshQuestList()
    {
        // Nettoie la liste actuelle
        if (questListParent != null)
        {
            foreach (Transform child in questListParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (QuestJournal.Instance == null) return;
        
        // Récupère les quêtes selon l'onglet actuel
        List<JournalQuest> questsToShow = new List<JournalQuest>();
        string tabName = "";
        
        switch (currentTab)
        {
            case QuestStatus.InProgress:
                questsToShow = QuestJournal.Instance.GetActiveQuests();
                tabName = "En cours";
                break;
            case QuestStatus.Completed:
                questsToShow = QuestJournal.Instance.GetCompletedQuests();
                tabName = "Terminées";
                break;
            case QuestStatus.Cancelled:
                questsToShow = QuestJournal.Instance.GetCancelledQuests();
                tabName = "Annulées";
                break;
        }
        
        // Met à jour le compteur
        if (questCountText != null)
        {
            questCountText.text = $"Quêtes {tabName}: {questsToShow.Count}";
        }
        
        // Crée les éléments de liste
        foreach (JournalQuest quest in questsToShow)
        {
            CreateQuestListItem(quest);
        }
        
        Debug.Log($"📋 Affichage de {questsToShow.Count} quêtes {tabName}");
    }
    
    void CreateQuestListItem(JournalQuest quest)
    {
        if (questItemPrefab == null || questListParent == null) return;
        
        GameObject questItem = Instantiate(questItemPrefab, questListParent);
        
        // Configure l'élément (nous créerons le prefab après)
        QuestListItem questComponent = questItem.GetComponent<QuestListItem>();
        if (questComponent != null)
        {
            questComponent.SetupQuest(quest);
        }
    }
    
    void UpdateTabAppearance()
    {
        // Utilise l'état interactable pour montrer quel onglet est actif
        // L'onglet actif devient non-interactable (utilisera la couleur Disabled du ColorBlock)
        
        if (activeQuestsTab != null)
            activeQuestsTab.interactable = (currentTab != QuestStatus.InProgress);
        
        if (completedQuestsTab != null)
            completedQuestsTab.interactable = (currentTab != QuestStatus.Completed);
        
        if (cancelledQuestsTab != null)
            cancelledQuestsTab.interactable = (currentTab != QuestStatus.Cancelled);
    }
    
    // Méthode appelée quand on clique sur une quête dans la liste
    public void ShowQuestDetails(JournalQuest quest)
    {
        if (questDetailsPanel == null) return;
        
        selectedQuest = quest;
        questDetailsPanel.SetActive(true);
        
        if (questTitleText != null)
            questTitleText.text = quest.questTitle;
            
        if (questDescriptionText != null)
            questDescriptionText.text = quest.description;
            
        if (questGiverText != null)
            questGiverText.text = $"Donneur de quête: {TextFormatter.FormatName(quest.giverNPCName)}";
            
        if (questProgressText != null)
            questProgressText.text = $"Progression: {quest.GetProgressText()}";
            
        if (questStatusText != null)
        {
            questStatusText.text = quest.GetStatusText();
            questStatusText.color = quest.GetStatusColor();
        }
        
        // Affiche le bouton d'annulation seulement pour les quêtes en cours
        if (cancelQuestButton != null)
        {
            cancelQuestButton.gameObject.SetActive(quest.status == QuestStatus.InProgress);
        }
        
        Debug.Log($"📄 Détails affichés pour: {quest.questTitle}");
    }
    
    public bool IsJournalOpen()
    {
        return isJournalOpen;
    }
    
    void CancelSelectedQuest()
    {
        if (selectedQuest != null && selectedQuest.status == QuestStatus.InProgress)
        {
            // Demande confirmation (simple pour l'instant)
            Debug.Log($"🔄 Annulation de la quête: {selectedQuest.questTitle}");
            
            // Annule la quête
            if (QuestJournal.Instance != null)
            {
                QuestJournal.Instance.CancelQuest(selectedQuest.questId);
            }
            
            // Cache les détails et rafraîchit la liste
            questDetailsPanel.SetActive(false);
            selectedQuest = null;
            RefreshQuestList();
        }
    }
}