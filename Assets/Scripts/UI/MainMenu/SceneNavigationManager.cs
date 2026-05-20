using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Simple scene navigation manager
/// Handles transitions between scenes
/// </summary>
public class SceneNavigationManager : MonoBehaviour
{
    public static SceneNavigationManager Instance { get; private set; }
    
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Game";
    
    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreenPrefab;
    private GameObject currentLoadingScreen;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // DontDestroyOnLoad only works on root GameObjects: detach if nested
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Load the main menu scene
    /// </summary>
    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneAsync(mainMenuSceneName));
    }
    
    /// <summary>
    /// Load the game scene
    /// </summary>
    public void LoadGame(bool newGame = false)
    {
        if (newGame)
        {
            PlayerPrefs.DeleteKey("LoadOnStart");
        }
        
        StartCoroutine(LoadSceneAsync(gameSceneName));
    }
    
    /// <summary>
    /// Load game with specific save
    /// </summary>
    public void LoadGameWithSave(string saveName)
    {
        PlayerPrefs.SetString("LoadOnStart", saveName);
        PlayerPrefs.Save();
        
        StartCoroutine(LoadSceneAsync(gameSceneName));
    }
    
    /// <summary>
    /// Async scene loading with loading screen
    /// </summary>
    IEnumerator LoadSceneAsync(string sceneName)
    {
        // Show loading screen
        ShowLoadingScreen();
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Start loading
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Wait for scene to load
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // Hide loading screen after a short delay
        yield return new WaitForSeconds(0.5f);
        HideLoadingScreen();
    }
    
    void ShowLoadingScreen()
    {
        if (loadingScreenPrefab != null && currentLoadingScreen == null)
        {
            currentLoadingScreen = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(currentLoadingScreen);
        }
    }
    
    void HideLoadingScreen()
    {
        if (currentLoadingScreen != null)
        {
            Destroy(currentLoadingScreen);
            currentLoadingScreen = null;
        }
    }
    
    /// <summary>
    /// Quick access to return to main menu
    /// </summary>
    public static void ReturnToMainMenu()
    {
        if (Instance != null)
        {
            Instance.LoadMainMenu();
        }
        else
        {
            // Fallback if no instance
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
