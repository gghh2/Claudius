using UnityEngine;

/// <summary>
/// Extensions pour intégrer facilement le Journal d'Aventure dans les autres systèmes
/// </summary>
public static class AdventureJournalExtensions
{
    /// <summary>
    /// Log quand une quête est acceptée (appelé depuis DialogueUI)
    /// </summary>
    public static void LogQuestAccepted(string questTitle, string npcName)
    {
        if (AdventureJournalUI.Instance != null)
        {
            string formattedTitle = TextFormatter.FormatName(questTitle);
            string formattedNPC = TextFormatter.FormatName(npcName);
            AdventureJournalUI.Instance.LogQuestAccepted(formattedTitle, formattedNPC);
        }
    }
    
    /// <summary>
    /// Log quand une quête est complétée (appelé depuis QuestJournal)
    /// </summary>
    public static void LogQuestCompleted(string questTitle)
    {
        if (AdventureJournalUI.Instance != null)
        {
            string formattedTitle = TextFormatter.FormatName(questTitle);
            AdventureJournalUI.Instance.LogQuestCompleted(formattedTitle);
        }
    }
    
    /// <summary>
    /// Log quand le joueur entre dans une zone (appelé depuis QuestZone ou triggers)
    /// </summary>
    public static void LogZoneDiscovered(string zoneName)
    {
        if (AdventureJournalUI.Instance != null)
        {
            string formattedZone = TextFormatter.FormatName(zoneName);
            AdventureJournalUI.Instance.LogZoneEntered(formattedZone);
        }
    }
    
    /// <summary>
    /// Log quand le joueur ramasse un objet important (appelé depuis QuestObject)
    /// </summary>
    public static void LogItemCollected(string itemName, int quantity = 1)
    {
        if (AdventureJournalUI.Instance != null)
        {
            string formattedItem = TextFormatter.FormatName(itemName);
            AdventureJournalUI.Instance.LogItemFound(formattedItem, quantity);
        }
    }
    
    /// <summary>
    /// Log la première rencontre avec un NPC (appelé depuis DialogueUI)
    /// </summary>
    public static void LogFirstMeeting(NPCData npcData)
    {
        if (AdventureJournalUI.Instance != null && npcData != null)
        {
            string formattedName = TextFormatter.FormatName(npcData.name);
            string formattedRole = TextFormatter.FormatName(npcData.role);
            AdventureJournalUI.Instance.LogFirstMeeting(formattedName, formattedRole);
        }
    }
    
    /// <summary>
    /// Log un événement personnalisé
    /// </summary>
    public static void LogCustom(string eventDescription)
    {
        if (AdventureJournalUI.Instance != null)
        {
            AdventureJournalUI.Instance.LogCustomEvent(eventDescription);
        }
    }
}

/// <summary>
/// Classe pour gérer l'intégration automatique du journal dans les événements du jeu
/// </summary>
public class AdventureJournalIntegration : MonoBehaviour
{
    private static AdventureJournalIntegration instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // S'abonner aux événements du jeu si nécessaire
        // Par exemple, on pourrait écouter des événements globaux
    }
    
    /// <summary>
    /// Méthode utilitaire pour vérifier si c'est la première fois qu'on fait quelque chose
    /// </summary>
    public static bool IsFirstTime(string key)
    {
        return PlayerPrefs.GetInt($"Journal_FirstTime_{key}", 0) == 0;
    }
    
    /// <summary>
    /// Marquer qu'on a déjà fait quelque chose
    /// </summary>
    public static void MarkAsDone(string key)
    {
        PlayerPrefs.SetInt($"Journal_FirstTime_{key}", 1);
        PlayerPrefs.Save();
    }
}
