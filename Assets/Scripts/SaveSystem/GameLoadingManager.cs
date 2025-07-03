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
        // Check if we're loading a save
        string saveToLoad = PlayerPrefs.GetString("LoadOnStart", "");
        
        if (!string.IsNullOrEmpty(saveToLoad))
        {
            StartCoroutine(LoadGameWithSave(saveToLoad));
        }
    }
    
    IEnumerator LoadGameWithSave(string saveName)
    {
        // Disable player immediately
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
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
        
        // Clear the flag
        PlayerPrefs.DeleteKey("LoadOnStart");
        PlayerPrefs.Save();
        
        // Wait a bit for all systems to initialize
        yield return new WaitForSeconds(initDelay);
        
        // Ensure SaveGameManager is ready
        while (SaveGameManager.Instance == null)
        {
            yield return null;
        }
        
        // Load the save
        if (SaveGameManager.Instance.SaveExists(saveName))
        {
            SaveGameManager.Instance.LoadGame(saveName);
            
            // Wait one more frame
            yield return null;
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
    }
}
