using UnityEngine;
using TMPro;
using System.Linq;

public class QuestObject : MonoBehaviour
{
	[Header("===== AI CONFIGURATION - Used by AI System =====")]
    
    [Header("Quest Object Info (AI)")]
    [Tooltip("AI SYSTEM - Object name displayed in dialogues and quests")]
    public string objectName;
    
    [Tooltip("AI SYSTEM - Object type for quest generation")]
    public QuestObjectType objectType;
    
    [Space(20)]
    [Header("===== TECHNICAL CONFIGURATION - Not used by AI =====")]
    
    [Header("Quest Tracking")]
    [Tooltip("Technical - Associated quest ID")]
    public string questId;
    
    [Tooltip("Technical - Collection status")]
    public bool isCollected = false;
    
    [Tooltip("Technical - Is this NPC a delivery target")]
    public bool isDeliveryTarget = false;
    
    [Header("Visual Settings")]
    [Tooltip("Visual - Highlight effect GameObject")]
    public GameObject highlightEffect;
    
    [Tooltip("Visual - Glow color")]
    public Color glowColor = Color.yellow;
    
    [Header("Name Display")]
    [Tooltip("Visual - Name display offset")]
    public Vector3 nameOffset = new Vector3(0, 1.5f, 0);
    
    [Tooltip("Visual - Font size")]
    public float fontSize = 3f;
    
    [Header("Interaction Settings")]
    [Tooltip("Technical - Trigger radius")]
    public float triggerRadius = 2f;
    
    [Header("Debug")]
    [Tooltip("Debug - Show detailed logs")]
    public bool debugMode = true;

    [Header("Exploration Settings")]
    [Tooltip("Temps requis dans la zone pour valider l'exploration (secondes)")]
    public float explorationTimeRequired = 2f;
    private float explorationTimer = 0f;
    private bool isExploring = false;
    
    // Private variables
    private bool playerInRange = false;
    private Renderer objectRenderer;
    private GameObject nameDisplay;
    private TextMeshPro nameText;
    private Camera mainCamera;
    private SphereCollider triggerCollider;
    
    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        mainCamera = Camera.main;
        
        if (debugMode)
            Debug.Log($"🔧 QuestObject Start() - {objectName} ({objectType})");
        
        // Setup du collider trigger
        SetupTriggerCollider();
        
        // Ajoute un effet de glow si pas d'effet custom
        if (highlightEffect == null && objectRenderer != null)
        {
            objectRenderer.material.SetColor("_EmissionColor", glowColor * 0.3f);
            objectRenderer.material.EnableKeyword("_EMISSION");
        }
        
        // Crée l'affichage du nom
        CreateNameDisplay();
        
        // NOUVEAU: Vérification du tag Player dans la scène
        CheckPlayerTag();
        
        if (debugMode)
            Debug.Log($"✅ Objet de quête configuré: {objectName} - Trigger radius: {triggerRadius}");
    }
    
    // NOUVEAU: Vérification debug
    void CheckPlayerTag()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (debugMode)
                Debug.Log($"✅ Joueur trouvé avec tag 'Player': {player.name}");
        }
        else
        {
            Debug.LogWarning("❌ Aucun GameObject avec tag 'Player' trouvé ! Assignez le tag au joueur.");
        }
    }
    
    void SetupTriggerCollider()
    {
        // Cherche s'il y a déjà un trigger pour la détection
        Collider[] allColliders = GetComponents<Collider>();
        bool hasTrigger = false;
        
        foreach (Collider col in allColliders)
        {
            if (col.isTrigger)
            {
                hasTrigger = true;
                triggerCollider = col as SphereCollider;
                if (debugMode)
                    Debug.Log($"✅ Trigger existant trouvé: {col.GetType().Name}");
                break;
            }
        }
        
        // Si pas de trigger, ajoute un SphereCollider SUPPLEMENTAIRE pour la détection
        if (!hasTrigger)
        {
            // Ajoute un nouveau GameObject enfant pour le trigger
            GameObject triggerObject = new GameObject("QuestTriggerZone");
            triggerObject.transform.SetParent(transform);
            triggerObject.transform.localPosition = Vector3.zero;
            
            // Ajoute le SphereCollider trigger sur l'enfant
            triggerCollider = triggerObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = triggerRadius;
            
            // Assure que le trigger est sur le même layer
            triggerObject.layer = gameObject.layer;
            
            // IMPORTANT: Ajoute un Rigidbody kinematic pour que les triggers fonctionnent
            if (GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                if (debugMode)
                    Debug.Log("🔧 Rigidbody kinematic ajouté pour les triggers");
            }
            
            if (debugMode)
                Debug.Log($"✅ Nouveau trigger créé sur GameObject enfant - Radius: {triggerRadius}");
        }
    }
    
    void CreateNameDisplay()
    {
        nameDisplay = new GameObject($"{gameObject.name}_QuestName");
        nameDisplay.transform.SetParent(transform);
        nameDisplay.transform.localPosition = nameOffset;
        
        nameText = nameDisplay.AddComponent<TextMeshPro>();
        
        string displayText = GetDisplayText();
        nameText.text = displayText;
        nameText.fontSize = fontSize;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
        
        Color textColor = GetTextColor();
        nameText.color = textColor;
        
        nameText.fontStyle = FontStyles.Bold;
        nameText.outlineWidth = 0.2f;
        nameText.outlineColor = Color.black;
        
        if (debugMode)
            Debug.Log($"📝 Nom affiché créé: {displayText}");
    }
    
    string GetDisplayText()
    {
        // NOUVEAU: Formate le nom pour enlever les underscores
        string formattedName = TextFormatter.FormatName(objectName);
        
        switch (objectType)
        {
            case QuestObjectType.Item:
                return $"📦 {formattedName}";
            case QuestObjectType.NPC:
                // Pour les NPCs de livraison, affiche "Livrer à [nom]"
                if (isDeliveryTarget)
                    return $"📦 Livrer à {formattedName}";
                else
                    return $"👤 {formattedName}";
            case QuestObjectType.InteractableObject:
                return $"🔧 {formattedName}";
            case QuestObjectType.Marker:
                return $"📍 Explorer: {formattedName}";
            default:
                return formattedName;
        }
    }
    
    Color GetTextColor()
    {
        switch (objectType)
        {
            case QuestObjectType.Item:
                return Color.cyan;
            case QuestObjectType.NPC:
                return Color.green;
            case QuestObjectType.InteractableObject:
                return new Color(1f, 0.5f, 0f);
            case QuestObjectType.Marker:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
    
    void Update()
    {
        // Billboard effect - always face camera
        if (nameDisplay != null && mainCamera != null)
        {
            // Make the text face the camera's forward direction
            nameDisplay.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
        }
        
        // Interaction
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isCollected)
        {
            if (debugMode)
                Debug.Log($"🎯 Tentative d'interaction avec {objectName}");
            InteractWithObject();
        }
        
        // Effet de pulsation
        if (objectRenderer != null && !isCollected)
        {
            float pulse = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
            objectRenderer.material.SetColor("_EmissionColor", glowColor * pulse * 0.5f);
        }
        
        // NOUVEAU : Timer d'exploration pour les marqueurs
        if (objectType == QuestObjectType.Marker && playerInRange && !isCollected)
        {
            if (!isExploring)
            {
                isExploring = true;
                explorationTimer = 0f;
                Debug.Log($"🗺️ Début exploration de {objectName}...");
            }
            
            explorationTimer += Time.deltaTime;
            
            // Affiche la progression
            if (nameText != null)
            {
                float progress = Mathf.Clamp01(explorationTimer / explorationTimeRequired);
                string formattedName = TextFormatter.FormatName(objectName);
                nameText.text = $"📍 {formattedName}\nExploration: {Mathf.RoundToInt(progress * 100)}%";
                
                // Change la couleur selon la progression
                nameText.color = Color.Lerp(Color.yellow, Color.green, progress);
                
                // Augmente la taille pendant l'exploration
                nameText.fontSize = fontSize * 1.3f;
            }
            
            // Validation automatique après le délai
            if (explorationTimer >= explorationTimeRequired)
            {
                Debug.Log($"🗺️ ZONE EXPLORÉE : {objectName}");
                ExploreMarker();
            }
        }
        
        // Interaction normale pour les autres types d'objets
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isCollected && objectType != QuestObjectType.Marker)
        {
            if (debugMode)
                Debug.Log($"🎯 Tentative d'interaction avec {objectName}");
            InteractWithObject();
        }


    }
    
    void InteractWithObject()
    {
        // Pour les NPCs de livraison, ne pas marquer comme collecté ici
        if (objectType == QuestObjectType.NPC && isDeliveryTarget)
        {
            // Ne rien faire - l'interaction se fait via le dialogue
            Debug.Log($"[DELIVERY] Interaction avec NPC destinataire - Ouverture du dialogue");
            return;
        }
        
        switch (objectType)
        {
            case QuestObjectType.Item:
                CollectItem();
                break;
            case QuestObjectType.InteractableObject:
                ActivateObject();
                break;
            case QuestObjectType.Marker:
                ExploreMarker();
                break;
            case QuestObjectType.NPC:
                ActivateObject();
                break;
        }
    }
    
    void CollectItem()
    {
        isCollected = true;
        Debug.Log($"✅ COLLECTE RÉUSSIE: {objectName} pour quête {questId}");
        
        // Ajoute à l'inventaire
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddItem(objectName, 1, questId);
            Debug.Log($"📦 Ajouté à l'inventaire: {objectName}");
        }
        else
        {
            Debug.LogWarning("❌ PlayerInventory.Instance est NULL !");
        }
        
        // Met à jour la quête
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnObjectCollected(questId, objectName);
        }
        else
        {
            Debug.LogWarning("❌ QuestManager.Instance est NULL !");
        }
        
        // Destroy immediately
        Debug.Log($"🗑️ Destruction de l'objet: {objectName}");
        Destroy(gameObject);
    }
    
    void ActivateObject()
    {
        // Check if this is a delivery target
        if (isDeliveryTarget)
        {
            HandleDelivery();
            return;
        }
        
        // NOUVEAU: Pour les NPCs de quête TALK, ne pas les détruire
        bool isTalkQuestNPC = (objectType == QuestObjectType.NPC && !isDeliveryTarget);
        
        isCollected = true;
        Debug.Log($"🔧 Objet activé: {objectName}");
        
        QuestManager.Instance?.OnObjectInteracted(questId, objectName);
        
        if (objectRenderer != null)
        {
            objectRenderer.material.color = Color.green;
        }
        
        if (nameText != null)
        {
            nameText.text = "✅ Activé";
            nameText.color = Color.green;
        }
        
        // NOUVEAU: Pour les NPCs de quête TALK, nettoyage spécial sans destruction
        if (isTalkQuestNPC)
        {
            Debug.Log($"[TALK] NPC {objectName} reste en place après la quête");
            CleanupTalkQuestNPC();
        }
    }
    
    void HandleDelivery()
    {
        // Vérifie si le joueur a l'objet à livrer
        var activeQuest = QuestManager.Instance?.activeQuests.FirstOrDefault(q => q.questId == questId);
        if (activeQuest == null)
        {
            Debug.LogError($"[DELIVERY] Quête introuvable: {questId}");
            return;
        }
        
        string packageName = activeQuest.questData.objectName;
        
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasItemsForQuest(packageName, 1, questId))
        {
            // Retire l'objet de l'inventaire
            PlayerInventory.Instance.RemoveItem(packageName, 1, questId);
            
            Debug.Log($"🚚 LIVRAISON RÉUSSIE: {packageName} livré à {objectName}");
            
            // Complète la quête
            isCollected = true;
            
            // Pour les quêtes DELIVERY, on complète directement ici
            if (QuestManager.Instance != null)
            {
                var quest = QuestManager.Instance.activeQuests.FirstOrDefault(q => q.questId == questId);
                if (quest != null)
                {
                    // Met à jour le journal
                    if (QuestJournal.Instance != null)
                    {
                        QuestJournal.Instance.CompleteQuest(questId);
                    }
                    
                    // Joue le son de complétion
                    QuestManager.Instance.PlayQuestCompleteSoundPublic();
                    
                    // Nettoie la quête SANS détruire le NPC
                    CleanupDeliveryQuest(quest);
                }
            }
            
            // Effet visuel de succès
            if (nameText != null)
            {
                nameText.text = $"✅ {packageName} livré !";
                nameText.color = Color.green;
                nameText.fontSize = fontSize * 1.5f;
            }
            
            // NE PAS détruire le NPC après livraison
            // StartCoroutine(ExplorationCompleteEffect()); // Commenté pour garder le NPC
        }
        else
        {
            Debug.LogWarning($"[DELIVERY] Le joueur n'a pas {packageName} dans son inventaire");
            
            if (nameText != null)
            {
                nameText.text = $"❌ Il me faut: {packageName}";
                nameText.color = Color.red;
            }
        }
    }
    
    // Méthode publique pour le bouton UI
    public void HandleDeliveryFromUI()
    {
        HandleDelivery();
    }
    
    // NOUVELLE MÉTHODE: Nettoyage spécial pour les NPCs de quête TALK
    void CleanupTalkQuestNPC()
    {
        // Détruit seulement l'affichage du nom créé par QuestObject
        if (nameDisplay != null)
        {
            Destroy(nameDisplay);
            nameDisplay = null;
            nameText = null;
        }
        
        // Retire l'effet glow
        if (objectRenderer != null)
        {
            // Désactive l'émission
            objectRenderer.material.DisableKeyword("_EMISSION");
            objectRenderer.material.SetColor("_EmissionColor", Color.black);
            
            // Restaure la couleur normale du material
            objectRenderer.material.color = Color.white;
        }
        
        // Désactive le trigger de quête
        GameObject triggerZone = transform.Find("QuestTriggerZone")?.gameObject;
        if (triggerZone != null)
        {
            Destroy(triggerZone);
        }
        
        // Le NPC reste actif et peut continuer à être interactif pour d'autres dialogues
        Debug.Log($"[TALK] NPC {objectName} nettoyé mais toujours présent dans le monde");
    }
    
    // Nettoyage spécial pour les quêtes DELIVERY
    void CleanupDeliveryQuest(ActiveQuest quest)
    {
        // Retire la quête de la liste active
        QuestManager.Instance.activeQuests.Remove(quest);
        
        // NE PAS détruire le NPC de livraison
        Debug.Log($"[DELIVERY] Quête terminée - NPC {objectName} reste en place");
        
        // Détruit l'affichage du nom créé par QuestObject
        if (nameDisplay != null)
        {
            Destroy(nameDisplay);
            nameDisplay = null;
            nameText = null;
        }
        
        // Retire l'effet glow
        if (objectRenderer != null)
        {
            // Désactive l'émission
            objectRenderer.material.DisableKeyword("_EMISSION");
            objectRenderer.material.SetColor("_EmissionColor", Color.black);
            
            // Restaure la couleur normale du material
            objectRenderer.material.color = Color.white;
        }
        
        // Désactive le trigger de quête
        GameObject triggerZone = transform.Find("QuestTriggerZone")?.gameObject;
        if (triggerZone != null)
        {
            Destroy(triggerZone);
        }
        
        // Désactive le flag de livraison
        isDeliveryTarget = false;
        isCollected = true; // Empêche toute interaction future
    }
    
    void ExploreMarker()
    {
        if (isCollected) return; // Évite la double validation
        
        isCollected = true;
        Debug.Log($"🗺️ ZONE EXPLORÉE AVEC SUCCÈS: {objectName}");
        Debug.Log($"🎆 Quête {questId} - Marqueur validé !");
        
        // Notifie le QuestManager
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnMarkerExplored(questId, objectName);
        }
        else
        {
            Debug.LogError("❌ QuestManager.Instance est NULL !");
        }
        
        // Effet visuel de validation
        if (nameText != null)
        {
            nameText.text = "✅ Zone Explorée !";
            nameText.color = Color.green;
            nameText.fontSize = fontSize * 1.5f;
        }
        
        // Effet de particules
        var footstepSystem = FindObjectOfType<FootstepSystem>();
        if (footstepSystem != null)
        {
            for (int i = 0; i < 3; i++)
            {
                footstepSystem.PlayLandingParticles();
            }
        }
        
        // Destruction progressive
        StartCoroutine(ExplorationCompleteEffect());
    }

	System.Collections.IEnumerator ExplorationCompleteEffect()
	{
	    yield return new WaitForSeconds(2f); // Laisse le message visible
	    
	    // Puis fait disparaître progressivement
	    float fadeTime = 1f;
	    float elapsed = 0f;
	    
	    while (elapsed < fadeTime)
	    {
	        elapsed += Time.deltaTime;
	        float alpha = 1f - (elapsed / fadeTime);
	        
	        if (objectRenderer != null)
	        {
	            Color color = objectRenderer.material.color;
	            color.a = alpha;
	            objectRenderer.material.color = color;
	        }
	        
	        if (nameText != null)
	        {
	            Color textColor = nameText.color;
	            textColor.a = alpha;
	            nameText.color = textColor;
	        }
	        
	        yield return null;
	    }
	    
	    Destroy(gameObject);
	}
    
    // TRIGGER EVENTS avec debug amélioré
    void OnTriggerEnter(Collider other)
    {
        if (debugMode)
            Debug.Log($"🔍 OnTriggerEnter - Objet détecté: {other.name} (Tag: {other.tag})");
        
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            // Pour les marqueurs d'exploration, on commence le timer
            if (objectType == QuestObjectType.Marker && !isCollected)
            {
                Debug.Log($"📍 ENTRÉ DANS LA ZONE D'EXPLORATION : {objectName}");
                Debug.Log($"🕐 Restez {explorationTimeRequired} secondes pour valider l'exploration");
            }
            else
            {
                // Pour les autres types, affiche le prompt
                ShowInteractionPrompt(true);
                Debug.Log($"✅ JOUEUR DÉTECTÉ près de {objectName} - Appuyez sur E pour interagir !");
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            ShowInteractionPrompt(false);
            
            // NOUVEAU : Reset le timer d'exploration si on quitte la zone
            if (objectType == QuestObjectType.Marker && isExploring && !isCollected)
            {
                isExploring = false;
                explorationTimer = 0f;
                Debug.Log($"📤 Exploration interrompue pour {objectName}");
                
                if (nameText != null)
                {
                    nameText.text = GetDisplayText();
                    nameText.color = GetTextColor();
                    nameText.fontSize = fontSize;
                }
            }
        }
    }
    
    void ShowInteractionPrompt(bool show)
    {
        if (show && !isCollected)
        {
            string action = GetActionText();
            Debug.Log($"💡💡💡 APPUYEZ SUR E POUR {action.ToUpper()} 💡💡💡");
            
            // Affichage dans le nameText de l'objet
            if (nameText != null)
            {
                nameText.text = $"{GetDisplayText()}\n[E] {action}";
                nameText.color = Color.white;
                // Augmente légèrement la taille pour plus de visibilité
                nameText.fontSize = fontSize * 1.2f;
            }
        }
        else if (nameText != null)
        {
            // Remet le texte normal
            nameText.text = GetDisplayText();
            nameText.color = GetTextColor();
            nameText.fontSize = fontSize;
        }
    }
    
    string GetActionText()
    {
        switch (objectType)
        {
            case QuestObjectType.Item:
                return "Ramasser";
            case QuestObjectType.InteractableObject:
                return "Interagir";
            case QuestObjectType.Marker:
                return "Explorer";
            case QuestObjectType.NPC:
                return "Parler";
            default:
                return "Interagir";
        }
    }
    
    // GIZMOS améliorés
    void OnDrawGizmos()
    {
        // Gizmos toujours visibles (pas seulement quand sélectionné)
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        // Centre de l'objet
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
    }
    
    void OnDrawGizmosSelected()
    {
        // Gizmos détaillés quand sélectionné
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, triggerRadius);
        
        // Affiche le nom au-dessus
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, Vector3.up * 3f);
    }

    [ContextMenu("Debug AI Fields")]
    public void DebugAIFields()
    {
        Debug.Log($"=== AI FIELDS for {gameObject.name} ===");
        Debug.Log($"Object Name: {objectName}");
        Debug.Log($"Object Type: {objectType}");
        Debug.Log("=====================================");
    }
}