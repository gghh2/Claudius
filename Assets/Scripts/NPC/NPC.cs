using UnityEngine;
using System.Linq;
using System.Collections;

public class NPC : MonoBehaviour
{
    [Header("===== NPC Configuration =====")]
    
    [Header("NPC Identity")]
    [Tooltip("NPC name displayed in dialogues")]
    public string npcName = "Alien Trader";
    
    [Tooltip("Role determines personality and dialogue style")]
    public string npcRole = "Trader";
    
    [Header("NPC Description")]
    [Tooltip("Detailed description for AI dialogue generation")]
    [TextArea(2, 4)]
    public string npcDescription = "Un marchand alien qui vend des équipements spatiaux";
    
    [Header("Interaction Settings")]
    [Tooltip("Distance at which player can interact")]
    public float interactionRange = 3f;
    
    [Header("Visual Settings")]
    [Tooltip("NPC color (optional - for visual differentiation)")]
    public Color npcColor = Color.white;
    
    private Transform player;
    private bool playerInRange = false;
    private Renderer npcRenderer;
    
    void Start()
    {
        player = FindObjectOfType<PlayerControllerCC>().transform;
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
            if (DialogueUI.Instance != null && !DialogueUI.Instance.IsDialogueOpen())
            {
                StartDialogue();
            }
        }
    }
    
    void ShowInteractionPrompt(bool show)
    {
        NPCNameDisplay nameDisplay = GetComponent<NPCNameDisplay>();
        
        if (show)
        {
            Debug.Log($"Appuyez sur E pour parler à {npcName} ({npcRole})");
            
            if (nameDisplay != null)
            {
                string formattedName = TextFormatter.FormatName(npcName);
                nameDisplay.SetDisplayName($"{formattedName}\n[E] Parler");
                nameDisplay.SetNameColor(Color.white);
            }
            else
            {
                string formattedName = TextFormatter.FormatName(npcName);
                InteractionPrompt.Show($"Appuyez sur E pour parler à {formattedName}", transform, new Vector3(0, 2f, 0));
            }
        }
        else
        {
            if (nameDisplay != null)
            {
                nameDisplay.RefreshDisplayName();
            }
            else
            {
                InteractionPrompt.HideIfCaller(transform);
            }
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
        
        // Cache le nom pendant le dialogue
        NPCNameDisplay nameDisplay = GetComponent<NPCNameDisplay>();
        if (nameDisplay != null)
        {
            nameDisplay.HideName();
        }
        
        // Vérifie les cas spéciaux de quêtes
        QuestObject questObj = GetComponent<QuestObject>();
        
        // Cas 1: NPC de livraison
        if (questObj != null && questObj.isDeliveryTarget)
        {
            HandleDeliveryDialogue(questObj);
            return;
        }
        
        // Cas 2: Vérifier si le joueur peut rendre une quête FETCH
        bool hasFetchQuestReady = CheckForFetchQuestCompletion();
        if (hasFetchQuestReady)
        {
            // Le dialogue sera géré par CheckForFetchQuestCompletion
            return;
        }
        
        // Cas normal: Dialogue IA standard
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.StartDialogue(GetNPCData());
        }
        else
        {
            Debug.LogError("DialogueUI non trouvé !");
        }
    }
    
    void HandleDeliveryDialogue(QuestObject questObj)
    {
        // Récupère les infos de la quête
        var activeQuest = QuestManager.Instance?.activeQuests.FirstOrDefault(q => q.questId == questObj.questId);
        if (activeQuest == null) return;
        
        string packageName = activeQuest.questData.objectName;
        string giverName = activeQuest.giverNPCName;
        
        // Vérifie si le joueur a le colis
        bool hasPackage = PlayerInventory.Instance != null && 
                         PlayerInventory.Instance.HasItemsForQuest(packageName, 1, questObj.questId);
        
        if (DialogueUI.Instance != null)
        {
            // Démarre le dialogue avec l'IA
            DialogueUI.Instance.StartDialogue(GetNPCData());
            
            // L'IA devrait gérer le dialogue de livraison
            // Mais on configure quand même le bouton si le joueur a le paquet
            if (hasPackage)
            {
                DialogueUI.Instance.ShowDeliveryButton(questObj.questId, packageName);
            }
        }
    }
    
    bool CheckForFetchQuestCompletion()
    {
        if (QuestJournal.Instance == null || PlayerInventory.Instance == null) return false;
        
        var activeQuests = QuestJournal.Instance.GetActiveQuests();
        
        Debug.Log($"[FETCH] Recherche quête pour NPC: '{npcName}'");
        Debug.Log($"[FETCH] Quêtes actives: {activeQuests.Count}");
        
        var fetchQuest = activeQuests.FirstOrDefault(q => 
            q.giverNPCName == npcName && 
            q.questType == QuestType.FETCH);
        
        if (fetchQuest != null)
        {
            Debug.Log($"[FETCH] Quête FETCH trouvée: {fetchQuest.questTitle}");
            
            // Extrait le nom de l'objet depuis la description
            string objectName = ExtractObjectNameFromDescription(fetchQuest.description);
            
            Debug.Log($"[FETCH] Objet recherché: {objectName} x{fetchQuest.maxProgress}");
            
            // Vérifie si le joueur a tous les objets nécessaires
            bool hasAllItems = PlayerInventory.Instance.HasItemsForQuest(
                objectName, 
                fetchQuest.maxProgress, 
                fetchQuest.questId
            );
            
            Debug.Log($"[FETCH] Joueur a tous les objets: {hasAllItems}");
            
            if (hasAllItems)
            {
                Debug.Log($"✅ Le joueur a tous les objets pour la quête FETCH: {fetchQuest.questTitle}");
                
                if (DialogueUI.Instance != null)
                {
                    // Démarre le dialogue d'abord
                    DialogueUI.Instance.StartDialogue(GetNPCData());
                    
                    // Configure le bouton de rendu après un frame
                    StartCoroutine(ShowFetchQuestDialogueDelayed(fetchQuest.questId, objectName, fetchQuest.maxProgress));
                }
                
                return true;
            }
        }
        else
        {
            Debug.Log($"[FETCH] Aucune quête FETCH trouvée pour '{npcName}'");
        }
        
        return false;
    }
    
    IEnumerator ShowFetchQuestDialogueDelayed(string questId, string objectName, int quantity)
    {
        // Attend un frame pour que le dialogue soit bien initialisé
        yield return null;
        
        // Configure le bouton de rendu
        DialogueUI.Instance.ShowFetchQuestButton(questId, objectName, quantity);
    }
    
    string ExtractObjectNameFromDescription(string description)
    {
        // La description est formatée, on doit extraire le nom de l'objet
        string[] words = description.Split(' ');
        
        for (int i = 0; i < words.Length - 2; i++)
        {
            if (words[i].ToLower() == "trouvez" && int.TryParse(words[i + 1], out _))
            {
                // Reconstitue le nom de l'objet
                string objectName = "";
                for (int j = i + 2; j < words.Length; j++)
                {
                    if (words[j].ToLower() == "dans")
                        break;
                    
                    if (!string.IsNullOrEmpty(objectName))
                        objectName += " ";
                    objectName += words[j];
                }
                
                // Convertit en format avec underscores pour le système interne
                string internalName = objectName.Replace(" ", "_").ToLower();
                
                Debug.Log($"[EXTRACT] Description: '{description}' -> Objet interne: '{internalName}'");
                return internalName;
            }
        }
        
        Debug.LogWarning($"[EXTRACT] Format de description non reconnu: {description}");
        return "objet_inconnu";
    }
    
    public NPCData GetNPCData()
    {
        return new NPCData
        {
            name = npcName,
            role = npcRole,
            description = npcDescription
        };
    }
    
    [ContextMenu("Debug NPC Info")]
    public void DebugNPCInfo()
    {
        Debug.Log($"=== NPC Info for {gameObject.name} ===");
        Debug.Log($"Name: {npcName}");
        Debug.Log($"Role: {npcRole}");
        Debug.Log($"Description: {npcDescription}");
        Debug.Log($"Interaction Range: {interactionRange}");
        Debug.Log($"Color: {npcColor}");
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
