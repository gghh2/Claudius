using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles game startup and loading saves from MainMenu
/// MUST run before other systems initialize
/// </summary>
[DefaultExecutionOrder(-500)] // Execute very early
public class GameSceneStartupManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float loadDelay = 0.1f;
    [SerializeField] private bool debugMode = true;
    
    private static bool isLoadingFromMenu = false;
    private static string pendingSave = "";
    
    // Called before scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoad()
    {
        // Check if we're loading from menu
        string saveToLoad = PlayerPrefs.GetString("LoadOnStart", "");
        if (!string.IsNullOrEmpty(saveToLoad))
        {
            isLoadingFromMenu = true;
            pendingSave = saveToLoad;
            Debug.Log($"[GameStartup] BeforeSceneLoad - Will load save: {saveToLoad}");
            
            // Clear the flag immediately
            PlayerPrefs.DeleteKey("LoadOnStart");
            PlayerPrefs.Save();
        }
    }
    
    void Awake()
    {
        if (debugMode)
            Debug.Log($"[GameStartup] Awake - isLoadingFromMenu: {isLoadingFromMenu}, pendingSave: {pendingSave}");
            
        if (isLoadingFromMenu && !string.IsNullOrEmpty(pendingSave))
        {
            // Disable all player systems immediately
            DisablePlayerSystems();
            
            // Start loading process
            StartCoroutine(LoadSaveProcess(pendingSave));
            
            // Reset flags
            isLoadingFromMenu = false;
            pendingSave = "";
        }
    }
    
    void DisablePlayerSystems()
    {
        // Disable player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (debugMode)
                Debug.Log("[GameStartup] Disabling player systems");
                
            // Disable controller
            PlayerControllerCC controller = player.GetComponent<PlayerControllerCC>();
            if (controller != null)
            {
                controller.enabled = false;
                // Store initial position to prevent it from being saved
                player.transform.position = Vector3.zero;
            }
            
            // Disable character controller to prevent physics
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;
                
            // Disable camera follow to prevent it from updating
            CameraFollow camFollow = FindObjectOfType<CameraFollow>();
            if (camFollow != null)
                camFollow.enabled = false;
        }
    }
    
    IEnumerator LoadSaveProcess(string saveName)
    {
        if (debugMode)
            Debug.Log($"[GameStartup] Starting load process for: {saveName}");
            
        // Wait one frame for all objects to be created
        yield return null;
        
        // Wait for SaveGameManager
        float waitTime = 0f;
        while (SaveGameManager.Instance == null && waitTime < 2f)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
        
        if (SaveGameManager.Instance == null)
        {
            Debug.LogError("[GameStartup] SaveGameManager not found after 2 seconds!");
            EnablePlayerSystems();
            yield break;
        }
        
        // Load the save
        if (SaveGameManager.Instance.SaveExists(saveName))
        {
            if (debugMode)
                Debug.Log($"[GameStartup] Loading save NOW: {saveName}");
                
            // Load the game - this should position the player correctly
            SaveGameManager.Instance.LoadGame(saveName);
            
            // Wait a frame for the load to complete
            yield return null;
            
            // Re-enable systems
            EnablePlayerSystems();
            
            // Show notification
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowSuccess("Game loaded!");
            }
        }
        else
        {
            Debug.LogError($"[GameStartup] Save not found: {saveName}");
            EnablePlayerSystems();
        }
    }
    
    void EnablePlayerSystems()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (debugMode)
                Debug.Log("[GameStartup] Re-enabling player systems");
                
            // Enable character controller first
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = true;
                
            // Enable player controller
            PlayerControllerCC controller = player.GetComponent<PlayerControllerCC>();
            if (controller != null)
                controller.enabled = true;
                
            // Enable camera follow
            CameraFollow camFollow = FindObjectOfType<CameraFollow>();
            if (camFollow != null)
                camFollow.enabled = true;
        }
    }
    
    // Public static method to check if we're loading from menu
    public static bool IsLoadingFromMenu()
    {
        return isLoadingFromMenu || !string.IsNullOrEmpty(PlayerPrefs.GetString("LoadOnStart", ""));
    }
}
