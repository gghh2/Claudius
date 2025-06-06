using UnityEngine;

public class PlayerController : MonoBehaviour
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
    
    [Header("Jump")]
    public float jumpForce = 12f;
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.2f;
    [Range(1f, 3f)]
    public float fallGravityMultiplier = 2f;
    
    [Header("Jump Effects")]
    public ParticleSystem jumpEffect;
    public AudioClip jumpSound;
    public AudioSource audioSource;
    
    [Header("Sprint Effects")]
    public ParticleSystem sprintEffect; // Effet visuel de sprint
    public AudioClip sprintStartSound;
    public AudioClip sprintStopSound;
    
    [Header("Animation")]
    public Animator animator;
    
    [Header("Camera Effects (Optional)")]
    public CameraFollow cameraFollow;
    public float sprintZoomMultiplier = 0.8f; // Multiplie le zoom actuel (0.8 = zoom out de 20%)
    
    // Variables privées
    private Vector3 moveDirection;
    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isSprinting = false;
    private bool canSprint = true;
    private float currentMoveSpeed;
    private float targetMoveSpeed;
    private float savedCameraZoom = -1f; // Sauvegarde du zoom avant sprint
    
    // Variables d'input pour l'animator
    private float inputX;
    private float inputY;
    private float currentSpeed;
    
    // Référence au système de pas
    private FootstepSystem footstepSystem;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        
        // Configuration Rigidbody pour mouvement plus fluide
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.drag = 8f;
        rb.angularDrag = 10f;
        rb.mass = 1f;
        
        // Initialise la stamina
        currentStamina = maxStamina;
        currentMoveSpeed = moveSpeed;
        targetMoveSpeed = moveSpeed;
        
        FixModelPosition();
        
        // Récupère l'Animator dans les enfants
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                Debug.Log("🎭 Animator trouvé dans les enfants: " + (animator != null ? animator.name : "AUCUN"));
            }
        }
        
        if (animator == null)
        {
            Debug.LogError("❌ AUCUN ANIMATOR TROUVÉ ! Vérifiez votre hiérarchie.");
        }
        else
        {
            Debug.Log($"✅ Animator trouvé sur: {animator.gameObject.name}");
        }
        
        // Vérifie le tag Player
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
        
        // Récupère le système de pas
        footstepSystem = GetComponent<FootstepSystem>();
        if (footstepSystem == null)
        {
            Debug.LogWarning("⚠️ Aucun FootstepSystem trouvé sur le joueur");
        }
        
        // Récupère la caméra si disponible
        if (cameraFollow == null)
        {
            cameraFollow = FindObjectOfType<CameraFollow>();
        }
        
        Debug.Log("🎮 PlayerController initialisé avec Sprint");
    }
    
    void Update()
    {
        HandleInput();
        CheckGrounded();
        UpdateSprint();
        UpdateStamina();
        UpdateAnimator();
        ApplyJumpPhysics();
        
        // Transition fluide de la vitesse
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, sprintTransitionSpeed);
    }
    
    void FixedUpdate()
    {
        MovePlayer();
    }
    
    void HandleInput()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        
        moveDirection = new Vector3(inputX, 0, inputY).normalized;
        
        currentSpeed = moveDirection.magnitude * currentMoveSpeed;
        
        // Gestion du sprint
        bool sprintPressed = Input.GetKey(sprintKey);
        bool isMoving = moveDirection.magnitude > 0.1f;
        
        // Détermine si on peut sprinter
        if (useStamina)
        {
            canSprint = currentStamina > 0f && (isGrounded || canSprintInAir) && isMoving;
        }
        else
        {
            canSprint = (isGrounded || canSprintInAir) && isMoving;
        }
        
        // Active/désactive le sprint
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
        
        // Gestion du saut
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }
    
    void UpdateSprint()
    {
        if (isSprinting)
        {
            targetMoveSpeed = sprintSpeed;
            
            // Ajuste la vitesse des pas pendant le sprint
            if (footstepSystem != null)
            {
                footstepSystem.stepInterval = Mathf.Lerp(footstepSystem.stepInterval, 0.3f, Time.deltaTime * 5f);
            }
            
            // Effet de particules continue
            if (sprintEffect != null && !sprintEffect.isPlaying)
            {
                sprintEffect.Play();
            }
        }
        else
        {
            targetMoveSpeed = moveSpeed;
            
            // Restaure la vitesse normale des pas
            if (footstepSystem != null)
            {
                footstepSystem.stepInterval = Mathf.Lerp(footstepSystem.stepInterval, 0.5f, Time.deltaTime * 5f);
            }
            
            // Arrête les particules
            if (sprintEffect != null && sprintEffect.isPlaying)
            {
                sprintEffect.Stop();
            }
        }
    }
    
    void UpdateStamina()
    {
        if (!useStamina) return;
        
        if (isSprinting)
        {
            // Consomme la stamina
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            staminaRegenTimer = staminaRegenDelay;
            
            // Force l'arrêt du sprint si plus de stamina
            if (currentStamina <= 0f)
            {
                StopSprint();
            }
        }
        else
        {
            // Régénère la stamina après un délai
            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            }
        }
    }
    
    void StartSprint()
    {
        isSprinting = true;
        
        Debug.Log("🏃 Sprint activé !");
        
        // Son de début de sprint
        if (sprintStartSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sprintStartSound, 0.5f);
        }
        
        // Sauvegarde et ajuste le zoom de la caméra
        if (cameraFollow != null && cameraFollow.enableZoom)
        {
            // Sauvegarde le zoom actuel du joueur
            savedCameraZoom = cameraFollow.GetCurrentZoom();
            
            // Applique un zoom relatif (multiplie le zoom actuel)
            float newZoom = savedCameraZoom * sprintZoomMultiplier;
            cameraFollow.SetZoom(newZoom);
            
            Debug.Log($"📷 Sprint Zoom: {savedCameraZoom:F1} → {newZoom:F1}");
        }
        
        // Déclenche l'animation de sprint
        if (animator != null)
        {
            animator.SetBool("IsSprinting", true);
        }
    }
    
    void StopSprint()
    {
        isSprinting = false;
        
        Debug.Log("🚶 Sprint désactivé");
        
        // Son de fin de sprint
        if (sprintStopSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sprintStopSound, 0.3f);
        }
        
        // Restaure le zoom sauvegardé de la caméra
        if (cameraFollow != null && cameraFollow.enableZoom && savedCameraZoom > 0)
        {
            cameraFollow.SetZoom(savedCameraZoom);
            Debug.Log($"📷 Zoom restauré: {savedCameraZoom:F1}");
            savedCameraZoom = -1f; // Reset la sauvegarde
        }
        
        // Arrête l'animation de sprint
        if (animator != null)
        {
            animator.SetBool("IsSprinting", false);
        }
    }
    
    void MovePlayer()
    {
        Vector3 movement = moveDirection * currentMoveSpeed;
        
        rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
        
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 15f);
        }
    }
    
    void UpdateAnimator()
    {
        if (animator == null) 
        {
            Debug.LogWarning("❌ Animator est NULL ! Impossible de mettre à jour les animations.");
            return;
        }
        
        animator.SetFloat("Speed", currentSpeed);
        animator.SetBool("IsGrounded", isGrounded);
        
        bool isMoving = currentSpeed > 0.1f;
        animator.SetBool("IsMoving", isMoving);
        
        // Paramètres de sprint
        animator.SetBool("IsSprinting", isSprinting);
        animator.SetFloat("SprintSpeed", isSprinting ? 1f : 0f);
        
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"🎭 Animator State: Speed={currentSpeed:F2}, IsMoving={isMoving}, IsSprinting={isSprinting}");
        }
    }
    
    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        if (animator != null)
        {
            animator.SetTrigger("Jump");
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
    
    void ApplyJumpPhysics()
    {
        if (rb.velocity.y < 0f && !isGrounded)
        {
            rb.AddForce(Vector3.down * Physics.gravity.magnitude * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (rb.velocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            rb.AddForce(Vector3.down * Physics.gravity.magnitude * 0.5f, ForceMode.Acceleration);
        }
    }
    
    void ReenableFootsteps()
    {
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepsEnabled(true);
        }
    }
    
    void CheckGrounded()
    {
        Transform spaceManModel = transform.Find("space_man_model");
        Vector3 rayStart;
        
        if (spaceManModel != null)
        {
            rayStart = spaceManModel.position + Vector3.up * 0.1f;
        }
        else
        {
            rayStart = transform.position + Vector3.up * 0.1f;
        }
        
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        
        if (!wasGrounded && isGrounded && footstepSystem != null)
        {
            footstepSystem.ForceFootstep();
            footstepSystem.PlayLandingParticles();
        }
        
        Debug.DrawRay(rayStart, Vector3.down * (groundCheckDistance + 0.1f), 
                     isGrounded ? Color.green : Color.red);
    }
    
    // Méthodes publiques
    public void ToggleFootsteps(bool enabled)
    {
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepsEnabled(enabled);
            Debug.Log($"🦶 Bruits de pas {(enabled ? "activés" : "désactivés")}");
        }
    }
    
    public void SetFootstepVolume(float volume)
    {
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepVolume(volume);
            Debug.Log($"🦶 Volume des pas: {volume:F2}");
        }
    }
    
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    
    public bool IsMoving()
    {
        return currentSpeed > 0.1f;
    }
    
    public bool IsSprinting()
    {
        return isSprinting;
    }
    
    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }
    
    public float GetStaminaPercentage()
    {
        return useStamina ? (currentStamina / maxStamina) : 1f;
    }
    
    void FixModelPosition()
    {
        Transform spaceManModel = transform.Find("space_man_model");
        if (spaceManModel == null)
        {
            Debug.LogWarning("⚠️ space_man_model non trouvé dans les enfants");
            return;
        }
        
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            Debug.LogWarning("⚠️ Capsule Collider non trouvé sur Player");
            return;
        }
        
        Vector3 originalPosition = spaceManModel.localPosition;
        
        float colliderBottom = capsule.center.y - (capsule.height / 2f);
        float newY = colliderBottom;
        
        spaceManModel.localPosition = new Vector3(originalPosition.x, newY, originalPosition.z);
        
        Debug.Log($"🔧 Position du modèle corrigée:");
        Debug.Log($"  Collider: Center Y={capsule.center.y:F2}, Height={capsule.height:F2}");
        Debug.Log($"  Bas du collider: Y={colliderBottom:F2}");
        Debug.Log($"  Modèle avant: Y={originalPosition.y:F2}");
        Debug.Log($"  Modèle après: Y={newY:F2}");
    }
    
    void OnGUI()
    {
        if (!Input.GetKey(KeyCode.F1)) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== PLAYER DEBUG (F1) ===");
        GUILayout.Label($"Vitesse actuelle: {currentSpeed:F2}");
        GUILayout.Label($"Vitesse de déplacement: {currentMoveSpeed:F2}");
        GUILayout.Label($"Au sol: {(isGrounded ? "✅" : "❌")}");
        GUILayout.Label($"Direction: {moveDirection}");
        GUILayout.Label($"En mouvement: {(IsMoving() ? "✅" : "❌")}");
        GUILayout.Label($"Sprint: {(isSprinting ? "✅ ACTIF" : "❌")}");
        
        if (useStamina)
        {
            GUILayout.Label($"Stamina: {currentStamina:F0}/{maxStamina:F0} ({GetStaminaPercentage()*100:F0}%)");
            GUILayout.Label($"Regen dans: {(staminaRegenTimer > 0 ? staminaRegenTimer.ToString("F1") + "s" : "Active")}");
        }
        
        if (animator != null)
        {
            GUILayout.Label($"État anim: {animator.GetCurrentAnimatorStateInfo(0).shortNameHash}");
        }
        
        if (GUILayout.Button("Debug Model Position"))
        {
            FixModelPosition();
        }
        
        if (GUILayout.Button("Toggle Sprint"))
        {
            if (isSprinting) StopSprint();
            else StartSprint();
        }
        
        GUILayout.EndArea();
    }
    
    [ContextMenu("Fix Model Position")]
    public void ManualFixModelPosition()
    {
        FixModelPosition();
    }
}