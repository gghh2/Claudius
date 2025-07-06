using UnityEngine;
using System.Collections;

/// <summary>
/// Simple startup manager that delays player initialization when loading from menu
/// </summary>
public class GameLoadingManager : MonoBehaviour
{
    [SerializeField] private float initDelay = 0.2f;
    
    void Awake()
    {
        Debug.Log("[GameLoadingManager] Awake called");
        
        // IMPORTANT: Ensure UI is active first
        EnsureUIIsActive();
        
        // Check if we're loading a save
        string saveToLoad = PlayerPrefs.GetString("LoadOnStart", "");
        Debug.Log($"[GameLoadingManager] LoadOnStart value: '{saveToLoad}'");
        
        if (!string.IsNullOrEmpty(saveToLoad))
        {
            Debug.Log($"[GameLoadingManager] Starting coroutine to load save: {saveToLoad}");
            StartCoroutine(LoadGameWithSave(saveToLoad));
        }
        else
        {
            Debug.Log("[GameLoadingManager] No save to load on start");
            // Hide any loading screen that might be visible
            HideLoadingScreen();
        }
    }
    
    void Start()
    {
        // Ensure game is not paused when starting from MainMenu
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("[GameLoadingManager] Time.timeScale was 0 at Start! Restoring to 1");
            Time.timeScale = 1f;
        }
    }
    
    void EnsureUIIsActive()
    {
        GameObject uiObject = GameObject.Find("UI");
        if (uiObject != null && !uiObject.activeSelf)
        {
            Debug.Log("🔧 GameLoadingManager: Activating UI GameObject");
            uiObject.SetActive(true);
        }
    }
    
    IEnumerator LoadGameWithSave(string saveName)
    {
        Debug.Log($"[GameLoadingManager] LoadGameWithSave coroutine started for: {saveName}");
        
        // Disable player immediately
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("[GameLoadingManager] Disabling player");
            // Force player to origin to prevent saving wrong position
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = Vector3.zero;
                player.transform.rotation = Quaternion.identity;
            }
            
            PlayerControllerCC controller = player.GetComponent<PlayerControllerCC>();
            if (controller != null)
                controller.enabled = false;
        }
        else
        {
            Debug.LogWarning("[GameLoadingManager] Player not found!");
        }
        
        // Clear the flag
        PlayerPrefs.DeleteKey("LoadOnStart");
        PlayerPrefs.Save();
        Debug.Log("[GameLoadingManager] LoadOnStart flag cleared");
        
        // Wait a bit for all systems to initialize
        Debug.Log($"[GameLoadingManager] Waiting {initDelay} seconds...");
        yield return new WaitForSeconds(initDelay);
        
        // Ensure SaveGameManager is ready
        Debug.Log("[GameLoadingManager] Waiting for SaveGameManager...");
        int waitFrames = 0;
        while (SaveGameManager.Instance == null)
        {
            waitFrames++;
            if (waitFrames > 300) // 5 seconds at 60fps
            {
                Debug.LogError("[GameLoadingManager] SaveGameManager not found after 5 seconds!");
                HideLoadingScreen();
                yield break;
            }
            yield return null;
        }
        Debug.Log($"[GameLoadingManager] SaveGameManager found after {waitFrames} frames");
        
        // Load the save
        if (SaveGameManager.Instance.SaveExists(saveName))
        {
            Debug.Log($"[GameLoadingManager] Save exists, loading: {saveName}");
            SaveGameManager.Instance.LoadGame(saveName);
            
            // Wait one more frame
            yield return null;
            Debug.Log("[GameLoadingManager] Save loaded successfully");
        }
        else
        {
            Debug.LogError($"[GameLoadingManager] Save does not exist: {saveName}");
        }
        
        // Re-enable player
        if (player != null)
        {
            Debug.Log("[GameLoadingManager] Re-enabling player");
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = true;
                
            PlayerControllerCC controller = player.GetComponent<PlayerControllerCC>();
            if (controller != null)
                controller.enabled = true;
        }
        
        // Hide loading screen
        Debug.Log("[GameLoadingManager] Hiding loading screen");
        HideLoadingScreen();
    }
    
    void HideLoadingScreen()
    {
        Debug.Log("[GameLoadingManager] HideLoadingScreen called");
        
        // First, ensure Time.timeScale is restored
        Time.timeScale = 1f;
        Debug.Log("[GameLoadingManager] Time.timeScale restored to 1");
        
        // Try to find and hide the loading screen (might not exist in Game scene)
        GameObject loadingScreen = GameObject.Find("LoadingScreen");
        if (loadingScreen != null)
        {
            Debug.Log("[GameLoadingManager] LoadingScreen found and hidden");
            loadingScreen.SetActive(false);
        }
        else
        {
            Debug.Log("[GameLoadingManager] LoadingScreen not found (normal if coming from MainMenu)");
        }
        
        // Ensure player controls are enabled
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerControllerCC controller = player.GetComponent<PlayerControllerCC>();
            if (controller != null)
            {
                controller.enabled = true;
                Debug.Log("[GameLoadingManager] Player controller re-enabled");
            }
        }
        
        // Force cursor to game state
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Debug.Log("[GameLoadingManager] Cursor locked for gameplay");
        
        // Close any open UI panels that might be blocking
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.CloseAllPanels();
            Debug.Log("[GameLoadingManager] All UI panels closed");
        }
    }
}
