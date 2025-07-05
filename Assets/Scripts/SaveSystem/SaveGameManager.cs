using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using System.Linq;

/// <summary>
/// Main save game system - handles saving and loading game state
/// </summary>
public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance { get; private set; }
    
    [Header("Save Settings")]
    [Tooltip("Name of the save file")]
    public string saveFileName = "savegame";
    
    // Auto save removed - no longer needed
    // [Tooltip("Auto save interval in seconds (0 = disabled)")]
    // public float autoSaveInterval = 60f;
    
    [Header("Debug")]
    public bool debugMode = true;
    
    // Events
    public static event Action OnGameSaved;
    public static event Action OnGameLoaded;
    
    // private float autoSaveTimer; // Removed
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
    
    // Auto save removed
    /*
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
    */
    
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
            
            Debug.Log($"[SaveGame] Saving player position: {data.playerData.position}");
            Debug.Log($"[SaveGame] Saving player rotation: {data.playerData.rotation}");
            
            // Save camera zoom
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.orthographic)
            {
                data.playerData.cameraZoom = mainCamera.orthographicSize;
            }
            else if (mainCamera != null)
            {
                // For perspective camera, you might want to save field of view
                data.playerData.cameraZoom = mainCamera.fieldOfView;
            }
        }
        else
        {
            Debug.LogError("[SaveGame] PlayerControllerCC not found!");
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
            
            Debug.Log($"[SaveGame] Saving quests - Journal has {QuestJournal.Instance.allQuests.Count} total quests");
            
            // Active quests
            foreach (var quest in QuestJournal.Instance.GetActiveQuests())
            {
                Debug.Log($"[SaveGame] Processing active quest: {quest.questTitle} (ID: {quest.questId})");
                
                // Get the original quest from QuestManager to get full details
                var questManager = QuestManager.Instance;
                string objectName = "";
                string targetName = "";
                
                // Get quest details from QuestManager if available
                ActiveQuest activeQuest = null;
                string exactZoneName = quest.zoneName; // Start with journal zone name
                
                if (questManager != null)
                {
                    activeQuest = questManager.GetActiveQuestPublic(quest.questId);
                    if (activeQuest != null && activeQuest.questData != null)
                    {
                        // Get details directly from the quest token
                        objectName = activeQuest.questData.objectName ?? "";
                        targetName = activeQuest.questData.targetName ?? "";
                        
                        // IMPORTANT: Get the exact zone name from the quest token
                        if (!string.IsNullOrEmpty(activeQuest.questData.zoneName))
                        {
                            exactZoneName = activeQuest.questData.zoneName;
                        }
                        
                        Debug.Log($"[SaveGame] Got quest details from QuestManager - objectName: {objectName}, targetName: {targetName}, zoneName: {exactZoneName}");
                        
                        // Also try to get the actual zone from the quest
                        var targetZone = activeQuest.GetTargetZone();
                        if (targetZone != null)
                        {
                            exactZoneName = targetZone.zoneName;
                            Debug.Log($"[SaveGame] Got exact zone name from target zone: {exactZoneName}");
                        }
                    }
                }
                
                // Fallback: Try to extract object/target names from description if not found
                if (string.IsNullOrEmpty(objectName) && string.IsNullOrEmpty(targetName))
                {
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
                    Debug.Log($"[SaveGame] Extracted from description - objectName: {objectName}, targetName: {targetName}");
                }
                
                // Get spawned objects from QuestManager
                var spawnedObjects = new List<QuestObjectSaveInfo>();
                if (QuestManager.Instance != null && activeQuest != null)
                {
                    if (activeQuest.spawnedObjects != null)
                    {
                        Debug.Log($"[SaveGame] Quest has {activeQuest.spawnedObjects.Count} spawned objects");
                        foreach (var obj in activeQuest.spawnedObjects)
                        {
                            if (obj != null)
                            {
                                string objType = "item";
                                if (obj.GetComponent<NPC>() != null) objType = "npc";
                                else if (obj.GetComponent<QuestObject>() != null) objType = "quest_object";
                                else if (obj.name.Contains("terminal") || obj.name.Contains("Terminal")) objType = "terminal";
                                
                                spawnedObjects.Add(new QuestObjectSaveInfo
                                {
                                    objectName = obj.name,
                                    position = obj.transform.position,
                                    rotation = obj.transform.rotation.eulerAngles,
                                    isActive = obj.activeSelf,
                                    objectType = objType
                                });
                                
                                Debug.Log($"[SaveGame] Saved object: {obj.name} at {obj.transform.position}");
                            }
                        }
                    }
                }
                else if (activeQuest == null)
                {
                    Debug.LogWarning($"[SaveGame] No active quest found in QuestManager for ID: {quest.questId}");
                }
                
                var questSaveInfo = new QuestSaveInfo
                {
                    questId = quest.questId,
                    questTitle = quest.questTitle,
                    description = quest.description,
                    questType = quest.questType,
                    status = quest.status,
                    currentProgress = quest.currentProgress,
                    maxProgress = quest.maxProgress,
                    giverNPCName = quest.giverNPCName,
                    isTracked = QuestJournal.Instance.IsQuestTracked(quest.questId),
                    objectName = objectName,
                    zoneName = exactZoneName,  // Use the exact zone name
                    targetName = targetName,
                    spawnedObjects = spawnedObjects
                };
                
                data.questData.activeQuests.Add(questSaveInfo);
                Debug.Log($"[SaveGame] Added quest to save data: {quest.questTitle} with {spawnedObjects.Count} objects");
            }
            
            // Completed quests - save full info instead of just IDs
            foreach (var quest in QuestJournal.Instance.GetCompletedQuests())
            {
                // Save completed quest as a full QuestSaveInfo for potential restoration
                var completedQuestInfo = new QuestSaveInfo
                {
                    questId = quest.questId,
                    questTitle = quest.questTitle,
                    description = quest.description,
                    questType = quest.questType,
                    status = quest.status,
                    currentProgress = quest.currentProgress,
                    maxProgress = quest.maxProgress,
                    giverNPCName = quest.giverNPCName,
                    isTracked = false,
                    objectName = "",
                    zoneName = quest.zoneName,
                    targetName = "",
                    spawnedObjects = new List<QuestObjectSaveInfo>()
                };
                
                // Add to activeQuests list but with Completed status
                data.questData.activeQuests.Add(completedQuestInfo);
                data.questData.completedQuests.Add(quest.questId);
                Debug.Log($"[SaveGame] Added completed quest: {quest.questTitle}");
            }
            // Cancelled quests - save full info
            foreach (var quest in QuestJournal.Instance.GetCancelledQuests())
            {
                // Save cancelled quest as a full QuestSaveInfo
                var cancelledQuestInfo = new QuestSaveInfo
                {
                    questId = quest.questId,
                    questTitle = quest.questTitle,
                    description = quest.description,
                    questType = quest.questType,
                    status = quest.status,
                    currentProgress = quest.currentProgress,
                    maxProgress = quest.maxProgress,
                    giverNPCName = quest.giverNPCName,
                    isTracked = false,
                    objectName = "",
                    zoneName = quest.zoneName,
                    targetName = "",
                    spawnedObjects = new List<QuestObjectSaveInfo>()
                };
                
                // Add to activeQuests list but with Cancelled status
                data.questData.activeQuests.Add(cancelledQuestInfo);
                Debug.Log($"[SaveGame] Added cancelled quest: {quest.questTitle}");
            }
            
            Debug.Log($"[SaveGame] Total saved: {data.questData.activeQuests.Count} quests ({QuestJournal.Instance.GetActiveQuests().Count} active, {QuestJournal.Instance.GetCompletedQuests().Count} completed, {QuestJournal.Instance.GetCancelledQuests().Count} cancelled)");
        }
        else
        {
            Debug.LogError("[SaveGame] QuestJournal.Instance is NULL during save!");
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
            Debug.Log($"[SaveGame] Loading player position: {data.playerData.position}");
            Debug.Log($"[SaveGame] Loading player rotation: {data.playerData.rotation}");
            
            // Apply position
            player.transform.position = data.playerData.position;
            player.transform.rotation = Quaternion.Euler(data.playerData.rotation);
            player.currentStamina = data.playerData.currentStamina;
            
            // Force CharacterController to update its position
            CharacterController charController = player.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
                player.transform.position = data.playerData.position;
                charController.enabled = true;
                Debug.Log($"[SaveGame] CharacterController updated, final position: {player.transform.position}");
            }
            
            // Update spawn position for respawn
            PauseMenuUI.UpdateSpawnPosition(data.playerData.position, data.playerData.rotation);
            
            // Restore camera zoom
            Camera mainCamera = Camera.main;
            if (mainCamera != null && data.playerData.cameraZoom > 0)
            {
                if (mainCamera.orthographic)
                {
                    mainCamera.orthographicSize = data.playerData.cameraZoom;
                }
                else
                {
                    mainCamera.fieldOfView = data.playerData.cameraZoom;
                }
                
                // Also update CameraFollow if it exists
                CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.SetZoom(data.playerData.cameraZoom);
                }
            }
        }
        else
        {
            if (player == null)
                Debug.LogError("[SaveGame] PlayerControllerCC not found during load!");
            if (data.playerData == null)
                Debug.LogError("[SaveGame] No player data in save file!");
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
        if (data.questData != null)
        {
            Debug.Log($"[SaveGame] Loading quest data: {data.questData.activeQuests.Count} active quests, {data.questData.completedQuests.Count} completed quests");
            
            if (QuestJournal.Instance != null)
            {
                // Clear current quests in both systems
                QuestJournal.Instance.ClearAllQuests();
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.ClearAllQuests();
                }
                else
                {
                    Debug.LogError("[SaveGame] QuestManager.Instance is NULL during load!");
                }
                
                // Restore all quests to the journal first
                foreach (var questInfo in data.questData.activeQuests)
                {
                    // Handle completed and cancelled quests
                    if (questInfo.status == QuestStatus.Completed || questInfo.status == QuestStatus.Cancelled)
                    {
                        QuestToken token = new QuestToken(questInfo.questType, questInfo.questId);
                        token.description = questInfo.description;
                        
                        JournalQuest restoredQuest = new JournalQuest(token, questInfo.giverNPCName);
                        restoredQuest.status = questInfo.status;
                        restoredQuest.currentProgress = questInfo.currentProgress;
                        restoredQuest.maxProgress = questInfo.maxProgress;
                        
                        QuestJournal.Instance.allQuests.Add(restoredQuest);
                        Debug.Log($"[SaveGame] Restored {questInfo.status} quest to journal: {restoredQuest.questTitle}");
                    }
                }
                
                // Filter out only the truly active (InProgress) quests for recreation
                var activeQuestsToRecreate = data.questData.activeQuests
                    .Where(q => q.status == QuestStatus.InProgress)
                    .ToList();
                
                Debug.Log($"[SaveGame] Found {activeQuestsToRecreate.Count} in-progress quests to recreate in world");
                Debug.Log($"[SaveGame] Journal restored with {QuestJournal.Instance.allQuests.Count} total quests");
                
                // Then recreate active quests in the world (this will handle both QuestManager and QuestJournal)
                Debug.Log("[SaveGame] Starting coroutine to recreate active quest objects...");
                StartCoroutine(RecreateQuestObjectsAfterLoad(activeQuestsToRecreate));
            }
            else
            {
                Debug.LogError("[SaveGame] QuestJournal.Instance is NULL during load!");
            }
        }
        else
        {
            Debug.LogWarning("[SaveGame] No quest data in save file");
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
    
    /// <summary>
    /// Coroutine to refresh markers after everything is loaded
    /// </summary>
    IEnumerator RefreshMarkersAfterLoad()
    {
        // Wait for end of frame to ensure everything is initialized
        yield return new WaitForEndOfFrame();
        
        // Force a journal refresh first
        if (QuestJournal.Instance != null)
        {
            var trackedQuest = QuestJournal.Instance.GetTrackedQuest();
            if (trackedQuest != null)
            {
                Debug.Log($"[SaveGame] Tracked quest after load: {trackedQuest.questTitle}");
            }
        }
        
        // Then refresh the markers
        if (QuestMarkerSystem.Instance != null)
        {
            QuestMarkerSystem.Instance.RefreshMarkers();
            Debug.Log("[SaveGame] Quest markers refreshed after load");
        }
    }
    
    /// <summary>
    /// Coroutine to recreate quest objects after load
    /// </summary>
    IEnumerator RecreateQuestObjectsAfterLoad(List<QuestSaveInfo> questInfos)
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        
        if (QuestManager.Instance == null)
        {
            Debug.LogError("[SaveGame] QuestManager.Instance is null, cannot recreate quest objects");
            yield break;
        }
        
        // Clear any existing quests first
        QuestManager.Instance.ClearAllQuests();
        
        foreach (var questInfo in questInfos)
        {
            if (questInfo.status == QuestStatus.InProgress)
            {
                Debug.Log($"[SaveGame] Recreating quest: {questInfo.questTitle} (ID: {questInfo.questId})");
                Debug.Log($"[SaveGame] Quest details - Type: {questInfo.questType}, Zone: {questInfo.zoneName}, Object: {questInfo.objectName}, Target: {questInfo.targetName}");
                
                // Create the quest in QuestManager
                QuestToken token = new QuestToken(questInfo.questType, questInfo.questId);
                token.description = questInfo.description;
                token.objectName = questInfo.objectName;
                token.zoneName = questInfo.zoneName;
                token.targetName = questInfo.targetName;
                token.quantity = questInfo.maxProgress;
                
                // Try to determine the zone type from the zone name
                if (!string.IsNullOrEmpty(questInfo.zoneName))
                {
                    // First try to find the actual zone in the world
                    QuestZone[] allZones = FindObjectsOfType<QuestZone>();
                    foreach (var zone in allZones)
                    {
                        if (zone.zoneName == questInfo.zoneName)
                        {
                            token.zoneType = zone.zoneType;
                            Debug.Log($"[SaveGame] Found matching zone: {zone.zoneName} of type {zone.zoneType}");
                            break;
                        }
                    }
                    
                    // If we didn't find the exact zone, try to parse the zone type
                    if (!token.zoneType.HasValue)
                    {
                        token.zoneType = QuestTokenDetector.Instance?.ParseZoneType(questInfo.zoneName);
                        Debug.Log($"[SaveGame] Parsed zone type from name: {token.zoneType}");
                    }
                }
                
                // Remove the quest from journal temporarily to avoid duplication
                var journalQuest = QuestJournal.Instance.allQuests.FirstOrDefault(q => q.questId == questInfo.questId);
                if (journalQuest != null)
                {
                    QuestJournal.Instance.allQuests.Remove(journalQuest);
                }
                
                // This will create the quest and spawn default objects
                bool created = QuestManager.Instance.CreateQuestFromToken(token, questInfo.giverNPCName);
                
                if (!created)
                {
                    Debug.LogError($"[SaveGame] Failed to recreate quest: {questInfo.questTitle}");
                    Debug.LogError($"[SaveGame] Make sure zone '{questInfo.zoneName}' supports object type required for {questInfo.questType} quests");
                    
                    // Re-add to journal if creation failed
                    if (journalQuest != null)
                    {
                        QuestJournal.Instance.allQuests.Add(journalQuest);
                    }
                    continue;
                }
                
                // Wait a frame for objects to be created
                yield return null;
                
                // Update the quest progress in QuestManager
                var activeQuest = QuestManager.Instance.GetActiveQuestPublic(questInfo.questId);
                if (activeQuest != null)
                {
                    activeQuest.currentProgress = questInfo.currentProgress;
                    Debug.Log($"[SaveGame] Updated quest progress: {questInfo.currentProgress}/{questInfo.maxProgress}");
                    
                    // Update progress in journal too
                    var newJournalQuest = QuestJournal.Instance.allQuests.FirstOrDefault(q => q.questId == questInfo.questId);
                    if (newJournalQuest != null)
                    {
                        newJournalQuest.currentProgress = questInfo.currentProgress;
                        newJournalQuest.status = questInfo.status;
                    }
                    
                    // If we have saved object positions, restore them
                    if (questInfo.spawnedObjects != null && questInfo.spawnedObjects.Count > 0)
                    {
                        Debug.Log($"[SaveGame] Quest {questInfo.questTitle} had {questInfo.spawnedObjects.Count} spawned objects saved");
                        
                        // Try to restore positions of spawned objects
                        for (int i = 0; i < activeQuest.spawnedObjects.Count && i < questInfo.spawnedObjects.Count; i++)
                        {
                            var spawnedObj = activeQuest.spawnedObjects[i];
                            var savedObj = questInfo.spawnedObjects[i];
                            
                            if (spawnedObj != null)
                            {
                                spawnedObj.transform.position = savedObj.position;
                                spawnedObj.transform.rotation = Quaternion.Euler(savedObj.rotation);
                                spawnedObj.SetActive(savedObj.isActive);
                                Debug.Log($"[SaveGame] Restored position for {savedObj.objectName} at {savedObj.position}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[SaveGame] Could not find active quest after creation: {questInfo.questId}");
                }
            }
        }
        
        // Restore tracked quest
        var trackedQuest = questInfos.FirstOrDefault(q => q.isTracked);
        if (trackedQuest != null)
        {
            QuestJournal.Instance.SetTrackedQuest(trackedQuest.questId);
            Debug.Log($"[SaveGame] Restored tracked quest: {trackedQuest.questTitle}");
        }
        
        // Final refresh of markers
        yield return null;
        if (QuestMarkerSystem.Instance != null)
        {
            QuestMarkerSystem.Instance.RefreshMarkers();
            Debug.Log("[SaveGame] Final marker refresh after recreating quest objects");
        }
    }
    
    /// <summary>
    /// Get the save date/time for a specific save
    /// </summary>
    public System.DateTime GetSaveDateTime(string saveName)
    {
        string fullPath = Path.Combine(savePath, saveName + ".json");
        
        if (File.Exists(fullPath))
        {
            try
            {
                // Try to read the save time from the file
                string jsonData = File.ReadAllText(fullPath);
                SaveData data = JsonUtility.FromJson<SaveData>(jsonData);
                
                if (!string.IsNullOrEmpty(data.saveTime))
                {
                    if (System.DateTime.TryParse(data.saveTime, out System.DateTime saveTime))
                    {
                        return saveTime;
                    }
                }
                
                // Fallback to file modification time
                return File.GetLastWriteTime(fullPath);
            }
            catch
            {
                // If we can't read the file, use file modification time
                return File.GetLastWriteTime(fullPath);
            }
        }
        
        return System.DateTime.MinValue;
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
    public float cameraZoom; // Added camera zoom/size
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
    public QuestStatus status;
    public int currentProgress;
    public int maxProgress;
    public string giverNPCName;
    public bool isTracked;
    // Additional fields for quest reconstruction
    public string objectName;
    public string zoneName;
    public string targetName;
    // Quest objects in world
    public List<QuestObjectSaveInfo> spawnedObjects;
}

[System.Serializable]
public class QuestObjectSaveInfo
{
    public string objectName;
    public Vector3 position;
    public Vector3 rotation;
    public bool isActive;
    public string objectType; // "item", "npc", "marker", "terminal"
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