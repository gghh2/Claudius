using UnityEngine;

/// <summary>
/// Enum pour identifier les systèmes de debug
/// </summary>
public enum DebugSystem
{
    Player,
    Companion,
    NPC,
    Footstep,
    AI,
    Quest,
    DynamicAssets
}

/// <summary>
/// Configuration globale du debug pour tout le projet
/// À placer sur un GameObject vide dans la scène
/// </summary>
public class GlobalDebugManager : MonoBehaviour
{
    private static GlobalDebugManager instance;
    public static GlobalDebugManager Instance => instance;
    
    [Header("=== Configuration Globale du Debug ===")]
    [Tooltip("Active/Désactive TOUS les debugs du projet")]
    public bool masterDebugEnabled = false;
    
    [Space(10)]
    [Header("Debug par Système")]
    [Tooltip("Active le debug du joueur (F1)")]
    public bool playerDebug = false;
    
    [Tooltip("Active le debug des compagnons (F8)")]
    public bool companionDebug = false;
    
    [Tooltip("Active le debug des NPCs")]
    public bool npcDebug = false;
    
    [Tooltip("Active le debug des footsteps")]
    public bool footstepDebug = false;
    
    [Tooltip("Active le debug de l'IA")]
    public bool aiDebug = false;
    
    [Tooltip("Active le debug des quêtes")]
    public bool questDebug = false;
    
    [Tooltip("Active le debug des assets dynamiques")]
    public bool dynamicAssetsDebug = false;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        ApplyDebugSettings();
    }
    
    void OnValidate()
    {
        // Applique les settings quand on change dans l'Inspector
        if (Application.isPlaying)
        {
            ApplyDebugSettings();
        }
    }
    
    void ApplyDebugSettings()
    {
        // Plus besoin de modifier les scripts individuels
        // car ils vont maintenant lire directement depuis GlobalDebugManager
        
        Debug.Log($"[GlobalDebug] Settings appliqués - Master: {masterDebugEnabled}");
    }
    
    /// <summary>
    /// Vérifie si le debug est activé pour un système spécifique
    /// </summary>
    public static bool IsDebugEnabled(DebugSystem system)
    {
        if (Instance == null || !Instance.masterDebugEnabled)
            return false;
            
        switch (system)
        {
            case DebugSystem.Player:
                return Instance.playerDebug;
            case DebugSystem.Companion:
                return Instance.companionDebug;
            case DebugSystem.NPC:
                return Instance.npcDebug;
            case DebugSystem.Footstep:
                return Instance.footstepDebug;
            case DebugSystem.AI:
                return Instance.aiDebug;
            case DebugSystem.Quest:
                return Instance.questDebug;
            case DebugSystem.DynamicAssets:
                return Instance.dynamicAssetsDebug;
            default:
                return false;
        }
    }
    

    
    [ContextMenu("Enable All Debug")]
    public void EnableAllDebug()
    {
        masterDebugEnabled = true;
        playerDebug = true;
        companionDebug = true;
        npcDebug = true;
        footstepDebug = true;
        aiDebug = true;
        questDebug = true;
        dynamicAssetsDebug = true;
        ApplyDebugSettings();
    }
    
    [ContextMenu("Disable All Debug")]
    public void DisableAllDebug()
    {
        masterDebugEnabled = false;
        ApplyDebugSettings();
    }
    
    void OnGUI()
    {
        if (!masterDebugEnabled) return;
        
        // Affiche un indicateur de debug actif
        GUI.Box(new Rect(Screen.width - 150, 10, 140, 25), "DEBUG MODE ACTIF");
        
        // Raccourcis clavier globaux
        if (Event.current.type == EventType.KeyDown)
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.F12:
                    masterDebugEnabled = !masterDebugEnabled;
                    ApplyDebugSettings();
                    Debug.Log($"[GlobalDebug] Master Debug: {masterDebugEnabled}");
                    break;
            }
        }
    }
}
