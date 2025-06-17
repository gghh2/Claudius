using UnityEngine;
using System.Collections;

[System.Serializable]
public class CompanionSounds
{
    [Header("Sound Settings")]
    public AudioClip[] idleSounds;
    public AudioClip[] moveSounds;
    public AudioClip[] happySounds;
    
    [Range(0f, 1f)]
    public float soundVolume = 0.3f;
    
    [Header("Sound Intervals")]
    public float minSoundInterval = 4f;
    public float maxSoundInterval = 8f;
}

[System.Serializable]
public class CompanionAnimations
{
    [Header("Animation Names")]
    public string idleAnimation = "Idle";
    public string moveAnimation = "Jump";
    public string happyAnimation = "Jump"; // Peut être le même que move
    
    [Header("Animation Parameters (if using Animator)")]
    public string isMovingParam = "IsMoving";
    public string speedParam = "Speed";
    public string jumpTrigger = "Jump";
}

public class CompanionController : MonoBehaviour
{
    [Header("== Companion Settings ==")]
    [Tooltip("Le prefab du compagnon à utiliser")]
    public GameObject companionPrefab;
    
    [Tooltip("Offset par rapport au sol")]
    public float groundOffset = 0f;
    
    [Header("== Following Behavior ==")]
    [Tooltip("Distance minimale avant de commencer à suivre")]
    public float followDistance = 3f;
    
    [Tooltip("Distance à maintenir du joueur")]
    public float stoppingDistance = 1.5f;
    
    [Tooltip("Vitesse de déplacement (même échelle que PlayerController)")]
    public float moveSpeed = 5f; // Même valeur par défaut que le joueur
    
    [Tooltip("Multiplicateur de vitesse par rapport au joueur (1 = même vitesse)")]
    [Range(0.5f, 2f)]
    public float speedMultiplier = 0.9f; // Légèrement plus lent pour rester derrière
    
    [Tooltip("Vitesse de rotation")]
    public float rotationSpeed = 10f;
    
    [Header("== Movement Style ==")]
    [Tooltip("Type de mouvement du compagnon")]
    public MovementType movementType = MovementType.Hopping;
    
    public enum MovementType
    {
        Continuous,      // Marche normale
        Hopping,         // Sautille (par code)
        AnimationDriven  // Mouvement basé sur l'animation
    }
    
    [Header("Hopping Settings (if Hopping)")]
    [Tooltip("Hauteur du saut")]
    public float hopHeight = 0.5f;
    
    [Tooltip("Durée d'un saut")]
    public float hopDuration = 0.5f;
    
    [Tooltip("Temps entre les sauts")]
    public float hopInterval = 0.2f;
    
    [Header("== Idle Behavior ==")]
    [Tooltip("Temps avant de commencer à se promener")]
    public float idleTimeBeforeWander = 3f;
    
    [Tooltip("Rayon de déplacement aléatoire")]
    public float wanderRadius = 2f;
    
    [Tooltip("Comportements idle personnalisés")]
    public bool enableIdleAnimations = true;
    
    [Header("== Sounds ==")]
    public CompanionSounds sounds;
    
    [Header("== Animations ==")]
    public CompanionAnimations animations;
    
    [Header("== References ==")]
    [Tooltip("Référence au joueur (auto-détecté si vide)")]
    public Transform player;
    
    // Debug est maintenant géré par GlobalDebugManager
    
    // Components
    private Rigidbody rb;
    private Animator animator;
    private Animation legacyAnimation;
    private AudioSource audioSource;
    private PlayerControllerCC playerController; // Référence au joueur
    
    // State
    private bool isMoving = false;
    private float lastSoundTime;
    private float nextSoundInterval;
    private float idleTimer = 0f;
    private Vector3 wanderTarget;
    private GameObject spawnedCompanion;
    
    // États
    private enum CompanionState
    {
        Following,
        Idle,
        Wandering,
        Happy
    }
    private CompanionState currentState = CompanionState.Idle;
    
    void Start()
    {
        InitializeCompanion();
    }
    
    void InitializeCompanion()
    {
        // Spawn le compagnon si un prefab est assigné
        if (companionPrefab != null && spawnedCompanion == null)
        {
            spawnedCompanion = Instantiate(companionPrefab, transform);
            spawnedCompanion.transform.localPosition = Vector3.up * groundOffset;
            
            // Propage le tag du parent au clone spawné
            string parentTag = gameObject.tag;
            
            if (!string.IsNullOrEmpty(parentTag) && parentTag != "Untagged")
            {
                // Applique le tag au clone principal
                spawnedCompanion.tag = parentTag;
                
                // Applique aussi le tag à tous les enfants qui ont un Renderer
                Renderer[] renderers = spawnedCompanion.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.gameObject.tag = parentTag;
                }
            }
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Companion))
                Debug.Log($"✅ Compagnon spawné: {companionPrefab.name}");
        }
        
        // Configure Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = 1f;
        rb.drag = 5f;
        rb.angularDrag = 10f;
        rb.freezeRotation = true;
        
        // Configure Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = sounds.soundVolume;
        
        // Trouve les composants d'animation
        animator = GetComponentInChildren<Animator>();
        legacyAnimation = GetComponentInChildren<Animation>();
        
        // Trouve le joueur
        if (player == null)
        {
            playerController = FindObjectOfType<PlayerControllerCC>();
            if (playerController != null)
            {
                player = playerController.transform;
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Companion))
                    Debug.Log("✅ Joueur trouvé automatiquement");
                
                // Synchronise la vitesse avec le joueur
                SyncSpeedWithPlayer();
            }
            else
            {
                Debug.LogError("❌ Aucun joueur trouvé!");
            }
        }
        else
        {
            // Si le joueur est assigné manuellement, trouve quand même le PlayerController
            playerController = player.GetComponent<PlayerControllerCC>();
            if (playerController != null)
            {
                SyncSpeedWithPlayer();
            }
        }
        
        // Initialise les sons
        nextSoundInterval = Random.Range(sounds.minSoundInterval, sounds.maxSoundInterval);
        lastSoundTime = Time.time;
    }
    
    void Update()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Détermine l'état
        UpdateState(distanceToPlayer);
        
        // Comportement selon l'état
        switch (currentState)
        {
            case CompanionState.Following:
                HandleFollowing(distanceToPlayer);
                break;
                
            case CompanionState.Idle:
                HandleIdle();
                break;
                
            case CompanionState.Wandering:
                HandleWandering();
                break;
                
            case CompanionState.Happy:
                HandleHappy();
                break;
        }
        
        // Gestion des sons
        HandleSounds();
        
        // Mise à jour des animations
        UpdateAnimations();
        
        // Debug de l'animator
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Companion) && Input.GetKeyDown(KeyCode.F8))
        {
            DebugAnimatorInfo();
        }
    }
    
    void UpdateState(float distanceToPlayer)
    {
        if (distanceToPlayer > followDistance)
        {
            if (currentState != CompanionState.Following)
            {
                currentState = CompanionState.Following;
                idleTimer = 0f;
            }
        }
        else if (distanceToPlayer <= stoppingDistance)
        {
            if (currentState == CompanionState.Following)
            {
                currentState = CompanionState.Idle;
                idleTimer = 0f;
            }
        }
    }
    
    void HandleFollowing(float distanceToPlayer)
    {
        if (distanceToPlayer > stoppingDistance)
        {
            if ((movementType == MovementType.Hopping || movementType == MovementType.AnimationDriven) && !isMoving)
            {
                StartCoroutine(HopTowards(player.position));
            }
            else if (movementType == MovementType.Continuous)
            {
                MoveTowards(player.position);
            }
        }
    }
    
    void HandleIdle()
    {
        idleTimer += Time.deltaTime;
        
        if (idleTimer > idleTimeBeforeWander)
        {
            currentState = CompanionState.Wandering;
            SetNewWanderTarget();
            idleTimer = 0f;
        }
        
        // Animations idle aléatoires
        if (enableIdleAnimations && Random.Range(0f, 1f) < 0.005f)
        {
            PlayIdleAnimation();
        }
    }
    
    void HandleWandering()
    {
        float distanceToTarget = Vector3.Distance(transform.position, wanderTarget);
        
        if (distanceToTarget > 0.5f)
        {
            if ((movementType == MovementType.Hopping || movementType == MovementType.AnimationDriven) && !isMoving)
            {
                StartCoroutine(HopTowards(wanderTarget));
            }
            else if (movementType == MovementType.Continuous)
            {
                MoveTowards(wanderTarget);
            }
        }
        else
        {
            currentState = CompanionState.Idle;
            idleTimer = 0f;
        }
    }
    
    void HandleHappy()
    {
        // État temporaire pour les animations de joie
        StartCoroutine(HappyAnimation());
    }
    
    void SetNewWanderTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle * wanderRadius;
        wanderTarget = transform.position + new Vector3(randomDirection.x, 0, randomDirection.y);
        
        // S'assure de ne pas trop s'éloigner du joueur
        if (Vector3.Distance(wanderTarget, player.position) > followDistance * 1.5f)
        {
            wanderTarget = transform.position;
        }
    }
    
    void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Reste sur le plan horizontal
        
        // Déplacement avec vitesse effective
        float currentSpeed = GetEffectiveSpeed();
        Vector3 newPosition = transform.position + direction * currentSpeed * Time.deltaTime;
        rb.MovePosition(newPosition);
        
        // Rotation
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        isMoving = true;
    }
    
    IEnumerator HopTowards(Vector3 targetPosition)
    {
        isMoving = true;
        
        Vector3 startPos = transform.position;
        Vector3 direction = (targetPosition - startPos).normalized;
        direction.y = 0;
        
        // Rotation vers la cible
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Play jump animation
        PlayAnimation(animations.moveAnimation);
        if (animator != null && !string.IsNullOrEmpty(animations.jumpTrigger))
        {
            animator.SetTrigger(animations.jumpTrigger);
        }
        
        if (movementType == MovementType.AnimationDriven)
        {
            // Laisse l'animation gérer le mouvement
            // Déplace juste horizontalement
            float moveDuration = hopDuration;
            float elapsed = 0f;
            
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                
                // Déplacement horizontal seulement avec vitesse effective
                float currentSpeed = GetEffectiveSpeed();
                Vector3 newPos = transform.position + direction * currentSpeed * Time.deltaTime;
                newPos.y = transform.position.y; // Garde la hauteur actuelle
                rb.MovePosition(newPos);
                
                // Continue la rotation
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                yield return null;
            }
        }
        else
        {
            // Ancien système de saut par code
            float currentSpeed = GetEffectiveSpeed();
            float hopDistance = Mathf.Min(currentSpeed * hopDuration, Vector3.Distance(startPos, targetPosition));
            Vector3 endPos = startPos + direction * hopDistance;
            
            float elapsed = 0f;
            while (elapsed < hopDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / hopDuration;
                
                // Position horizontale
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                
                // Hauteur parabolique
                float height = Mathf.Sin(t * Mathf.PI) * hopHeight;
                currentPos.y = startPos.y + height;
                
                rb.MovePosition(currentPos);
                
                yield return null;
            }
        }
        
        // Son d'atterrissage
        PlayMoveSound();
        
        // Pause entre les sauts
        yield return new WaitForSeconds(hopInterval);
        
        isMoving = false;
    }
    
    void HandleSounds()
    {
        if (Time.time - lastSoundTime > nextSoundInterval)
        {
            PlayRandomIdleSound();
            lastSoundTime = Time.time;
            nextSoundInterval = Random.Range(sounds.minSoundInterval, sounds.maxSoundInterval);
        }
    }
    
    void PlayRandomIdleSound()
    {
        if (sounds.idleSounds != null && sounds.idleSounds.Length > 0 && Random.Range(0f, 1f) < 0.5f)
        {
            AudioClip randomSound = sounds.idleSounds[Random.Range(0, sounds.idleSounds.Length)];
            if (randomSound != null)
            {
                audioSource.PlayOneShot(randomSound, sounds.soundVolume);
            }
        }
    }
    
    void PlayMoveSound()
    {
        if (sounds.moveSounds != null && sounds.moveSounds.Length > 0)
        {
            AudioClip randomSound = sounds.moveSounds[Random.Range(0, sounds.moveSounds.Length)];
            if (randomSound != null)
            {
                audioSource.PlayOneShot(randomSound, sounds.soundVolume * 0.7f);
            }
        }
    }
    
    void PlayHappySound()
    {
        if (sounds.happySounds != null && sounds.happySounds.Length > 0)
        {
            AudioClip randomSound = sounds.happySounds[Random.Range(0, sounds.happySounds.Length)];
            if (randomSound != null)
            {
                audioSource.PlayOneShot(randomSound, sounds.soundVolume);
            }
        }
    }
    
    void UpdateAnimations()
    {
        // Pour Animator (Mecanim)
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(animations.isMovingParam))
            {
                animator.SetBool(animations.isMovingParam, isMoving);
            }
            
            if (!string.IsNullOrEmpty(animations.speedParam))
            {
                float currentSpeed = GetEffectiveSpeed();
                animator.SetFloat(animations.speedParam, isMoving ? currentSpeed : 0f);
            }
        }
        
        // Pour Legacy Animation
        if (legacyAnimation != null && !isMoving)
        {
            if (!legacyAnimation.isPlaying)
            {
                PlayAnimation(animations.idleAnimation);
            }
        }
    }
    
    void PlayAnimation(string animationName)
    {
        if (string.IsNullOrEmpty(animationName)) return;
        
        if (legacyAnimation != null && legacyAnimation[animationName] != null)
        {
            legacyAnimation.Play(animationName);
        }
    }
    
    void PlayIdleAnimation()
    {
        PlayAnimation(animations.idleAnimation);
    }
    
    IEnumerator HappyAnimation()
    {
        currentState = CompanionState.Happy;
        
        PlayAnimation(animations.happyAnimation);
        PlayHappySound();
        
        // Petit saut de joie
        if (movementType == MovementType.Hopping)
        {
            Vector3 startPos = transform.position;
            float jumpHeight = hopHeight * 1.5f;
            float jumpDuration = 0.4f;
            
            float elapsed = 0f;
            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / jumpDuration;
                
                float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                transform.position = new Vector3(startPos.x, startPos.y + height, startPos.z);
                
                // Rotation
                transform.Rotate(Vector3.up, 360f * Time.deltaTime);
                
                yield return null;
            }
            
            transform.position = startPos;
        }
        
        yield return new WaitForSeconds(1f);
        
        currentState = CompanionState.Idle;
    }
    
    // Méthodes de synchronisation de vitesse
    void SyncSpeedWithPlayer()
    {
        if (playerController != null)
        {
            // Synchronise la vitesse de base avec celle du joueur
            float playerBaseSpeed = playerController.moveSpeed;
            moveSpeed = playerBaseSpeed * speedMultiplier;
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Companion))
            {
                Debug.Log($"🏃 Vitesse synchronisée avec le joueur:");
                Debug.Log($"  - Vitesse du joueur: {playerBaseSpeed} m/s");
                Debug.Log($"  - Multiplicateur: {speedMultiplier}x");
                Debug.Log($"  - Vitesse du compagnon: {moveSpeed} m/s");
            }
        }
    }
    
    // Méthode publique pour ajuster la vitesse en temps réel
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.5f, 2f);
        SyncSpeedWithPlayer();
    }
    
    // Obtient la vitesse effective (utile pour le sprint)
    float GetEffectiveSpeed()
    {
        if (playerController != null && playerController.IsSprinting())
        {
            // Si le joueur sprinte, le compagnon accélère aussi
            return moveSpeed * (playerController.sprintSpeed / playerController.moveSpeed);
        }
        return moveSpeed;
    }
    
    // Interaction avec le joueur
    public void OnPlayerInteract()
    {
        if (currentState != CompanionState.Happy)
        {
            currentState = CompanionState.Happy;
        }
    }
    
    // Appelé quand le joueur s'approche
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && Random.Range(0f, 1f) < 0.3f)
        {
            OnPlayerInteract();
        }
    }
    
    void DebugAnimatorInfo()
    {
        Debug.Log("=== ANIMATOR DEBUG ===");
        
        if (animator != null)
        {
            Debug.Log("🎮 Utilise Animator Controller (Mecanim)");
            Debug.Log($"Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NONE")}");
            
            // Liste les paramètres
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"  Param: {param.name} ({param.type})");
            }
            
            // État actuel
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Current State: {stateInfo.shortNameHash}");
        }
        else if (legacyAnimation != null)
        {
            Debug.Log("🎮 Utilise Legacy Animation");
            Debug.Log($"Clips disponibles:");
            foreach (AnimationState state in legacyAnimation)
            {
                Debug.Log($"  - {state.name}");
            }
            Debug.Log($"Is Playing: {legacyAnimation.isPlaying}");
        }
        else
        {
            Debug.LogWarning("❌ Aucun système d'animation trouvé!");
        }
    }
    
    // Debug Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        
        if (currentState == CompanionState.Wandering)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, wanderTarget);
            Gizmos.DrawSphere(wanderTarget, 0.2f);
        }
    }
    
    /// <summary>
    /// Force la propagation du tag au compagnon spawné
    /// </summary>
    [ContextMenu("Force Tag Propagation")]
    public void ForceTagPropagation()
    {
        if (spawnedCompanion == null)
        {
            Debug.LogWarning("Pas de compagnon spawné pour propager le tag");
            return;
        }
        
        string parentTag = gameObject.tag;
        
        if (!string.IsNullOrEmpty(parentTag) && parentTag != "Untagged")
        {
            // Applique au spawn principal
            spawnedCompanion.tag = parentTag;
            
            // Applique à tous les enfants avec Renderer
            Renderer[] renderers = spawnedCompanion.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.gameObject.tag = parentTag;
            }
            
            Debug.Log($"✅ Tag '{parentTag}' propagé au compagnon et ses enfants");
        }
        else
        {
            Debug.LogError($"❌ Le parent n'a pas de tag valide. Assignez le tag 'Companion' au GameObject parent!");
        }
    }
}
