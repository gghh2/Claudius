using UnityEngine;
using TMPro;

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
        // Retire tous les colliders existants pour éviter les conflits
        Collider[] existingColliders = GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            if (debugMode)
                Debug.Log($"🗑️ Suppression collider existant: {col.GetType().Name}");
            DestroyImmediate(col);
        }
        
        // Crée un nouveau trigger propre
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;
        
        if (debugMode)
            Debug.Log($"✅ Nouveau trigger créé - Radius: {triggerRadius}, IsTrigger: {triggerCollider.isTrigger}");
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
        switch (objectType)
        {
            case QuestObjectType.Item:
                return $"📦 {objectName}";
            case QuestObjectType.NPC:
                return $"👤 {objectName}";
            case QuestObjectType.InteractableObject:
                return $"🔧 {objectName}";
            case QuestObjectType.Marker:
                return $"📍 Explorer: {objectName}";
            default:
                return objectName;
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
        // Billboard effect
        if (nameDisplay != null && mainCamera != null)
        {
            nameDisplay.transform.LookAt(mainCamera.transform);
            nameDisplay.transform.Rotate(0, 180, 0);
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
        
        // NOUVEAU: Debug status chaque seconde
        if (debugMode && Time.frameCount % 60 == 0) // Toutes les secondes environ
        {
            Debug.Log($"📊 {objectName} - PlayerInRange: {playerInRange}, IsCollected: {isCollected}");
        }


        // NOUVEAU : Timer d'exploration
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
                nameText.text = $"📍 Exploration: {Mathf.RoundToInt(progress * 100)}%";
            }
            
            // Validation automatique après le délai
            if (explorationTimer >= explorationTimeRequired)
            {
                Debug.Log($"🗺️ ZONE EXPLORÉE : {objectName}");
                ExploreMarker();
            }
        }
        
        // Interaction normale pour les autres types
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isCollected && objectType != QuestObjectType.Marker)
        {
            InteractWithObject();
        }


    }
    
    void InteractWithObject()
    {
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
	            ActivateObject(); // ← Changez TalkToNPC() par ActivateObject()
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
        
        StartCoroutine(ExplorationCompleteEffect());
    }
    
    void ActivateObject()
    {
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
    }
    
    void ExploreMarker()
	{
	    isCollected = true;
	    Debug.Log($"🗺️ Zone explorée: {objectName}");
	    
	    QuestManager.Instance?.OnMarkerExplored(questId, objectName);
	    
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
	        
	        // NOUVEAU : Auto-validation pour les marqueurs d'exploration
	        if (objectType == QuestObjectType.Marker && !isCollected)
	        {
	            Debug.Log($"🗺️ ZONE EXPLORÉE AUTOMATIQUEMENT : {objectName}");
	            ExploreMarker();
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
            
            // NOUVEAU: Affichage plus visible
            if (nameText != null)
            {
                nameText.text = $"{GetDisplayText()}\n[E] {action}";
                nameText.color = Color.white;
            }
        }
        else if (nameText != null)
        {
            // Remet le texte normal
            nameText.text = GetDisplayText();
            nameText.color = GetTextColor();
        }
    }
    
    string GetActionText()
    {
        switch (objectType)
        {
            case QuestObjectType.Item:
                return $"ramasser {objectName}";
            case QuestObjectType.InteractableObject:
                return $"interagir avec {objectName}";
            case QuestObjectType.Marker:
                return $"explorer {objectName}";
            case QuestObjectType.NPC:
                return $"parler à {objectName}";
            default:
                return $"interagir avec {objectName}";
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