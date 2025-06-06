using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestListItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questGiverText;
    public TextMeshProUGUI questLocationText;
    public TextMeshProUGUI questProgressText;
    public Button questButton; // Pour cliquer sur la quête
    public Image backgroundImage; // Pour changer la couleur de fond
    
    private JournalQuest linkedQuest;
    
    void Start()
    {
        // Setup du bouton de clic
        if (questButton != null)
        {
            questButton.onClick.AddListener(OnQuestClicked);
        }
    }
    
    // Méthode appelée pour configurer cette ligne avec une quête
    public void SetupQuest(JournalQuest quest)
    {
        linkedQuest = quest;
        
        // Affiche les informations de la quête
        if (questTitleText != null)
            questTitleText.text = quest.questTitle;
            
        if (questGiverText != null)
            questGiverText.text = $"👤 {quest.giverNPCName}";
            
        if (questLocationText != null)
            questLocationText.text = $"📍 {quest.zoneName}";
            
        if (questProgressText != null)
            questProgressText.text = $"📊 {quest.GetProgressText()}";
        
        // Change la couleur de fond selon le statut
        UpdateAppearance();
    }
    
    void UpdateAppearance()
    {
        if (linkedQuest == null || backgroundImage == null) return;
        
        // Couleur de fond selon le statut
        Color bgColor = linkedQuest.GetStatusColor();
        bgColor.a = 0.3f; // Rend semi-transparent
        backgroundImage.color = bgColor;
        
        // Change la couleur du titre aussi
        if (questTitleText != null)
        {
            questTitleText.color = linkedQuest.GetStatusColor();
        }
    }
    
    // Appelé quand on clique sur cette quête
    void OnQuestClicked()
    {
        if (linkedQuest != null && QuestJournalUI.Instance != null)
        {
            // Affiche les détails de cette quête
            QuestJournalUI.Instance.ShowQuestDetails(linkedQuest);
            
            Debug.Log($"🎯 Quête sélectionnée: {linkedQuest.questTitle}");
        }
    }
    
    // Méthode pour mettre à jour l'affichage si la quête change
    public void RefreshDisplay()
    {
        if (linkedQuest != null)
        {
            SetupQuest(linkedQuest);
        }
    }
}