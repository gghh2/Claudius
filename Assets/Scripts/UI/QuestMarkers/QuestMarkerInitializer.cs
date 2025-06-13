using UnityEngine;

/// <summary>
/// Auto-initializes the Quest Marker System
/// </summary>
public class QuestMarkerInitializer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Prefab to instantiate for the quest marker system")]
    public static GameObject questMarkerPrefab;
    
    [Tooltip("Configuration to use for the quest markers")]
    public static QuestMarkerConfig defaultConfig;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        // Check if QuestMarkerSystem already exists
        if (QuestMarkerSystem.Instance == null)
        {
            GameObject markerSystem = null;
            
            // Try to load prefab from Resources
            if (questMarkerPrefab == null)
            {
                questMarkerPrefab = Resources.Load<GameObject>("QuestMarkerSystemPrefab");
            }
            
            // If prefab exists, instantiate it
            if (questMarkerPrefab != null)
            {
                markerSystem = Instantiate(questMarkerPrefab);
                markerSystem.name = "QuestMarkerSystem";
            }
            else
            {
                // Create from scratch
                markerSystem = new GameObject("QuestMarkerSystem");
                QuestMarkerSystem system = markerSystem.AddComponent<QuestMarkerSystem>();
                
                // Try to load and assign config
                if (defaultConfig == null)
                {
                    defaultConfig = Resources.Load<QuestMarkerConfig>("QuestMarkerConfig");
                }
                
                if (defaultConfig != null)
                {
                    system.config = defaultConfig;
                }
            }
            
            // Make it persist across scenes
            DontDestroyOnLoad(markerSystem);
        }
    }
}
