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
    public Button trackButton; // Pour suivre/arrêter de suivre la quête
    public Image trackButtonIcon; // Icône du bouton de suivi
    public Image backgroundImage; // Pour changer la couleur de fond
    
    private JournalQuest linkedQuest;
    
    void Start()
    {
        // Setup du bouton de clic
        if (questButton != null)
            questButton.onClick.AddListener(OnQuestClicked);
        
        // Setup du bouton de suivi
        if (trackButton != null)
            trackButton.onClick.AddListener(OnTrackButtonClicked);
    }
    
    // Méthode appelée pour configurer cette ligne avec une quête
    public void SetupQuest(JournalQuest quest)
    {
        linkedQuest = quest;
        
        // NOUVEAU : Force le style des textes (pas de gras)
        RemoveBoldFromTexts();
        
        // Affiche les informations de la quête
        if (questTitleText != null)
            questTitleText.text = quest.questTitle; // Déjà formaté dans JournalQuest
            
        if (questGiverText != null)
            questGiverText.text = $"Donneur : {quest.giverNPCName}";

        if (questLocationText != null)
            questLocationText.text = $"Lieu : {quest.zoneName}";

        if (questProgressText != null)
            questProgressText.text = $"Progression : {quest.GetProgressText()}";
        
        // Change la couleur de fond selon le statut
        UpdateAppearance();
        
        // Met à jour le bouton de suivi
        UpdateTrackButton();
    }
    
    // NOUVEAU : Méthode pour enlever le gras de tous les textes
    void RemoveBoldFromTexts()
    {
        TextMeshProUGUI[] allTexts = { questTitleText, questGiverText, questLocationText, questProgressText };
        
        foreach (var textComponent in allTexts)
        {
            if (textComponent != null)
            {
                // Enlève le style Bold
                textComponent.fontStyle &= ~FontStyles.Bold;
                
                // Si vous voulez aussi enlever l'outline
                var outline = textComponent.GetComponent<UnityEngine.UI.Outline>();
                if (outline != null)
                {
                    outline.enabled = false;
                }
            }
        }
    }
    
    void UpdateAppearance()
    {
        if (linkedQuest == null || backgroundImage == null) return;
        
        // Vérifie si cette quête est actuellement suivie
        bool isTracked = QuestJournal.Instance != null && QuestJournal.Instance.IsQuestTracked(linkedQuest.questId);
        
        // Couleur de fond selon le statut ET si elle est suivie
        Color bgColor;
        if (isTracked && linkedQuest.status == QuestStatus.InProgress)
        {
            bgColor = QuestSystemConfig.TrackedQuestBackgroundColor;
        }
        else
        {
            bgColor = linkedQuest.GetStatusColor();
            bgColor.a = QuestSystemConfig.NormalQuestBackgroundColor.a;
        }
        backgroundImage.color = bgColor;
        
        // Change la couleur du titre aussi
        if (questTitleText != null)
            questTitleText.color = linkedQuest.GetStatusColor();
    }
    
    // Appelé quand on clique sur cette quête
    void OnQuestClicked()
    {
        if (linkedQuest != null && QuestJournalUI.Instance != null)
        {
            QuestJournalUI.Instance.ShowQuestDetails(linkedQuest);
        }
    }
    
    // Appelé quand on clique sur le bouton de suivi
    void OnTrackButtonClicked()
    {
        if (linkedQuest == null || linkedQuest.status != QuestStatus.InProgress) return;
        
        // Ne rien faire si déjà suivie
        if (QuestJournal.Instance.IsQuestTracked(linkedQuest.questId)) return;
        
        // Définir comme quête suivie
        QuestJournal.Instance.SetTrackedQuest(linkedQuest.questId);
        
        // Rafraîchit l'affichage
        if (QuestJournalUI.Instance != null)
            QuestJournalUI.Instance.RefreshCurrentTab();
    }
    
    // Met à jour l'apparence du bouton de suivi
    void UpdateTrackButton()
    {
        if (trackButton == null) return;
        
        // Cache le bouton si la quête n'est pas en cours
        if (linkedQuest == null || linkedQuest.status != QuestStatus.InProgress)
        {
            trackButton.gameObject.SetActive(false);
            return;
        }
        
        trackButton.gameObject.SetActive(true);
        
        // Change l'apparence selon si la quête est suivie
        bool isTracked = QuestJournal.Instance.IsQuestTracked(linkedQuest.questId);
        
        if (trackButtonIcon != null)
        {
            // Configure l'icône comme un cercle vide ou plein
            trackButtonIcon.color = isTracked ? QuestSystemConfig.TrackedButtonColor : QuestSystemConfig.UntrackedButtonColor;
            
            // Si l'Image a un sprite, on peut utiliser fillCenter pour vide/plein
            // Sinon on change la transparence ou utilise un sprite différent
            trackButtonIcon.fillCenter = isTracked;
            
            // Alternative: Change l'alpha pour simuler vide/plein
            Color iconColor = trackButtonIcon.color;
            iconColor.a = isTracked ? 1f : 0.3f; // Plein = opaque, Vide = semi-transparent
            trackButtonIcon.color = iconColor;
        }
        
        // Optionnel : Change le texte du tooltip si vous avez un système de tooltip
        // var tooltip = trackButton.GetComponent<YourTooltipComponent>();
        // if (tooltip != null)
        // {
        //     tooltip.text = isTracked ? "Arrêter de suivre" : "Suivre cette quête";
        // }
        
        // Ou change directement le texte si c'est un bouton texte
        var buttonText = trackButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            // Utilise des caractères Unicode pour cercle vide/plein
            buttonText.text = isTracked ? "●" : "○"; // Cercle plein vs cercle vide
            buttonText.color = isTracked ? QuestSystemConfig.TrackedButtonColor : QuestSystemConfig.UntrackedButtonColor;
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