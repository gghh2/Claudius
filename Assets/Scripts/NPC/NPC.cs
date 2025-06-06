using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("NPC Configuration")]
    [Tooltip("Nom du PNJ affiché dans les dialogues")]
    public string npcName = "Alien Trader";
    
    [Tooltip("Personnalité/Rôle du PNJ (ex: Marchand, Garde, Scientifique)")]
    public string npcRole = "Trader";
    
    [Tooltip("Description courte du PNJ pour l'IA")]
    [TextArea(2, 4)]
    public string npcDescription = "Un marchand alien qui vend des équipements spatiaux";
    
    [Header("Interaction Settings")]
    [Tooltip("Distance à laquelle le joueur peut interagir")]
    public float interactionRange = 3f;
    
    [Tooltip("Couleur du PNJ (optionnel - pour différencier visuellement)")]
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
    
    // Méthode utile pour récupérer les infos du PNJ (pour l'IA plus tard)
    public NPCData GetNPCData()
    {
        return new NPCData
        {
            name = npcName,
            role = npcRole,
            description = npcDescription
        };
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