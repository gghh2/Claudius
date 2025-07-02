using UnityEngine;

/// <summary>
/// Game startup manager - Ensures the game starts with the main menu
/// </summary>
public class GameStartupManager : MonoBehaviour
{
    [Header("Startup Settings")]
    [SerializeField] private bool startWithMainMenu = true;
    [SerializeField] private GameObject mainMenuPrefab;
    
    void Awake()
    {
        if (startWithMainMenu)
        {
            // Ensure main menu exists
            MainMenuUI mainMenu = FindObjectOfType<MainMenuUI>();
            
            if (mainMenu == null && mainMenuPrefab != null)
            {
                // Instantiate main menu if it doesn't exist
                Instantiate(mainMenuPrefab);
            }
            else if (mainMenu == null)
            {
                Debug.LogError("[GameStartupManager] No MainMenuUI found and no prefab assigned!");
            }
        }
    }
}
