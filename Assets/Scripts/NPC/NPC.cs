using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("===== AI CONFIGURATION - Used by AI System =====")]
    
    [Header("NPC Identity (AI)")]
    [Tooltip("AI SYSTEM - NPC name displayed in dialogues")]
    public string npcName = "Alien Trader";
    
    [Tooltip("AI SYSTEM - Role determines personality and dialogue style")]
    public string npcRole = "Trader";
    
    [Header("NPC Description (AI)")]
    [Tooltip("AI SYSTEM - Detailed description for coherent dialogue generation")]
    [TextArea(2, 4)]
    public string npcDescription = "Un marchand alien qui vend des équipements spatiaux";
    
    [Space(20)]
    [Header("===== TECHNICAL CONFIGURATION - Not used by AI =====")]
    
    [Header("Interaction Settings")]
    [Tooltip("Technical - Distance at which player can interact")]
    public float interactionRange = 3f;
    
    [Header("Visual Settings")]
    [Tooltip("Visual - NPC color (optional - for visual differentiation)")]
    public Color npcColor = Color.white;
    
    private Transform player;
    private bool playerInRange = false;
    private Renderer npcRenderer;
    
    void Start()
    {
        player = FindObjectOfType<PlayerController>().transform;
        npcRenderer = GetComponent<Renderer>();
        
        // Applique la couleur du PNJ
        if (npcRenderer != null)
        {
            npcRenderer.material.color = npcColor;
        }
        
        // Validation des données
        if (string.IsNullOrEmpty(npcName))
        {
            npcName = "Unknown NPC";
            Debug.LogWarning($"PNJ {gameObject.name} n'a pas de nom défini !");
        }
    }
    
    void Update()
    {
        CheckPlayerDistance();
    }
    
    void CheckPlayerDistance()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= interactionRange && !playerInRange)
        {
            playerInRange = true;
            ShowInteractionPrompt(true);
        }
        else if (distance > interactionRange && playerInRange)
        {
            playerInRange = false;
            ShowInteractionPrompt(false);
        }
        
        // Interaction avec E - SEULEMENT si aucun dialogue n'est ouvert
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Vérifier si l'UI de dialogue est ouverte
            if (DialogueUI.Instance != null && !DialogueUI.Instance.IsDialogueOpen())
            {
                StartDialogue();
            }
        }
    }
    
    void ShowInteractionPrompt(bool show)
    {
        if (show)
        {
            Debug.Log($"Appuyez sur E pour parler à {npcName} ({npcRole})");
        }
    }
    
    void StartDialogue()
    {
        Debug.Log($"=== Dialogue avec {npcName} ===");
        
        // Arrête le mouvement du NPC
        NPCMovement movement = GetComponent<NPCMovement>();
        if (movement != null)
        {
            movement.StopMovement();
        }
        
        // Cache le nom pendant le dialogue (optionnel)
        NPCNameDisplay nameDisplay = GetComponent<NPCNameDisplay>();
        if (nameDisplay != null)
        {
            nameDisplay.HideName();
        }
        
        // Utilise la nouvelle interface
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.StartDialogue(GetNPCData());
        }
        else
        {
            Debug.LogError("DialogueUI non trouvé !");
        }
    }
    
    // Méthode utile pour récupérer les infos du PNJ (pour l'IA)
    public NPCData GetNPCData()
    {
        return new NPCData
        {
            name = npcName,
            role = npcRole,
            description = npcDescription
        };
    }
    
    [ContextMenu("Debug AI Fields")]
    public void DebugAIFields()
    {
        Debug.Log($"=== AI FIELDS for {gameObject.name} ===");
        Debug.Log($"NPC Name: {npcName}");
        Debug.Log($"NPC Role: {npcRole}");
        Debug.Log($"NPC Description: {npcDescription}");
        Debug.Log("=====================================");
    }
}

// Structure pour organiser les données du PNJ
[System.Serializable]
public class NPCData
{
    public string name;
    public string role;
    public string description;
}