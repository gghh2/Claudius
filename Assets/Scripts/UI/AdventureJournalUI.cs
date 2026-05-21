using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// Journal de bord narratif du joueur, enrichi par l'IA
/// </summary>
public class AdventureJournalUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Panel principal du journal")]
    public GameObject journalPanel;
    
    [Tooltip("Titre du journal")]
    public TextMeshProUGUI journalTitle;
    
    [Tooltip("Contenu du journal")]
    public TextMeshProUGUI journalContent;
    
    [Tooltip("Indicateur de chargement")]
    public GameObject loadingIndicator;
    
    [Tooltip("Bouton fermer")]
    public Button closeButton;
    
    [Tooltip("Bouton rafraîchir")]
    public Button refreshButton;
    
    [Tooltip("ScrollRect pour le contenu")]
    public ScrollRect scrollRect;
    
    [Header("Journal Settings")]
    [Tooltip("Nombre maximum d'entrées")]
    public int maxEntries = 50;
    
    [Tooltip("Délai minimum entre les mises à jour IA (secondes)")]
    public float aiUpdateCooldown = 30f;
    
    // Journal entries
    private List<JournalEntry> journalEntries = new List<JournalEntry>();
    private float lastAIUpdateTime = -999f;
    private bool isUpdatingJournal = false;
    
    // Events tracking
    private List<string> pendingEvents = new List<string>();
    
    public static AdventureJournalUI Instance { get; private set; }
    
    [System.Serializable]
    public class JournalEntry
    {
        public string timestamp;
        public string content;
        public bool isAIGenerated;

        // Constructeur sans argument requis par JsonUtility (chargement de sauvegarde).
        public JournalEntry() { }

        public JournalEntry(string content, bool isAI = false)
        {
            this.timestamp = System.DateTime.Now.ToString("HH:mm");
            this.content = content;
            this.isAIGenerated = isAI;
        }
    }
    
    void Awake()
    {
        // Contrôleur UI vivant sur le GameObject "UI" (toujours actif), comme les
        // autres scripts UI du projet. Pas de DontDestroyOnLoad : le journal
        // n'existe que dans la scène Game.
        Instance = this;
    }
    
    void Start()
    {
        if (journalPanel != null)
            journalPanel.SetActive(false);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseJournal);
            
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshJournalWithAI);
            
        // Ajouter l'entrée initiale
        AddEntry("Votre aventure commence ici...", true);
        
        // S'abonner aux événements du jeu
        SubscribeToGameEvents();
    }
    
    // NB : l'ouverture/fermeture par la touche L est gérée par UnifiedUIManager
    // (raccourci unique pour toute la navigation UI).

    void SubscribeToGameEvents()
    {
        // Ici on pourrait s'abonner à différents événements du jeu
        // Pour l'instant, on va créer des méthodes publiques que les autres systèmes peuvent appeler
    }
    
    /// <summary>
    /// Ouvre le journal (via UnifiedUIManager).
    /// </summary>
    public void OpenJournal()
    {
        if (UnifiedUIManager.Instance != null)
            UnifiedUIManager.Instance.NavigateTo("AdventureJournal");
        else if (journalPanel != null)
            journalPanel.SetActive(true);
    }

    /// <summary>
    /// Appelé par AdventureJournalPanelHelper quand le panneau devient visible.
    /// Le contrôleur vit sur le GameObject "UI" : il ne reçoit pas OnEnable
    /// lui-même quand le panneau s'affiche.
    /// </summary>
    public void OnPanelShown()
    {
        UpdateJournalDisplay();

        // Scroll en bas pour voir les dernières entrées
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        // Tente une mise à jour IA si des événements sont en attente
        if (Time.time - lastAIUpdateTime > aiUpdateCooldown && pendingEvents.Count > 0)
        {
            RefreshJournalWithAI();
        }
    }
    
    /// <summary>
    /// Ferme le journal
    /// </summary>
    public void CloseJournal()
    {
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateBack();
        }
        else
        {
            journalPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Vérifie si le journal est ouvert
    /// </summary>
    public bool IsJournalOpen()
    {
        return journalPanel != null && journalPanel.activeInHierarchy;
    }
    
    /// <summary>
    /// Ajoute une entrée simple au journal
    /// </summary>
    public void AddEntry(string content, bool isAIGenerated = false)
    {
        var entry = new JournalEntry(content, isAIGenerated);
        journalEntries.Add(entry);
        
        // Limiter le nombre d'entrées
        if (journalEntries.Count > maxEntries)
        {
            journalEntries.RemoveAt(0);
        }
        
        if (IsJournalOpen())
        {
            UpdateJournalDisplay();
        }
    }
    
    /// <summary>
    /// Enregistre un événement pour traitement IA ultérieur
    /// </summary>
    public void LogGameEvent(string eventDescription)
    {
        pendingEvents.Add(eventDescription);
        Debug.Log($"[Journal] Événement enregistré: {eventDescription}");
        
        // Si le journal est ouvert et qu'on peut faire une mise à jour
        if (IsJournalOpen() && Time.time - lastAIUpdateTime > aiUpdateCooldown)
        {
            RefreshJournalWithAI();
        }
    }
    
    /// <summary>
    /// Met à jour l'affichage du journal
    /// </summary>
    void UpdateJournalDisplay()
    {
        if (journalContent == null) return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<size=18><b>Journal de Bord</b></size>\n");
        
        foreach (var entry in journalEntries)
        {
            if (entry == null) continue;
            if (entry.isAIGenerated)
            {
                // Entrées IA en italique avec une couleur différente
                sb.AppendLine($"<color=#E8DCC0><i>[{entry.timestamp}] {entry.content}</i></color>\n");
            }
            else
            {
                // Entrées système normales
                sb.AppendLine($"<color=#CCCCCC>[{entry.timestamp}] {entry.content}</color>\n");
            }
        }

        journalContent.text = sb.ToString();
    }
    
    /// <summary>
    /// Rafraîchit le journal avec une narration IA
    /// </summary>
    public void RefreshJournalWithAI()
    {
        if (isUpdatingJournal || pendingEvents.Count == 0) return;
        
        if (!AIDialogueManager.Instance || !AIDialogueManager.Instance.IsConfigured())
        {
            Debug.LogError("[Journal] IA non configurée");
            return;
        }
        
        StartCoroutine(UpdateJournalWithAI());
    }
    
    IEnumerator UpdateJournalWithAI()
    {
        isUpdatingJournal = true;
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
            
        if (refreshButton != null)
            refreshButton.interactable = false;
        
        // Construire le prompt pour l'IA
        string prompt = BuildAIPrompt();
        
        // Envoyer à l'API OpenAI
        yield return StartCoroutine(GetAINarration(prompt));
        
        // Nettoyer
        pendingEvents.Clear();
        lastAIUpdateTime = Time.time;
        isUpdatingJournal = false;
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
            
        if (refreshButton != null)
            refreshButton.interactable = true;
    }
    
    string BuildAIPrompt()
    {
        StringBuilder prompt = new StringBuilder();
        
        prompt.AppendLine("Tu rédiges le journal de bord intime du JOUEUR, dans un univers de space opera.");
        prompt.AppendLine("Le joueur explore une planète alien parsemée de ruines anciennes.");
        prompt.AppendLine();
        prompt.AppendLine("RÈGLES IMPÉRATIVES :");
        prompt.AppendLine("- Écris à la première personne (« je »). Le narrateur est le JOUEUR lui-même.");
        prompt.AppendLine("- Le joueur n'est AUCUN des personnages cités dans les événements. Les PNJ rencontrés sont d'AUTRES personnes — ne deviens jamais l'un d'eux.");
        prompt.AppendLine("- Un objet reste un objet : ne transforme jamais un objet (outil, cristal...) en personnage.");
        prompt.AppendLine("- N'invente RIEN : aucun nouveau personnage, lieu, technologie ni événement absent de la liste ci-dessous. Tu enjolives le style, jamais les faits.");
        prompt.AppendLine("- Ton immersif et personnel, mais fidèle aux faits. Maximum 3 à 4 phrases.");
        prompt.AppendLine();
        prompt.AppendLine("Événements à raconter — et RIEN d'autre :");

        foreach (string evt in pendingEvents)
        {
            prompt.AppendLine($"- {evt}");
        }

        prompt.AppendLine();
        prompt.AppendLine("Rédige une courte entrée de journal à la première personne, fidèle à ces événements.");
        
        return prompt.ToString();
    }
    
    IEnumerator GetAINarration(string prompt)
    {
        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system", "Tu es un narrateur talentueux qui écrit des entrées de journal immersives."),
            new OpenAIMessage("user", prompt)
        };

        // Passe par l'abstraction IA (AIService) ; le modèle est celui du provider actif.
        var request = new AIRequest(messages, 0.8f, 400);
        yield return StartCoroutine(AIService.Provider.Complete(request, OnNarrationReceived));
    }

    void OnNarrationReceived(AIResponse response)
    {
        if (response.success)
        {
            string narration = response.text.Trim();
            AddEntry(narration, true);
            UpdateJournalDisplay();

            // Auto-scroll vers le bas
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        else
        {
            Debug.LogError($"[Journal] Erreur IA : {response.error}");
            AddEntry("*Les pages du journal semblent floues, l'écriture devient illisible...*", true);
        }
    }
    
    #region Event Logging Methods
    
    /// <summary>
    /// Log quand le joueur accepte une quête
    /// </summary>
    public void LogQuestAccepted(string questTitle, string npcName)
    {
        LogGameEvent($"J'ai accepté une mission de {npcName}: {questTitle}");
    }
    
    /// <summary>
    /// Log quand le joueur complète une quête
    /// </summary>
    public void LogQuestCompleted(string questTitle)
    {
        LogGameEvent($"Mission accomplie: {questTitle}");
    }
    
    /// <summary>
    /// Log quand le joueur entre dans une nouvelle zone
    /// </summary>
    public void LogZoneEntered(string zoneName)
    {
        LogGameEvent($"J'ai découvert une nouvelle zone: {zoneName}");
    }
    
    /// <summary>
    /// Log quand le joueur trouve un objet important
    /// </summary>
    public void LogItemFound(string itemName, int quantity = 1)
    {
        if (quantity > 1)
            LogGameEvent($"J'ai trouvé {quantity} {itemName}");
        else
            LogGameEvent($"J'ai trouvé un {itemName}");
    }
    
    /// <summary>
    /// Log quand le joueur parle à un NPC pour la première fois
    /// </summary>
    public void LogFirstMeeting(string npcName, string npcRole)
    {
        LogGameEvent($"J'ai rencontré {npcName}, un {npcRole}");
    }
    
    /// <summary>
    /// Log quand le joueur meurt ou échoue
    /// </summary>
    public void LogPlayerDeath(string cause = "")
    {
        if (string.IsNullOrEmpty(cause))
            LogGameEvent("L'obscurité m'a englouti...");
        else
            LogGameEvent($"J'ai succombé à {cause}");
    }
    
    /// <summary>
    /// Log un événement personnalisé
    /// </summary>
    public void LogCustomEvent(string eventDescription)
    {
        LogGameEvent(eventDescription);
    }
    
    #endregion
    
    #region Save/Load Support
    
    [System.Serializable]
    public class JournalSaveData
    {
        public List<JournalEntry> entries;
        public List<string> pendingEvents;

        // Constructeur sans argument requis par JsonUtility (chargement de sauvegarde).
        public JournalSaveData() { }

        public JournalSaveData(List<JournalEntry> entries, List<string> events)
        {
            this.entries = new List<JournalEntry>(entries);
            this.pendingEvents = new List<string>(events);
        }
    }
    
    public JournalSaveData GetSaveData()
    {
        return new JournalSaveData(journalEntries, pendingEvents);
    }
    
    public void LoadSaveData(JournalSaveData data)
    {
        if (data == null) return;

        journalEntries = data.entries != null
            ? new List<JournalEntry>(data.entries)
            : new List<JournalEntry>();
        pendingEvents = data.pendingEvents != null
            ? new List<string>(data.pendingEvents)
            : new List<string>();

        UpdateJournalDisplay();
    }
    
    #endregion
}
