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

    [Header("Treasure Settings")]
    [Tooltip("Marker en mode trésor : nécessite d'appuyer sur E pour creuser.")]
    public bool isTreasure = false;
    [Tooltip("Temps de creusement (secondes) une fois E pressé.")]
    public float digDuration = 2.5f;
    private bool isDigging = false;
    private float digTimer = 0f;

    [Header("Rendez-vous (heure requise)")]
    [Tooltip("Si >= 0 : la quête EXPLORE / TREASURE ne progresse QUE si l'heure " +
        "in-game correspond. Permet de coder un RDV (ex. requiredHour = 0 = minuit).")]
    public int requiredHour = -1;
    [Tooltip("Tolérance en heures autour de requiredHour (ex. 1 = entre H-1 et H+1).")]
    public int requiredHourTolerance = 1;
    
    // Private variables
    private bool playerInRange = false;
    private Renderer objectRenderer;
    private GameObject nameDisplay;
    private TextMeshPro nameText;
    private Camera mainCamera;
    private SphereCollider triggerCollider;
    
    void Awake()
    {
        // Pour les marqueurs d'exploration, positionne au sol dès que possible
        if (objectType == QuestObjectType.Marker)
        {
            Debug.Log($"🎯 [MARKER AWAKE] Positionnement précoce du marqueur {objectName}");
            PositionMarkerOnGround();
        }
    }
    
    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        mainCamera = Camera.main;
        
        if (debugMode)
            Debug.Log($"🔧 QuestObject Start() - {objectName} ({objectType})");
        
        // Pour les marqueurs d'exploration, s'assure qu'ils sont au sol
        if (objectType == QuestObjectType.Marker)
        {
            PositionMarkerOnGround();
        }
        
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
    
    void PositionMarkerOnGround()
    {
        Vector3 currentPos = transform.position;
        Debug.Log($"🎯 [MARKER GROUND CHECK] Position initiale: {currentPos}");
        
        Vector3 rayStart = new Vector3(currentPos.x, currentPos.y + 50f, currentPos.z);
        
        // Layer mask pour ignorer les triggers et zones
        // On veut seulement les objets solides (terrain, bâtiments, etc.)
        int layerMask = ~0; // Commence avec tous les layers
        
        // Ignore les layers communs pour les triggers
        // NOTE: Ajustez ces layers selon votre projet
        int ignoreLayer1 = LayerMask.NameToLayer("Ignore Raycast");
        int ignoreLayer2 = LayerMask.NameToLayer("UI");
        int ignoreLayer3 = LayerMask.NameToLayer("TransparentFX");
        
        if (ignoreLayer1 >= 0) layerMask &= ~(1 << ignoreLayer1);
        if (ignoreLayer2 >= 0) layerMask &= ~(1 << ignoreLayer2);
        if (ignoreLayer3 >= 0) layerMask &= ~(1 << ignoreLayer3);
        
        // Alternative: utiliser QueryTriggerInteraction pour ignorer TOUS les triggers
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 100f, layerMask, QueryTriggerInteraction.Ignore))
        {
            // Vérifie que c'est bien un sol valide et pas un trigger qui serait passé
            if (!hit.collider.isTrigger)
            {
                transform.position = hit.point + Vector3.up * 0.1f;
                Debug.Log($"🎯 [MARKER GROUNDED] Repositionné au sol: {hit.point} (hit: {hit.collider.name}, layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                return;
            }
            else
            {
                Debug.LogWarning($"🎯 [MARKER] Hit un trigger! {hit.collider.name} - on continue la recherche");
            }
        }
        
        Debug.LogWarning($"🎯 [MARKER GROUND CHECK] Premier raycast échoué depuis Y={rayStart.y}");
        
        // Deuxième essai depuis plus bas
        rayStart = currentPos + Vector3.up * 2f;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.isTrigger)
            {
                transform.position = hit.point + Vector3.up * 0.1f;
                Debug.Log($"🎯 [MARKER GROUNDED] Repositionné au sol (2e essai): {hit.point} (hit: {hit.collider.name})");
                return;
            }
        }
        
        // Troisième essai: RaycastAll pour trouver le VRAI sol
        Debug.Log($"🎯 [MARKER] Essai avec RaycastAll pour ignorer les triggers...");
        rayStart = new Vector3(currentPos.x, currentPos.y + 50f, currentPos.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 100f, layerMask);
        
        // Trie par distance et trouve le premier non-trigger
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        foreach (RaycastHit hitInfo in hits)
        {
            if (!hitInfo.collider.isTrigger)
            {
                transform.position = hitInfo.point + Vector3.up * 0.1f;
                Debug.Log($"🎯 [MARKER GROUNDED via RaycastAll] Sol trouvé: {hitInfo.point} (hit: {hitInfo.collider.name})");
                return;
            }
            else
            {
                Debug.Log($"🎯 [MARKER] RaycastAll - Ignoré trigger: {hitInfo.collider.name}");
            }
        }
        
        // Fallback final
        transform.position = new Vector3(currentPos.x, 0f, currentPos.z);
        Debug.LogWarning($"🎯 [MARKER FALLBACK] Aucun sol non-trigger trouvé! Position à y=0");
        
        Debug.Log($"🎯 [MARKER FINAL] Position finale: {transform.position}");
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
                return $"[ITEM] {formattedName}";
            case QuestObjectType.NPC:
                // Pour les NPCs de livraison, affiche "Livrer à [nom]"
                if (isDeliveryTarget)
                    return $"[DELIVER TO] {formattedName}";
                else
                    return $"[NPC] {formattedName}";
            case QuestObjectType.InteractableObject:
                return $"[INTERACT] {formattedName}";
            case QuestObjectType.Marker:
                return $"[EXPLORE] {formattedName}";
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
        
        // (L'interaction E est désormais gérée plus bas, gated sur le type :
        // les Markers ont leur propre logique — EXPLORE auto / TREASURE dig.)

        // Effet de pulsation
        if (objectRenderer != null && !isCollected)
        {
            float pulse = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
            objectRenderer.material.SetColor("_EmissionColor", glowColor * pulse * 0.5f);
        }
        
        // Markers : deux modes — EXPLORE auto-progressif, TREASURE à creuser.
        if (objectType == QuestObjectType.Marker && playerInRange && !isCollected)
        {
            if (isTreasure)
            {
                UpdateTreasureDig();
            }
            else
            {
                UpdateExploreProgress();
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
    
    bool IsWithinRequiredHour()
    {
        if (requiredHour < 0) return true;
        if (GameClock.Instance == null) return true;
        int h = GameClock.Instance.Hour;
        int dist = Mathf.Min(
            Mathf.Abs(h - requiredHour),
            24 - Mathf.Abs(h - requiredHour));
        return dist <= requiredHourTolerance;
    }

    void UpdateExploreProgress()
    {
        // Contrainte horaire : si on est hors fenêtre, on bloque la progression
        // et on l'indique au joueur. La quête reste là, il pourra revenir.
        if (!IsWithinRequiredHour())
        {
            if (nameText != null)
            {
                nameText.text = $"Revenez vers {requiredHour:00}h\n[{TextFormatter.FormatName(objectName)}]";
                nameText.color = new Color(0.8f, 0.7f, 1f);
                nameText.fontSize = fontSize * 1.2f;
            }
            return;
        }

        if (!isExploring)
        {
            isExploring = true;
            explorationTimer = 0f;
            if (debugMode) Debug.Log($"🗺️ Début exploration de {objectName}...");
        }

        explorationTimer += Time.deltaTime;

        if (nameText != null)
        {
            float progress = Mathf.Clamp01(explorationTimer / explorationTimeRequired);
            string formattedName = TextFormatter.FormatName(objectName);
            nameText.text = $"[EXPLORE] {formattedName}\nProgress: {Mathf.RoundToInt(progress * 100)}%";
            nameText.color = Color.Lerp(Color.yellow, Color.green, progress);
            nameText.fontSize = fontSize * 1.3f;
        }

        if (explorationTimer >= explorationTimeRequired)
        {
            ExploreMarker();
        }
    }

    void UpdateTreasureDig()
    {
        string formattedName = TextFormatter.FormatName(objectName);

        // Phase 1 : pas encore commencé à creuser → prompt 'E pour creuser'.
        if (!isDigging)
        {
            if (nameText != null)
            {
                nameText.text = $"{formattedName}\nAppuyer sur E pour creuser";
                nameText.color = Color.yellow;
                nameText.fontSize = fontSize * 1.2f;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                isDigging = true;
                digTimer = 0f;
                if (debugMode) Debug.Log($"⛏️ Début du creusement : {objectName}");
            }
            return;
        }

        // Phase 2 : creusement en cours, on freine le joueur par retour visuel.
        digTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(digTimer / digDuration);

        if (nameText != null)
        {
            int percent = Mathf.RoundToInt(progress * 100);
            // Petite barre ASCII pour le feedback.
            int filled = Mathf.RoundToInt(progress * 10);
            string bar = new string('█', filled) + new string('░', 10 - filled);
            nameText.text = $"Creusement... {percent}%\n[{bar}]";
            nameText.color = Color.Lerp(new Color(0.9f, 0.7f, 0.2f), Color.green, progress);
            nameText.fontSize = fontSize * 1.3f;
        }

        // Visuel : le marker s'enfonce progressivement sous le sol.
        if (objectRenderer != null)
        {
            Vector3 pos = transform.position;
            // On garde un offset relatif (sink jusqu'à -0.5 sous la position d'origine).
            // L'offset est appliqué via material, pas la transform, pour ne pas
            // perturber les colliders ; ici on fait un léger scale-down.
            float s = Mathf.Lerp(1f, 0.4f, progress);
            transform.localScale = new Vector3(s, Mathf.Lerp(1f, 0.2f, progress), s);
        }

        if (digTimer >= digDuration)
        {
            if (debugMode) Debug.Log($"⛏️ TRÉSOR DÉTERRÉ : {objectName}");
            ExploreMarker();
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

        // Log la collecte de l'objet dans le Journal d'Aventure
        AdventureJournalExtensions.LogItemCollected(objectName);
        
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
            nameText.text = "[ACTIVE]";
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
                nameText.text = $"[DELIVERED] {packageName}";
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
                nameText.text = $"[NEED] {packageName}";
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
        
        // NOUVEAU : Met à jour la quête suivie
        if (QuestJournal.Instance != null)
        {
            QuestJournal.Instance.UpdateTrackedQuestAfterCompletion(quest.questId);
        }
        
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
            nameText.text = "[EXPLORED]";
            nameText.color = Color.green;
            nameText.fontSize = fontSize * 1.5f;
        }
        
        // Effet de particules
        var footstepSystem = FindFirstObjectByType<FootstepSystem>();
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
            
            // Reset le timer d'exploration / creusement si on quitte la zone.
            if (objectType == QuestObjectType.Marker && !isCollected)
            {
                if (isExploring)
                {
                    isExploring = false;
                    explorationTimer = 0f;
                    if (debugMode) Debug.Log($"📤 Exploration interrompue pour {objectName}");
                }
                if (isDigging)
                {
                    isDigging = false;
                    digTimer = 0f;
                    // Restaure l'échelle d'origine.
                    transform.localScale = Vector3.one;
                    if (debugMode) Debug.Log($"📤 Creusement interrompu pour {objectName}");
                }

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