using UnityEngine;

/// <summary>
/// Version du PlayerController utilisant le Character Controller d'Unity
/// Remplace le Rigidbody par un Character Controller pour un meilleur contrôle
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerControllerCC : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    
    [Header("Sprint")]
    public float sprintSpeed = 8f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    [Range(0.1f, 1f)]
    public float sprintTransitionSpeed = 0.3f;
    public bool canSprintInAir = false;
    
    [Header("Sprint Stamina (Optional)")]
    public bool useStamina = true;
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f; // Par seconde
    public float staminaRegenRate = 15f; // Par seconde
    public float staminaRegenDelay = 1f; // Délai avant régénération
    [HideInInspector]
    public float currentStamina; // Public pour l'UI mais caché dans l'Inspector
    private float staminaRegenTimer = 0f;
    
    [Header("Jump & Gravity")]
    public float jumpHeight = 2f; // Hauteur du saut en mètres
    public float gravity = -20f; // Force de gravité
    public LayerMask groundLayer = -1; // -1 = tous les layers par défaut
    [Tooltip("Distance de détection du sol pour le saut")]
    public float groundCheckDistance = 0.1f;
    
    [Header("Character Controller Step")]
    [Tooltip("Hauteur maximale des marches franchissables automatiquement")]
    [Range(0.0f, 1f)]
    public float stepOffset = 0.3f; // Le Character Controller gère ça nativement !
    [Tooltip("Angle maximum des pentes franchissables")]
    [Range(0f, 90f)]
    public float slopeLimit = 45f;
    
    [Header("Jump Effects")]
    public ParticleSystem jumpEffect;
    public AudioClip jumpSound;
    public AudioSource audioSource;
    
    [Header("Sprint Effects")]
    public ParticleSystem sprintEffect;
    public AudioClip sprintStartSound;
    public AudioClip sprintStopSound;
    
    [Header("Animation")]
    public Animator animator;
    
    [Header("Camera Effects (Optional)")]
    public CameraFollow cameraFollow;
    
    // Components
    private CharacterController controller;
    
    // Movement variables
    private Vector3 moveDirection;
    private Vector3 velocity;
    private bool isGrounded = false;
    private bool isSprinting = false;
    private bool canSprint = true;
    private float currentMoveSpeed;
    private float targetMoveSpeed;
    private bool isControlEnabled = true;
    
    // Input variables
    private float inputX;
    private float inputY;
    private float currentSpeed;
    
    // References
    private FootstepSystem footstepSystem;
    
    void Start()
    {
        // Get Character Controller
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
        
        // Configure Character Controller
        controller.stepOffset = stepOffset;
        controller.slopeLimit = slopeLimit;
        controller.skinWidth = 0.08f;
        
        // Initialize stamina
        currentStamina = maxStamina;
        currentMoveSpeed = moveSpeed;
        targetMoveSpeed = moveSpeed;
        
        FixModelPosition();
        
        // Get Animator
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                Debug.Log("🎭 Animator trouvé dans les enfants: " + (animator != null ? animator.name : "AUCUN"));
            }
        }
        
        // Verify Player tag
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
            Debug.Log("✅ Tag 'Player' assigné automatiquement");
        }
        
        // Configure AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Get footstep system
        footstepSystem = GetComponent<FootstepSystem>();
        if (footstepSystem == null)
        {
            Debug.LogWarning("⚠️ Aucun FootstepSystem trouvé sur le joueur");
        }
        
        // Get camera
        if (cameraFollow == null)
        {
            cameraFollow = FindObjectOfType<CameraFollow>();
        }
        
        Debug.Log("🎮 PlayerControllerCC (Character Controller) initialisé");
    }
    
    void Update()
    {
        // Always update stamina
        UpdateStamina();
        
        if (isControlEnabled && enabled)
        {
            // Ground check
            CheckGrounded();
            
            // Handle input
            HandleInput();
            
            // Update movement
            UpdateMovement();
            
            // Update sprint
            UpdateSprint();
            
            // Update animator
            UpdateAnimator();
            
            // Smooth speed transition
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, sprintTransitionSpeed);
        }
    }
    
    void HandleInput()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        
        // Keep raw input for other systems
        moveDirection = new Vector3(inputX, 0, inputY).normalized;
        
        currentSpeed = moveDirection.magnitude * currentMoveSpeed;
        
        // Sprint management
        bool sprintPressed = Input.GetKey(sprintKey);
        bool isMoving = moveDirection.magnitude > 0.1f;
        
        // Determine if we can sprint
        if (useStamina)
        {
            canSprint = currentStamina > 0f && (isGrounded || canSprintInAir) && isMoving;
        }
        else
        {
            canSprint = (isGrounded || canSprintInAir) && isMoving;
        }
        
        // Toggle sprint
        if (sprintPressed && canSprint)
        {
            if (!isSprinting)
            {
                StartSprint();
            }
        }
        else
        {
            if (isSprinting)
            {
                StopSprint();
            }
        }
        
        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }
    
    void UpdateMovement()
    {
        // Get camera for direction reference
        Camera mainCamera = Camera.main;
        
        // Calculate movement relative to camera view (important for isometric/orthographic cameras)
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        
        // Project onto horizontal plane
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        // Calculate movement direction
        Vector3 move = (right * inputX + forward * inputY) * currentMoveSpeed;
        
        // Apply horizontal movement
        controller.Move(move * Time.deltaTime);
        
        // Apply gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        
        // Rotation - face movement direction
        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }
    
    void CheckGrounded()
    {
        // Character Controller has built-in ground detection
        bool wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;
        
        // Additional ground check for more reliability
        if (!isGrounded)
        {
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.SphereCast(rayStart, controller.radius * 0.9f, Vector3.down, 
                out hit, groundCheckDistance + 0.1f, groundLayer);
        }
        
        // Landing detection
        if (!wasGrounded && isGrounded)
        {
            if (footstepSystem != null)
            {
                footstepSystem.ForceFootstep();
                footstepSystem.PlayLandingParticles();
            }
            
            // Reset vertical velocity on landing
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
        
        // Debug visualization
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            Debug.DrawRay(rayStart, Vector3.down * (groundCheckDistance + 0.1f), 
                isGrounded ? Color.green : Color.red);
        }
    }
    
    void Jump()
    {
        // Calculate jump velocity from desired height
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        
        if (animator != null)
        {
            SetAnimatorTriggerSafe("Jump");
        }
        
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepsEnabled(false);
            footstepSystem.PlayJumpParticles();
            Invoke(nameof(ReenableFootsteps), 0.3f);
        }
        
        if (jumpEffect != null)
            jumpEffect.Play();
        
        if (jumpSound != null && audioSource != null)
            audioSource.PlayOneShot(jumpSound);
        
        Debug.Log("🦘 Saut !");
    }
    
    void UpdateSprint()
    {
        if (isSprinting)
        {
            targetMoveSpeed = sprintSpeed;
            
            if (footstepSystem != null)
            {
                footstepSystem.stepInterval = Mathf.Lerp(footstepSystem.stepInterval, 0.3f, Time.deltaTime * 5f);
            }
            
            if (sprintEffect != null && !sprintEffect.isPlaying)
            {
                sprintEffect.Play();
            }
        }
        else
        {
            targetMoveSpeed = moveSpeed;
            
            if (footstepSystem != null)
            {
                footstepSystem.stepInterval = Mathf.Lerp(footstepSystem.stepInterval, 0.5f, Time.deltaTime * 5f);
            }
            
            if (sprintEffect != null && sprintEffect.isPlaying)
            {
                sprintEffect.Stop();
            }
        }
    }
    
    void UpdateStamina()
    {
        if (!useStamina) return;
        
        float deltaTime = Time.unscaledDeltaTime;
        
        if (isSprinting && isControlEnabled)
        {
            if (Time.timeScale > 0)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
                staminaRegenTimer = staminaRegenDelay;
            }
            
            if (currentStamina <= 0f)
            {
                StopSprint();
            }
        }
        else
        {
            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= deltaTime;
            }
            else
            {
                currentStamina += staminaRegenRate * deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            }
        }
    }
    
    void StartSprint()
    {
        isSprinting = true;
        
        Debug.Log("🏃 Sprint activé !");
        
        if (sprintStartSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sprintStartSound, 0.5f);
        }
        
        if (animator != null)
        {
            SetAnimatorBoolSafe("IsSprinting", true);
        }
    }
    
    void StopSprint()
    {
        isSprinting = false;
        
        Debug.Log("🚶 Sprint désactivé");
        
        if (sprintStopSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sprintStopSound, 0.3f);
        }
        
        if (animator != null)
        {
            SetAnimatorBoolSafe("IsSprinting", false);
        }
    }
    
    void UpdateAnimator()
    {
        if (animator == null) return;
        
        // Use safe parameter setting with HasParameter check
        SetAnimatorFloatSafe("Speed", currentSpeed);
        SetAnimatorBoolSafe("IsGrounded", isGrounded);
        
        bool isMoving = currentSpeed > 0.1f;
        SetAnimatorBoolSafe("IsMoving", isMoving);
        
        SetAnimatorBoolSafe("IsSprinting", isSprinting);
        SetAnimatorFloatSafe("SprintSpeed", isSprinting ? 1f : 0f);
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player) && Input.GetKeyDown(KeyCode.F1))
        {
            ListAnimatorParameters();
        }
    }
    
    // Safe methods to set animator parameters
    void SetAnimatorBoolSafe(string paramName, bool value)
    {
        if (animator == null) return;
        
        // Check if parameter exists before setting it
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(paramName, value);
                return;
            }
        }
    }
    
    void SetAnimatorFloatSafe(string paramName, float value)
    {
        if (animator == null) return;
        
        // Check if parameter exists before setting it
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(paramName, value);
                return;
            }
        }
    }
    
    void SetAnimatorTriggerSafe(string paramName)
    {
        if (animator == null) return;
        
        // Check if parameter exists before setting it
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(paramName);
                return;
            }
        }
    }
    
    void ListAnimatorParameters()
    {
        if (animator == null) return;
        
        Debug.Log("=== Animator Parameters ===");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"- {param.name} ({param.type})");
        }
        Debug.Log("==========================");
    }
    
    void ReenableFootsteps()
    {
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepsEnabled(true);
        }
    }
    
    public void EnableControls(bool enable)
    {
        isControlEnabled = enable;
        
        if (!enable)
        {
            inputX = 0f;
            inputY = 0f;
            moveDirection = Vector3.zero;
            currentSpeed = 0f;
            
            if (isSprinting)
            {
                StopSprint();
            }
            
            UpdateAnimator();
        }
    }
    
    void FixModelPosition()
    {
        Transform modelTransform = transform.Find("Model");
        if (modelTransform == null)
        {
            modelTransform = transform.Find("Visuals");
        }
        if (modelTransform == null)
        {
            modelTransform = GetComponentInChildren<SkinnedMeshRenderer>()?.transform;
        }
        
        if (modelTransform != null)
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
            {
                Debug.Log($"🔧 Modèle trouvé: {modelTransform.name} à la position locale {modelTransform.localPosition}");
            }
            
            // Force correct alignment
            modelTransform.localPosition = new Vector3(0, 0, 0);
            modelTransform.localRotation = Quaternion.identity;
        }
        else
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
            {
                Debug.LogWarning("⚠️ Aucun modèle trouvé sous 'Model', 'Visuals' ou avec SkinnedMeshRenderer");
            }
        }
    }
    
    public Vector3 GetVelocity()
    {
        if (controller != null)
        {
            return controller.velocity;
        }
        return Vector3.zero;
    }
    
    public bool IsMoving()
    {
        return currentSpeed > 0.1f;
    }
    
    // For UI and systems
    public float GetCurrentStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }
    
    public bool IsSprinting()
    {
        return isSprinting;
    }
    
    void OnDrawGizmos()
    {
        Vector3 pos = transform.position;
        
        // Ground check sphere
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(pos + Vector3.down * groundCheckDistance, 0.2f);
        
        // Movement direction
        if (Application.isPlaying && moveDirection.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(pos, moveDirection * 2f);
        }
    }
}
