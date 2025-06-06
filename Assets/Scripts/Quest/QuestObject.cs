using UnityEngine;
using TMPro;

public class QuestObject : MonoBehaviour
{
    [Header("Quest Object Info")]
    public string questId;
    public string objectName;
    public QuestObjectType objectType;
    public bool isCollected = false;
    
    [Header("Visual Settings")]
    public GameObject highlightEffect;
    public Color glowColor = Color.yellow;
    
    [Header("Name Display")]
    public Vector3 nameOffset = new Vector3(0, 1.5f, 0);
    public float fontSize = 3f;
    
    [Header("Interaction Settings")]
    public float triggerRadius = 2f;
    
    [Header("Debug")]
    public bool debugMode = true; // NOUVEAU: Debug activé par défaut
    
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
                TalkToNPC();
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
        
        StartCoroutine(CollectionEffect());
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
        StartCoroutine(CollectionEffect());
    }
    
    void TalkToNPC()
    {
        isCollected = true;
        Debug.Log($"💬 Conversation avec: {objectName}");
        
        QuestManager.Instance?.OnObjectInteracted(questId, objectName);
        
        if (nameText != null)
        {
            nameText.text = "✅ Mission accomplie";
            nameText.color = Color.green;
        }
    }
    
    System.Collections.IEnumerator CollectionEffect()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * 2f;
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            if (objectRenderer != null)
            {
                Color color = objectRenderer.material.color;
                color.a = 1f - t;
                objectRenderer.material.color = color;
            }
            
            if (nameText != null)
            {
                Color textColor = nameText.color;
                textColor.a = 1f - t;
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
            ShowInteractionPrompt(true);
            Debug.Log($"✅ JOUEUR DÉTECTÉ près de {objectName} - Appuyez sur E pour interagir !");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (debugMode)
            Debug.Log($"🔍 OnTriggerExit - Objet quitté: {other.name} (Tag: {other.tag})");
        
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            ShowInteractionPrompt(false);
            Debug.Log($"📤 Joueur s'éloigne de {objectName}");
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
}