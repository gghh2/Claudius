using UnityEngine;
using System.Linq;

public class QuestDebugger : MonoBehaviour
{
    public static QuestDebugger Instance;
    
    [Header("Debug Settings")]
    public KeyCode debugKey = KeyCode.F9;
    public KeyCode forceCompleteKey = KeyCode.F10;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // Vérifie d'abord si le debug quest est activé
        if (!GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest)) 
            return;
            
        if (Input.GetKeyDown(debugKey))
        {
            ShowQuestDebugInfo();
        }
        
        if (Input.GetKeyDown(forceCompleteKey))
        {
            ForceCompleteActiveQuest();
        }
    }
    
    void ShowQuestDebugInfo()
    {
        Debug.Log("=== QUEST DEBUG INFO ===");
        
        // Affiche les quêtes actives
        if (QuestJournal.Instance != null)
        {
            var activeQuests = QuestJournal.Instance.GetActiveQuests();
            Debug.Log($"📋 Quêtes actives: {activeQuests.Count}");
            
            foreach (var quest in activeQuests)
            {
                Debug.Log($"\n--- Quête: {quest.questTitle} ---");
                Debug.Log($"ID: {quest.questId}");
                Debug.Log($"Type: {quest.questType}");
                Debug.Log($"Description: {quest.description}");
                Debug.Log($"Donneur: {quest.giverNPCName}");
                Debug.Log($"Progression: {quest.currentProgress}/{quest.maxProgress}");
                Debug.Log($"Zone: {quest.zoneName}");
                Debug.Log($"Statut: {quest.status}");
                
                // Pour les quêtes FETCH, vérifie l'inventaire
                if (quest.questType == QuestType.FETCH)
                {
                    string objectName = ExtractObjectName(quest.description);
                    if (PlayerInventory.Instance != null)
                    {
                        int count = PlayerInventory.Instance.GetItemQuantity(objectName, quest.questId);
                        Debug.Log($"📦 Inventaire: {count} {objectName}");
                    }
                }
            }
        }
        
        // Affiche l'inventaire complet
        if (PlayerInventory.Instance != null)
        {
            Debug.Log("\n=== INVENTAIRE ===");
            PlayerInventory.Instance.ShowInventory();
        }
        
        // Affiche les quêtes actives dans QuestManager
        if (QuestManager.Instance != null)
        {
            Debug.Log($"\n=== QUEST MANAGER ===");
            Debug.Log($"Quêtes actives: {QuestManager.Instance.activeQuests.Count}");
            foreach (var quest in QuestManager.Instance.activeQuests)
            {
                Debug.Log($"- {quest.questData.description} (Complétée: {quest.isCompleted})");
            }
        }
    }
    
    void ForceCompleteActiveQuest()
    {
        if (QuestJournal.Instance == null) return;
        
        var activeQuests = QuestJournal.Instance.GetActiveQuests();
        if (activeQuests.Count > 0)
        {
            var quest = activeQuests[0];
            
            Debug.Log($"🔧 FORCE COMPLETE: {quest.questTitle}");
            
            // Force la progression au maximum
            quest.currentProgress = quest.maxProgress;
            QuestJournal.Instance.UpdateQuestProgress(quest.questId, quest.maxProgress);
            
            // Pour les quêtes FETCH, ajoute les objets à l'inventaire
            if (quest.questType == QuestType.FETCH && PlayerInventory.Instance != null)
            {
                string objectName = ExtractObjectName(quest.description);
                for (int i = 0; i < quest.maxProgress; i++)
                {
                    PlayerInventory.Instance.AddItem(objectName, 1, quest.questId);
                }
                Debug.Log($"✅ Ajouté {quest.maxProgress} {objectName} à l'inventaire");
            }
            
            Debug.Log("✅ Quête forcée comme complétée ! Retournez voir le PNJ.");
        }
        else
        {
            Debug.Log("❌ Aucune quête active à forcer");
        }
    }
    
    string ExtractObjectName(string description)
    {
        // Format: "Trouvez X objet_name dans zone"
        string[] words = description.Split(' ');
        for (int i = 0; i < words.Length - 2; i++)
        {
            if (words[i].ToLower() == "trouvez" && int.TryParse(words[i + 1], out _))
            {
                return words[i + 2];
            }
        }
        
        // Essaye d'autres formats
        if (description.Contains("artefact"))
            return "artefact";
        if (description.Contains("cristal"))
            return "cristal";
        
        return "objet_inconnu";
    }
    
    void OnGUI()
    {
        // Affiche seulement si le debug quest est activé
        if (!GlobalDebugManager.IsDebugEnabled(DebugSystem.Quest)) 
            return;
            
        // Affiche les raccourcis en haut à droite
        GUI.Box(new Rect(Screen.width - 250, 10, 240, 60), "Quest Debug");
        GUI.Label(new Rect(Screen.width - 245, 30, 230, 20), "F9 - Afficher infos quêtes");
        GUI.Label(new Rect(Screen.width - 245, 50, 230, 20), "F10 - Forcer complétion quête");
    }
}
