using UnityEngine;
using System.Linq;
using System.Collections;

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
        NPCNameDisplay nameDisplay = GetComponent<NPCNameDisplay>();
        
        if (show)
        {
            Debug.Log($"Appuyez sur E pour parler à {npcName} ({npcRole})");
            
            // Utilise le NPCNameDisplay s'il existe
            if (nameDisplay != null)
            {
                string formattedName = TextFormatter.FormatName(npcName);
                nameDisplay.SetDisplayName($"{formattedName}\n[E] Parler");
                nameDisplay.SetNameColor(Color.white);
            }
            else
            {
                // Fallback sur InteractionPrompt si pas de NPCNameDisplay
                string formattedName = TextFormatter.FormatName(npcName);
                InteractionPrompt.Show($"Appuyez sur E pour parler à {formattedName}", transform, new Vector3(0, 2f, 0));
            }
        }
        else
        {
            // Restaure l'affichage normal
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
        
        // Cache le nom pendant le dialogue (optionnel)
        NPCNameDisplay nameDisplay = GetComponent<NPCNameDisplay>();
        if (nameDisplay != null)
        {
            nameDisplay.HideName();
        }
        
        // Vérifie si c'est un NPC de livraison
        QuestObject questObj = GetComponent<QuestObject>();
        if (questObj != null && questObj.isDeliveryTarget)
        {
            HandleDeliveryDialogue(questObj);
        }
        else
        {
            // NOUVEAU: Vérifie si le joueur a une quête FETCH active avec ce NPC
            bool hasFetchQuestReady = CheckForFetchQuestCompletion();
            
            // Si pas de quête FETCH prête, dialogue normal
            if (!hasFetchQuestReady)
            {
                // Dialogue normal avec IA
                if (DialogueUI.Instance != null)
                {
                    DialogueUI.Instance.StartDialogue(GetNPCData());
                }
                else
                {
                    Debug.LogError("DialogueUI non trouvé !");
                }
            }
        }
    }
    
    void HandleDeliveryDialogue(QuestObject questObj)
    {
        // Récupère les infos de la quête
        var activeQuest = QuestManager.Instance?.activeQuests.FirstOrDefault(q => q.questId == questObj.questId);
        if (activeQuest == null) return;
        
        string packageName = activeQuest.questData.objectName;
        string giverName = activeQuest.giverNPCName;
        
        // NOUVEAU: Formate les noms pour l'affichage
        string formattedPackageName = TextFormatter.FormatName(packageName);
        string formattedGiverName = TextFormatter.FormatName(giverName);
        string formattedNPCName = TextFormatter.FormatName(npcName);
        
        // Vérifie si le joueur a le colis
        bool hasPackage = PlayerInventory.Instance != null && 
                         PlayerInventory.Instance.HasItemsForQuest(packageName, 1, questObj.questId);
        
        if (DialogueUI.Instance != null)
        {
            // Ouvre le dialogue
            DialogueUI.Instance.StartDialogue(GetNPCData());
            
            // Affiche le message approprié
            if (hasPackage)
            {
                string message = $"{formattedNPCName}: Ah ! Vous avez {formattedPackageName} de la part de {formattedGiverName} ! " +
                               $"C'est exactement ce que j'attendais. Merci beaucoup pour la livraison !";
                DialogueUI.Instance.ShowText(message);
                
                // Configure un bouton spécial pour rendre la quête
                DialogueUI.Instance.ShowDeliveryButton(questObj.questId, packageName);
            }
            else
            {
                string message = $"{formattedNPCName}: Bonjour ! J'attends {formattedPackageName} de la part de {formattedGiverName}. " +
                               $"Revenez me voir quand vous l'aurez.";
                DialogueUI.Instance.ShowText(message);
            }
        }
    }
    
    bool CheckForFetchQuestCompletion()
    {
        if (QuestJournal.Instance == null || PlayerInventory.Instance == null) return false;
        
        // Cherche une quête FETCH active avec ce NPC
        var activeQuests = QuestJournal.Instance.GetActiveQuests();
        // NOUVEAU: Compare avec le nom formaté car giverNPCName est formaté dans JournalQuest
        string formattedNPCName = TextFormatter.FormatName(npcName);
        
        Debug.Log($"[FETCH] Recherche quête pour NPC: '{npcName}' (formaté: '{formattedNPCName}')");
        Debug.Log($"[FETCH] Quêtes actives: {activeQuests.Count}");
        
        var fetchQuest = activeQuests.FirstOrDefault(q => 
            q.giverNPCName == formattedNPCName && 
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
                
                // Configure le dialogue pour afficher le bouton de rendu
                if (DialogueUI.Instance != null)
                {
                    // Démarre le dialogue d'abord
                    DialogueUI.Instance.StartDialogue(GetNPCData());
                    
                    // Attend un frame pour éviter les conflits
                    StartCoroutine(ShowFetchQuestDialogueDelayed(fetchQuest.questId, objectName, fetchQuest.maxProgress));
                }
                
                return true; // Indique qu'une quête FETCH est prête
            }
        }
        else
        {
            Debug.Log($"[FETCH] Aucune quête FETCH trouvée pour '{formattedNPCName}'");
            foreach (var quest in activeQuests)
            {
                Debug.Log($"  - Quête: {quest.questTitle}, Donneur: '{quest.giverNPCName}', Type: {quest.questType}");
            }
        }
        
        return false; // Pas de quête FETCH prête
    }
    
    System.Collections.IEnumerator ShowFetchQuestDialogueDelayed(string questId, string objectName, int quantity)
    {
        // Attend un frame pour que le dialogue soit bien initialisé
        yield return null;
        
        // NOUVEAU: Formate les noms pour l'affichage
        string formattedNPCName = TextFormatter.FormatName(npcName);
        string formattedObjectName = TextFormatter.FormatName(objectName);
        
        // Affiche le message et le bouton de rendu
        string message = $"{formattedNPCName}: Ah ! Je vois que vous avez récupéré tous les {formattedObjectName} que je vous avais demandés ! " +
                       $"Excellent travail ! Voulez-vous me les remettre ?";
        
        DialogueUI.Instance.ShowText(message);
        DialogueUI.Instance.ShowFetchQuestButton(questId, objectName, quantity);
    }
    
    string ExtractObjectNameFromDescription(string description)
    {
        // La description est maintenant formatée, donc on doit chercher avec une casse insensible
        // Format attendu: "Trouvez X objet name dans zone"
        string[] words = description.Split(' ');
        
        for (int i = 0; i < words.Length - 2; i++)
        {
            if (words[i].ToLower() == "trouvez" && int.TryParse(words[i + 1], out _))
            {
                // Reconstitue le nom de l'objet (peut être sur plusieurs mots)
                string objectName = "";
                for (int j = i + 2; j < words.Length; j++)
                {
                    if (words[j].ToLower() == "dans")
                        break;
                    
                    if (!string.IsNullOrEmpty(objectName))
                        objectName += "_";
                    objectName += words[j].ToLower();
                }
                
                Debug.Log($"[EXTRACT] Description: '{description}' -> Objet: '{objectName}'");
                return objectName;
            }
        }
        
        Debug.LogWarning($"[EXTRACT] Format de description non reconnu: {description}");
        return "objet_inconnu";
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