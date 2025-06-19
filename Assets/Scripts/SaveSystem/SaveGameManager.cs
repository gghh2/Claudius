using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Main save game system - handles saving and loading game state
/// </summary>
public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance { get; private set; }
    
    [Header("Save Settings")]
    [Tooltip("Name of the save file")]
    public string saveFileName = "savegame";
    
    [Tooltip("Auto save interval in seconds (0 = disabled)")]
    public float autoSaveInterval = 60f;
    
    [Header("Debug")]
    public bool debugMode = true;
    
    // Events
    public static event Action OnGameSaved;
    public static event Action OnGameLoaded;
    
    private float autoSaveTimer;
    private string savePath;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSavePath();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeSavePath()
    {
        savePath = Path.Combine(Application.persistentDataPath, "saves");
        
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            Debug.Log($"[SaveGame] Created save directory: {savePath}");
        }
    }
    
    void Update()
    {
        // Auto save
        if (autoSaveInterval > 0)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                autoSaveTimer = 0f;
                SaveGame("autosave");
            }
        }
    }
    
    /// <summary>
    /// Save the current game state
    /// </summary>
    public void SaveGame(string saveName = null)
    {
        if (string.IsNullOrEmpty(saveName))
            saveName = saveFileName;
        
        try
        {
            SaveData saveData = CollectSaveData();
            
            string json = JsonUtility.ToJson(saveData, true);
            string filePath = Path.Combine(savePath, saveName + ".json");
            
            File.WriteAllText(filePath, json);
            
            if (debugMode)
                Debug.Log($"[SaveGame] Game saved to: {filePath}");
            
            OnGameSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveGame] Failed to save game: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load a saved game
    /// </summary>
    public void LoadGame(string saveName = null)
    {
        if (string.IsNullOrEmpty(saveName))
            saveName = saveFileName;
        
        try
        {
            string filePath = Path.Combine(savePath, saveName + ".json");
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveGame] Save file not found: {filePath}");
                return;
            }
            
            string json = File.ReadAllText(filePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            
            ApplySaveData(saveData);
            
            if (debugMode)
                Debug.Log($"[SaveGame] Game loaded from: {filePath}");
            
            OnGameLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveGame] Failed to load game: {e.Message}");
        }
    }
    
    /// <summary>
    /// Collect all game data to save
    /// </summary>
    SaveData CollectSaveData()
    {
        SaveData data = new SaveData();
        data.saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.playTime = Time.time;
        
        // Player data
        PlayerControllerCC player = FindObjectOfType<PlayerControllerCC>();
        if (player != null)
        {
            data.playerData = new PlayerSaveData
            {
                position = player.transform.position,
                rotation = player.transform.rotation.eulerAngles,
                currentStamina = player.currentStamina,
                health = 100f // Add if you have health system
            };
        }
        
        // Companion data
        CompanionController companion = FindObjectOfType<CompanionController>();
        if (companion != null && companion.gameObject.activeSelf)
        {
            data.companionData = new CompanionSaveData
            {
                hasCompanion = true,
                companionType = companion.gameObject.name,
                position = companion.transform.position,
                rotation = companion.transform.rotation.eulerAngles
            };
        }
        
        // Quest data
        if (QuestJournal.Instance != null)
        {
            data.questData = new QuestSaveData
            {
                activeQuests = new List<QuestSaveInfo>(),
                completedQuests = new List<string>()
            };
            
            // Active quests
            foreach (var quest in QuestJournal.Instance.GetActiveQuests())
            {
                // Get the original quest from QuestManager to get full details
                var questManager = QuestManager.Instance;
                string objectName = "";
                string targetName = "";
                
                // Try to extract object/target names based on quest type
                // This is a simplified approach - in production you'd store these in JournalQuest
                if (quest.questType == QuestType.FETCH || quest.questType == QuestType.INTERACT)
                {
                    // Extract from description like "Trouvez 3 cristal_energie dans laboratory"
                    var match = System.Text.RegularExpressions.Regex.Match(quest.description, @"\d+\s+(\w+)");
                    if (match.Success) objectName = match.Groups[1].Value;
                }
                else if (quest.questType == QuestType.TALK || quest.questType == QuestType.DELIVERY)
                {
                    // Extract from description like "Parlez à scientist dans laboratory"
                    var match = System.Text.RegularExpressions.Regex.Match(quest.description, @"à\s+(\w+)");
                    if (match.Success) targetName = match.Groups[1].Value;
                }
                
                data.questData.activeQuests.Add(new QuestSaveInfo
                {
                    questId = quest.questId,
                    questTitle = quest.questTitle,
                    description = quest.description,
                    questType = quest.questType,
                    currentProgress = quest.currentProgress,
                    maxProgress = quest.maxProgress,
                    giverNPCName = quest.giverNPCName,
                    isTracked = QuestJournal.Instance.IsQuestTracked(quest.questId),
                    objectName = objectName,
                    zoneName = quest.zoneName,
                    targetName = targetName
                });
            }
            
            // Completed quests
            foreach (var quest in QuestJournal.Instance.GetCompletedQuests())
            {
                data.questData.completedQuests.Add(quest.questId);
            }
        }
        
        // NPC data
        data.npcData = new List<NPCSaveData>();
        NPC[] allNPCs = FindObjectsOfType<NPC>();
        foreach (NPC npc in allNPCs)
        {
            data.npcData.Add(new NPCSaveData
            {
                npcName = npc.npcName,
                position = npc.transform.position,
                rotation = npc.transform.rotation.eulerAngles,
                isActive = npc.gameObject.activeSelf
            });
        }
        
        // Inventory data
        if (PlayerInventory.Instance != null)
        {
            data.inventoryData = new InventorySaveData
            {
                items = new List<ItemSaveInfo>()
            };
            
            foreach (var item in PlayerInventory.Instance.GetAllItems())
            {
                data.inventoryData.items.Add(new ItemSaveInfo
                {
                    itemName = item.itemName,
                    quantity = item.quantity,
                    questId = item.questId
                });
            }
        }
        
        // Game settings
        data.gameSettings = new GameSettingsSaveData
        {
            masterVolume = AudioListener.volume,
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f),
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f)
        };
        
        return data;
    }
    
    /// <summary>
    /// Apply loaded save data to the game
    /// </summary>
    void ApplySaveData(SaveData data)
    {
        // Player position
        PlayerControllerCC player = FindObjectOfType<PlayerControllerCC>();
        if (player != null && data.playerData != null)
        {
            player.transform.position = data.playerData.position;
            player.transform.rotation = Quaternion.Euler(data.playerData.rotation);
            player.currentStamina = data.playerData.currentStamina;
        }
        
        // Companion
        if (data.companionData != null && data.companionData.hasCompanion)
        {
            // Find or spawn companion
            CompanionController companion = FindObjectOfType<CompanionController>();
            if (companion != null)
            {
                companion.transform.position = data.companionData.position;
                companion.transform.rotation = Quaternion.Euler(data.companionData.rotation);
                companion.gameObject.SetActive(true);
            }
        }
        
        // Quests
        if (data.questData != null && QuestJournal.Instance != null)
        {
            // Clear current quests
            QuestJournal.Instance.ClearAllQuests();
            
            // Restore active quests
            foreach (var questInfo in data.questData.activeQuests)
            {
                // Recreate quest token
                QuestToken token = new QuestToken(questInfo.questType, questInfo.questId);
                token.description = questInfo.description;
                
                // Restore quest details from saved info
                token.objectName = questInfo.objectName;
                token.zoneName = questInfo.zoneName;
                token.targetName = questInfo.targetName;
                token.quantity = questInfo.maxProgress;
                
                // Add quest directly to journal list since AddQuest might create a new ID
                JournalQuest journalQuest = new JournalQuest(token, questInfo.giverNPCName);
                journalQuest.currentProgress = questInfo.currentProgress;
                journalQuest.maxProgress = questInfo.maxProgress;
                journalQuest.status = questInfo.currentProgress >= questInfo.maxProgress ? 
                    QuestStatus.Completed : QuestStatus.InProgress;
                
                QuestJournal.Instance.allQuests.Add(journalQuest);
                
                // Set tracking
                if (questInfo.isTracked)
                {
                    QuestJournal.Instance.SetTrackedQuest(questInfo.questId);
                }
            }
        }
        
        // NPCs
        if (data.npcData != null)
        {
            NPC[] allNPCs = FindObjectsOfType<NPC>();
            foreach (var npcData in data.npcData)
            {
                NPC npc = System.Array.Find(allNPCs, n => n.npcName == npcData.npcName);
                if (npc != null)
                {
                    npc.transform.position = npcData.position;
                    npc.transform.rotation = Quaternion.Euler(npcData.rotation);
                    npc.gameObject.SetActive(npcData.isActive);
                }
            }
        }
        
        // Inventory
        if (data.inventoryData != null && PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.ClearInventory();
            
            foreach (var item in data.inventoryData.items)
            {
                PlayerInventory.Instance.AddItem(item.itemName, item.quantity, item.questId);
            }
        }
        
        // Game settings
        if (data.gameSettings != null)
        {
            AudioListener.volume = data.gameSettings.masterVolume;
            PlayerPrefs.SetFloat("MusicVolume", data.gameSettings.musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", data.gameSettings.sfxVolume);
        }
    }
    
    /// <summary>
    /// Check if a save file exists
    /// </summary>
    public bool SaveExists(string saveName = null)
    {
        if (string.IsNullOrEmpty(saveName))
            saveName = saveFileName;
        
        string filePath = Path.Combine(savePath, saveName + ".json");
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// Delete a save file
    /// </summary>
    public void DeleteSave(string saveName = null)
    {
        if (string.IsNullOrEmpty(saveName))
            saveName = saveFileName;
        
        string filePath = Path.Combine(savePath, saveName + ".json");
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"[SaveGame] Deleted save: {filePath}");
        }
    }
    
    /// <summary>
    /// Get list of all save files
    /// </summary>
    public string[] GetAllSaves()
    {
        string[] files = Directory.GetFiles(savePath, "*.json");
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        }
        return files;
    }
}

// Save data structures
[System.Serializable]
public class SaveData
{
    public string saveTime;
    public float playTime;
    public PlayerSaveData playerData;
    public CompanionSaveData companionData;
    public QuestSaveData questData;
    public List<NPCSaveData> npcData;
    public InventorySaveData inventoryData;
    public GameSettingsSaveData gameSettings;
}

[System.Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public Vector3 rotation;
    public float currentStamina;
    public float health;
}

[System.Serializable]
public class CompanionSaveData
{
    public bool hasCompanion;
    public string companionType;
    public Vector3 position;
    public Vector3 rotation;
}

[System.Serializable]
public class QuestSaveData
{
    public List<QuestSaveInfo> activeQuests;
    public List<string> completedQuests;
}

[System.Serializable]
public class QuestSaveInfo
{
    public string questId;
    public string questTitle;
    public string description;
    public QuestType questType;
    public int currentProgress;
    public int maxProgress;
    public string giverNPCName;
    public bool isTracked;
    // Additional fields for quest reconstruction
    public string objectName;
    public string zoneName;
    public string targetName;
}

[System.Serializable]
public class NPCSaveData
{
    public string npcName;
    public Vector3 position;
    public Vector3 rotation;
    public bool isActive;
}

[System.Serializable]
public class InventorySaveData
{
    public List<ItemSaveInfo> items;
}

[System.Serializable]
public class ItemSaveInfo
{
    public string itemName;
    public int quantity;
    public string questId;
}

[System.Serializable]
public class GameSettingsSaveData
{
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;
}