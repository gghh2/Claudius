using UnityEngine;
using System.Collections;

/// <summary>
/// Manages game initialization when loading from MainMenu with a saved game.
/// Ensures proper state restoration including Time.timeScale, player controls, and UI.
/// </summary>
public class GameLoadingManager : MonoBehaviour
{
    [SerializeField] private float initDelay = 0.2f;
    
    void Awake()
    {
        // IMPORTANT: Ensure UI is active first
        EnsureUIIsActive();
        
        // Check if we're loading a save from MainMenu
        string saveToLoad = PlayerPrefs.GetString("LoadOnStart", "");
        
        if (!string.IsNullOrEmpty(saveToLoad))
        {
            Debug.Log($"[GameLoadingManager] Loading save on start: {saveToLoad}");
            StartCoroutine(LoadGameWithSave(saveToLoad));
        }
        else
        {
            // No save to load - ensure game is ready to play
            EnsureGameIsPlayable();
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
        
        // Double-check UnifiedUIManager exists
        if (UnifiedUIManager.Instance == null)
        {
            Debug.LogError("[GameLoadingManager] UnifiedUIManager not found in scene! ESCAPE key won't work.");
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
        // Disable player immediately to prevent wrong position save
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        Debug.Log($"[GameLoadingManager] Found player: {(player != null ? player.name : "NULL")} (InstanceID: {(player != null ? player.GetInstanceID().ToString() : "N/A")})" );
        if (player != null)
        {
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
        
        // Clear the flag immediately
        PlayerPrefs.DeleteKey("LoadOnStart");
        PlayerPrefs.Save();
        
        // Wait for all systems to initialize
        yield return new WaitForSeconds(initDelay);
        
        // Ensure SaveGameManager is ready
        int waitFrames = 0;
        while (SaveGameManager.Instance == null)
        {
            waitFrames++;
            if (waitFrames > 300) // 5 seconds at 60fps
            {
                Debug.LogError("[GameLoadingManager] SaveGameManager timeout!");
                HideLoadingScreen();
                yield break;
            }
            yield return null;
        }
        
        // Load the save
        if (SaveGameManager.Instance.SaveExists(saveName))
        {
            SaveGameManager.Instance.LoadGame(saveName);
            yield return null; // Wait one frame for load to complete
            Debug.Log($"[GameLoadingManager] Save loaded: {saveName}");
        }
        else
        {
            Debug.LogError($"[GameLoadingManager] Save not found: {saveName}");
        }
        
        // Re-enable player
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = true;
                
            PlayerControllerCC controller = player.GetComponent<PlayerControllerCC>();
            if (controller != null)
                controller.enabled = true;
        }
        
        // Ensure game is ready to play
        HideLoadingScreen();
    }
    
    void HideLoadingScreen()
    {
        // CRITICAL: Ensure Time.timeScale is restored
        Time.timeScale = 1f;
        
        // Try to find and hide the loading screen (might not exist in Game scene)
        GameObject loadingScreen = GameObject.Find("LoadingScreen");
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
        
        // Ensure player controls are enabled
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerControllerCC controller = player.GetComponent<PlayerControllerCC>();
            if (controller != null)
            {
                controller.enabled = true;
            }
        }
        
        // Le curseur est géré par SmartCursorManager (autorité unique) ;
        // ne pas y toucher ici.

        // Close any open UI panels that might be blocking
        // Only for save loading, not for new games
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.CloseAllPanels();
        }
    }
    
    void EnsureGameIsPlayable()
    {
        // For new game starts, just ensure basic state without closing panels
        Time.timeScale = 1f;
        
        // Hide loading screen if present
        GameObject loadingScreen = GameObject.Find("LoadingScreen");
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
        
        // Le curseur est géré par SmartCursorManager (autorité unique) ;
        // ne pas y toucher ici.

        // DO NOT close panels or mess with UnifiedUIManager for new games
        // Let it initialize naturally
    }
}
