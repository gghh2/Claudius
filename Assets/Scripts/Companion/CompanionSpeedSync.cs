using UnityEngine;

// Extension pour synchroniser la vitesse du compagnon avec celle du joueur
[RequireComponent(typeof(CompanionController))]
public class CompanionSpeedSync : MonoBehaviour
{
    [Header("Speed Synchronization")]
    [Tooltip("Synchroniser automatiquement avec la vitesse du joueur")]
    public bool autoSyncSpeed = true;
    
    [Tooltip("Le compagnon accélère aussi quand le joueur sprinte")]
    public bool followPlayerSprint = true;
    
    [Tooltip("Multiplicateur supplémentaire pendant le sprint (1 = même ratio que le joueur)")]
    [Range(0.8f, 1.2f)]
    public float sprintRatioMultiplier = 1f;
    
    [Header("Debug")]
    [Tooltip("Active le debug GUI (F8 pour toggle)")]
    public bool showDebugGUI = false;
    
    // Références
    private CompanionController companion;
    private PlayerControllerCC playerController;
    private float baseSpeed;
    
    void Start()
    {
        companion = GetComponent<CompanionController>();
        
        // Trouve le joueur
        playerController = FindFirstObjectByType<PlayerControllerCC>();
        
        if (playerController != null && autoSyncSpeed)
        {
            SyncWithPlayer();
        }
    }
    
    void Update()
    {
        if (!autoSyncSpeed || playerController == null || companion == null) return;
        
        // Ajuste la vitesse en temps réel si le joueur sprinte
        if (followPlayerSprint)
        {
            // Utilise le speedMultiplier du CompanionController
            float targetSpeed = baseSpeed * companion.speedMultiplier;
            
            if (playerController.IsSprinting())
            {
                // Calcule le ratio de sprint du joueur
                float playerSprintRatio = playerController.sprintSpeed / playerController.moveSpeed;
                targetSpeed = baseSpeed * companion.speedMultiplier * playerSprintRatio * sprintRatioMultiplier;
                
                // Applique une transition fluide
                companion.moveSpeed = Mathf.Lerp(companion.moveSpeed, targetSpeed, Time.deltaTime * 3f);
            }
            else
            {
                // Retour à la vitesse normale
                companion.moveSpeed = Mathf.Lerp(companion.moveSpeed, targetSpeed, Time.deltaTime * 3f);
            }
        }
    }
    
    [ContextMenu("Sync With Player Speed")]
    public void SyncWithPlayer()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerControllerCC>();
        }
        
        if (playerController != null && companion != null)
        {
            // Synchronise avec la vitesse de base du joueur
            baseSpeed = playerController.moveSpeed;
            companion.moveSpeed = baseSpeed * companion.speedMultiplier;
            
            Debug.Log($"🏃 Vitesse synchronisée:");
            Debug.Log($"  - Vitesse du joueur: {playerController.moveSpeed} m/s");
            Debug.Log($"  - Multiplicateur (CompanionController): {companion.speedMultiplier}x");
            Debug.Log($"  - Vitesse du compagnon: {companion.moveSpeed} m/s");
            
            if (followPlayerSprint)
            {
                Debug.Log($"  - Sprint activé: Le compagnon accélérera aussi");
                float maxSpeed = baseSpeed * companion.speedMultiplier * (playerController.sprintSpeed / playerController.moveSpeed) * sprintRatioMultiplier;
                Debug.Log($"  - Vitesse max compagnon: {maxSpeed:F1} m/s");
            }
        }
        else
        {
            Debug.LogError("❌ PlayerController ou CompanionController non trouvé!");
        }
    }
    
    void OnValidate()
    {
        // Re-sync quand les valeurs changent dans l'Inspector
        if (Application.isPlaying && autoSyncSpeed && playerController != null)
        {
            SyncWithPlayer();
        }
    }
    
    void OnGUI()
    {
        if (!showDebugGUI) return;
        
        // Toggle avec F8
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F8)
        {
            showDebugGUI = !showDebugGUI;
        }
        
        GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 180));
        GUILayout.Box("=== COMPANION SPEED SYNC ===");
        
        if (playerController != null && companion != null)
        {
            GUILayout.Label($"Joueur: {playerController.moveSpeed} m/s");
            GUILayout.Label($"Compagnon: {companion.moveSpeed:F1} m/s");
            
            if (playerController.IsSprinting())
            {
                GUILayout.Label("État: SPRINT ACTIF", GUI.skin.box);
                GUILayout.Label($"Vitesse sprint joueur: {playerController.sprintSpeed} m/s");
            }
            else
            {
                GUILayout.Label("État: Marche normale");
            }
            
            GUILayout.Label($"Multiplicateur de base: {companion.speedMultiplier:F2}x");
            GUILayout.Label($"Sprint ratio multiplier: {sprintRatioMultiplier:F2}x");
            
            // Calcul de la vitesse théorique
            float theoreticalSpeed = baseSpeed * companion.speedMultiplier;
            if (playerController.IsSprinting())
            {
                float sprintRatio = playerController.sprintSpeed / playerController.moveSpeed;
                theoreticalSpeed *= sprintRatio * sprintRatioMultiplier;
            }
            GUILayout.Label($"Vitesse théorique: {theoreticalSpeed:F1} m/s");
        }
        
        GUILayout.EndArea();
    }
}
