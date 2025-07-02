using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI controller for the quest journal system
/// Handles display of active, completed, and cancelled quests
/// </summary>
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
    [SerializeField] private Transform questListParent;
    [SerializeField] private GameObject questItemPrefab;
    [SerializeField] private TextMeshProUGUI questCountText;
    
    [Header("Quest Details")]
    public GameObject questDetailsPanel;
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI questGiverText;
    public TextMeshProUGUI questProgressText;
    public TextMeshProUGUI questStatusText;
    public Button cancelQuestButton;
    
    // Private state
    private QuestStatus currentTab = QuestStatus.InProgress;
    private bool isJournalOpen = false;
    private JournalQuest selectedQuest = null;
    
    #region Unity Lifecycle
    
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
        // Hide panels at start
        if (journalPanel != null)
            journalPanel.SetActive(false);
        
        if (questDetailsPanel != null)
            questDetailsPanel.SetActive(false);
        
        // Setup button listeners
        SetupButtons();
    }
    
    void OnEnable()
    {
        // Auto-refresh when panel becomes active
        if (journalPanel != null && journalPanel.activeInHierarchy)
        {
            isJournalOpen = true;
            SwitchTab(QuestStatus.InProgress);
        }
    }
    
    void OnDisable()
    {
        isJournalOpen = false;
    }
    
    void Update()
    {
        // J shortcut is handled by UnifiedUIManager
    }
    
    #endregion
    
    #region Setup
    
    void SetupButtons()
    {
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
    
    #endregion
    
    #region Public Methods
    
    public void OpenJournal()
    {
        if (journalPanel != null)
        {
            if (UnifiedUIManager.Instance != null)
            {
                UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.QuestJournal);
                isJournalOpen = true;
            }
            else
            {
                journalPanel.SetActive(true);
                isJournalOpen = true;
            }
            
            SwitchTab(QuestStatus.InProgress);
        }
    }
    
    public void CloseJournal()
    {
        if (journalPanel != null)
        {
            if (UnifiedUIManager.Instance != null)
            {
                UnifiedUIManager.Instance.NavigateBack();
                isJournalOpen = false;
            }
            else
            {
                journalPanel.SetActive(false);
                isJournalOpen = false;
            }
            
            if (questDetailsPanel != null)
                questDetailsPanel.SetActive(false);
        }
    }
    
    public void SwitchTab(QuestStatus status)
    {
        currentTab = status;
        RefreshQuestList();
        UpdateTabAppearance();
        
        if (questDetailsPanel != null)
            questDetailsPanel.SetActive(false);
    }
    
    public void RefreshCurrentTab()
    {
        RefreshQuestList();
    }
    
    public void ShowQuestDetails(JournalQuest quest)
    {
        if (questDetailsPanel == null) return;
        
        selectedQuest = quest;
        questDetailsPanel.SetActive(true);
        
        // Update detail fields
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
        
        // Show cancel button only for active quests
        if (cancelQuestButton != null)
        {
            cancelQuestButton.gameObject.SetActive(quest.status == QuestStatus.InProgress);
        }
    }
    
    public bool IsJournalOpen()
    {
        return isJournalOpen;
    }
    
    #endregion
    
    #region Private Methods
    
    void RefreshQuestList()
    {
        // Clear existing items
        if (questListParent != null)
        {
            foreach (Transform child in questListParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (QuestJournal.Instance == null) return;
        
        // Get quests based on current tab
        List<JournalQuest> questsToShow = GetQuestsForCurrentTab();
        
        // Update count display
        UpdateQuestCount(questsToShow.Count);
        
        // Create list items
        foreach (JournalQuest quest in questsToShow)
        {
            CreateQuestListItem(quest);
        }
    }
    
    List<JournalQuest> GetQuestsForCurrentTab()
    {
        switch (currentTab)
        {
            case QuestStatus.InProgress:
                return QuestJournal.Instance.GetActiveQuests();
            case QuestStatus.Completed:
                return QuestJournal.Instance.GetCompletedQuests();
            case QuestStatus.Cancelled:
                return QuestJournal.Instance.GetCancelledQuests();
            default:
                return new List<JournalQuest>();
        }
    }
    
    void UpdateQuestCount(int count)
    {
        if (questCountText != null)
        {
            string tabName = GetTabName();
            questCountText.text = $"Quêtes {tabName}: {count}";
        }
    }
    
    string GetTabName()
    {
        switch (currentTab)
        {
            case QuestStatus.InProgress: return "En cours";
            case QuestStatus.Completed: return "Terminées";
            case QuestStatus.Cancelled: return "Annulées";
            default: return "";
        }
    }
    
    void CreateQuestListItem(JournalQuest quest)
    {
        if (questItemPrefab == null || questListParent == null) return;
        
        GameObject questItem = Instantiate(questItemPrefab, questListParent);
        
        QuestListItem questComponent = questItem.GetComponent<QuestListItem>();
        if (questComponent != null)
        {
            questComponent.SetupQuest(quest);
        }
    }
    
    void UpdateTabAppearance()
    {
        // Active tab becomes non-interactable (shows as selected)
        if (activeQuestsTab != null)
            activeQuestsTab.interactable = (currentTab != QuestStatus.InProgress);
        
        if (completedQuestsTab != null)
            completedQuestsTab.interactable = (currentTab != QuestStatus.Completed);
        
        if (cancelledQuestsTab != null)
            cancelledQuestsTab.interactable = (currentTab != QuestStatus.Cancelled);
    }
    
    void CancelSelectedQuest()
    {
        if (selectedQuest != null && selectedQuest.status == QuestStatus.InProgress)
        {
            if (QuestJournal.Instance != null)
            {
                QuestJournal.Instance.CancelQuest(selectedQuest.questId);
            }
            
            questDetailsPanel.SetActive(false);
            selectedQuest = null;
            RefreshQuestList();
        }
    }
    
    #endregion
}
