using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gère le curseur en vérifiant les panels UI enfants d'un Canvas
/// </summary>
public class SmartCursorManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool debugMode = false;
    
    [Header("Panels UI à surveiller")]
    [SerializeField] private GameObject[] uiPanels;
    
    [Header("Panels à ignorer (toujours actifs)")]
    [SerializeField] private string[] ignoredPanelNames = {
        "StaminaUI",
        "HistoryPanel",
        "HealthBar",
        "MiniMap",
        "HUD"
    };
    
    [Header("Auto-détection")]
    [SerializeField] private bool autoDetectPanels = true;
    [SerializeField] private string[] panelNamesToDetect = {
        "DialoguePanel",
        "InventoryPanel", 
        "QuestJournalPanel",
        "StaminaUI",
        "PauseMenu"
    };
    
    void Start()
    {
        // Cache le curseur au démarrage
        Cursor.visible = false;
        
        // Auto-détection des panels si activé
        if (autoDetectPanels)
        {
            AutoDetectPanels();
        }
    }
    
    void AutoDetectPanels()
    {
        var panels = new System.Collections.Generic.List<GameObject>();
        
        // Cherche le Canvas principal
        Canvas mainCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (mainCanvas != null)
        {
            // Cherche les panels enfants par nom
            foreach (string panelName in panelNamesToDetect)
            {
                Transform panel = mainCanvas.transform.Find(panelName);
                if (panel != null)
                {
                    panels.Add(panel.gameObject);
                    if (debugMode) Debug.Log($"[SmartCursor] Panel auto-détecté: {panelName}");
                }
            }
        }
        
        // Cherche aussi dans toute la scène
        foreach (string panelName in panelNamesToDetect)
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel != null && !panels.Contains(panel))
            {
                panels.Add(panel);
                if (debugMode) Debug.Log($"[SmartCursor] Panel trouvé dans la scène: {panelName}");
            }
        }
        
        uiPanels = panels.ToArray();
    }
    
    void Update()
    {
        bool shouldShowCursor = false;
        
        // Vérifie chaque panel
        foreach (GameObject panel in uiPanels)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                // Vérifie si ce panel doit être ignoré
                bool shouldIgnore = false;
                foreach (string ignoredName in ignoredPanelNames)
                {
                    if (panel.name.Contains(ignoredName))
                    {
                        shouldIgnore = true;
                        break;
                    }
                }
                
                if (shouldIgnore)
                {
                    if (debugMode) Debug.Log($"[SmartCursor] Panel ignoré: {panel.name}");
                    continue;
                }
                
                // Vérification spéciale pour PauseMenu
                if (panel.name.Contains("PauseMenu") || panel.name.Contains("Pause"))
                {
                    // Vérifie si le menu est vraiment visible
                    CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                    if (canvasGroup != null && canvasGroup.alpha <= 0)
                    {
                        if (debugMode) Debug.Log("[SmartCursor] PauseMenu actif mais invisible (alpha=0)");
                        continue;
                    }
                    
                    // Vérifie aussi la scale (certains menus utilisent scale 0 pour cacher)
                    if (panel.transform.localScale == Vector3.zero)
                    {
                        if (debugMode) Debug.Log("[SmartCursor] PauseMenu actif mais invisible (scale=0)");
                        continue;
                    }
                }
                
                shouldShowCursor = true;
                if (debugMode) Debug.Log($"[SmartCursor] Panel actif: {panel.name}");
                break;
            }
        }
        
        // Vérifie aussi si DialogueUI est active (au cas où c'est un composant)
        if (!shouldShowCursor)
        {
            DialogueUI dialogueUI = FindObjectOfType<DialogueUI>();
            if (dialogueUI != null && dialogueUI.IsDialogueOpen())
            {
                shouldShowCursor = true;
                if (debugMode) Debug.Log("[SmartCursor] DialogueUI active");
            }
        }
        
        // Applique l'état du curseur
        Cursor.visible = shouldShowCursor;
    }
    
    // Méthodes publiques pour contrôle manuel
    public void ShowCursor() => Cursor.visible = true;
    public void HideCursor() => Cursor.visible = false;
}
