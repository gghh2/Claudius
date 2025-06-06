using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    
    [Header("Jump")]
    public float jumpForce = 8f;
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.2f;
    
    [Header("Jump Effects")]
    public ParticleSystem jumpEffect;
    public AudioClip jumpSound;
    public AudioSource audioSource;
    
    [Header("Animation")]
    public Animator animator;
    
    // Variables privées
    private Vector3 moveDirection;
    private Rigidbody rb;
    private bool isGrounded = false;
    
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
        
        // CORRECTION : Récupère l'Animator dans les enfants
        if (animator == null)
        {
            animator = GetComponent<Animator>(); // D'abord sur cet objet
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(); // Puis dans les enfants
                Debug.Log("🎭 Animator trouvé dans les enfants: " + (animator != null ? animator.name : "AUCUN"));
            }
        }
        
        // Vérification finale
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
        
        Debug.Log("🎮 PlayerController initialisé avec Animator");
    }
    
    void Update()
    {
        HandleInput();
        CheckGrounded();
        UpdateAnimator();
    }
    
    void FixedUpdate()
    {
        MovePlayer();
    }
    
    void HandleInput()
    {
        // Récupère les inputs
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        
        // Calcule la direction de mouvement
        moveDirection = new Vector3(inputX, 0, inputY).normalized;
        
        // CORRECTION : Calcule la vitesse selon le mouvement désiré
        // Option 1: Vitesse basée sur l'input (plus réactive)
        currentSpeed = moveDirection.magnitude * moveSpeed;
        
        // Option 2: Vitesse basée sur la vélocité réelle du Rigidbody
        // currentSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        
        // Gestion du saut
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }
    
    void MovePlayer()
    {
        Vector3 movement = moveDirection * moveSpeed;
        
        // Garde la vélocité Y actuelle (pour le saut)
        rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
        
        // Rotation du personnage vers la direction de mouvement
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }
    
    void UpdateAnimator()
    {
        if (animator == null) 
        {
            Debug.LogWarning("❌ Animator est NULL ! Impossible de mettre à jour les animations.");
            return;
        }
        
        // Pour l'isométrique, on n'a besoin que de la vitesse
        animator.SetFloat("Speed", currentSpeed);
        animator.SetBool("IsGrounded", isGrounded);
        
        // Paramètre simplifié pour savoir si on bouge
        bool isMoving = currentSpeed > 0.1f;
        animator.SetBool("IsMoving", isMoving);
        
        // DEBUG TEMPORAIRE - Retirez après diagnostic
        if (Time.frameCount % 30 == 0) // Toutes les demi-secondes environ
        {
            Debug.Log($"🎭 DEBUG ANIMATOR: Speed={currentSpeed:F2} | IsMoving={isMoving} | Input=({inputX:F1},{inputY:F1}) | Animator={animator.gameObject.name}");
        }
        
        // Debug manuel avec F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"🎭 Animator State: Speed={currentSpeed:F2}, IsMoving={isMoving}, Direction={moveDirection}");
        }
    }
    
    void Jump()
    {
        // Applique la force de saut
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        // Animation de saut
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
        
        // Désactive temporairement les pas pendant le saut
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepsEnabled(false);
            footstepSystem.PlayJumpParticles();
            
            // Réactive après une courte durée
            Invoke(nameof(ReenableFootsteps), 0.3f);
        }
        
        // Effets visuels et sonores
        if (jumpEffect != null)
            jumpEffect.Play();
        
        if (jumpSound != null && audioSource != null)
            audioSource.PlayOneShot(jumpSound);
        
        Debug.Log("🦘 Saut !");
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
        // Raycast vers le bas pour vérifier si on touche le sol
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        
        // Son d'atterrissage
        if (!wasGrounded && isGrounded && footstepSystem != null)
        {
            footstepSystem.ForceFootstep();
            footstepSystem.PlayLandingParticles();
        }
        
        // Debug visuel
        Debug.DrawRay(rayStart, Vector3.down * (groundCheckDistance + 0.1f), 
                     isGrounded ? Color.green : Color.red);
    }
    
    // Méthodes publiques pour contrôle externe
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
    
    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }
    
    // Debug dans l'éditeur
    void OnGUI()
    {
        if (!Input.GetKey(KeyCode.F1)) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 250, 120));
        GUILayout.Label("=== PLAYER DEBUG (F1) ===");
        GUILayout.Label($"Vitesse: {currentSpeed:F2}");
        GUILayout.Label($"Au sol: {(isGrounded ? "✅" : "❌")}");
        GUILayout.Label($"Direction: {moveDirection}");
        GUILayout.Label($"En mouvement: {(IsMoving() ? "✅" : "❌")}");
        
        if (animator != null)
        {
            GUILayout.Label($"État actuel: {animator.GetCurrentAnimatorStateInfo(0).shortNameHash}");
        }
        
        GUILayout.EndArea();
    }
}