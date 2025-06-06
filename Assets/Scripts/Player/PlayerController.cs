using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    
    [Header("Jump")]
    public float jumpForce = 12f; // Augmenté de 8f à 12f
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.2f;
    [Range(1f, 3f)]
    [Tooltip("Multiplicateur de gravité pendant la chute (plus haut = chute plus rapide)")]
    public float fallGravityMultiplier = 2f;
    
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
        
        // Configuration Rigidbody pour mouvement plus fluide
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Lisse le mouvement
        rb.drag = 8f; // Ajoute de la résistance pour un arrêt plus net
        rb.angularDrag = 10f; // Résistance à la rotation
        rb.mass = 1f; // Masse normale
        
        // NOUVEAU : Correction automatique de position du modèle
        FixModelPosition();
        
        // Récupère l'Animator dans les enfants
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
        
        Debug.Log("🎮 PlayerController initialisé");
    }
    
    void Update()
    {
        HandleInput();
        CheckGrounded();
        UpdateAnimator();
        
        // NOUVEAU : Gravité augmentée pendant la chute pour saut plus réactif
        ApplyJumpPhysics();
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
        
        // SOLUTION 1 : Rotation plus fluide et conditionnelle
        if (moveDirection != Vector3.zero)
        {
            // Calcule la rotation cible
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            
            // Applique une rotation progressive au lieu d'instantanée
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 15f);
            
            // Alternative plus douce :
            // transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.fixedDeltaTime);
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
        
        // Debug manuel avec F1 seulement
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"🎭 Animator State: Speed={currentSpeed:F2}, IsMoving={isMoving}, Direction={moveDirection}");
        }
    }
    
    void Jump()
    {
        // SOLUTION : Saut plus réactif avec reset de vélocité Y
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Reset la vélocité Y
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
    
    /// <summary>
    /// NOUVEAU : Gère la physique du saut pour un feeling plus réactif
    /// </summary>
    void ApplyJumpPhysics()
    {
        // Si on tombe (vélocité Y négative) et qu'on n'est pas au sol
        if (rb.velocity.y < 0f && !isGrounded)
        {
            // Applique une gravité supplémentaire pour une chute plus rapide
            rb.AddForce(Vector3.down * Physics.gravity.magnitude * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
        }
        // Optionnel : Gravité réduite pendant la montée pour un saut plus "floaty" au pic
        else if (rb.velocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            // Si on relâche espace pendant la montée, on accélère la descente
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
        // CORRECTION : Raycast depuis les pieds du modèle, pas depuis le centre du Player
        Transform spaceManModel = transform.Find("space_man_model");
        Vector3 rayStart;
        
        if (spaceManModel != null)
        {
            // Raycast depuis la position du modèle (ses pieds)
            rayStart = spaceManModel.position + Vector3.up * 0.1f;
        }
        else
        {
            // Fallback : depuis le Player
            rayStart = transform.position + Vector3.up * 0.1f;
        }
        
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        
        // Son d'atterrissage
        if (!wasGrounded && isGrounded && footstepSystem != null)
        {
            footstepSystem.ForceFootstep();
            footstepSystem.PlayLandingParticles();
        }
        
        // Debug visuel depuis les pieds du modèle
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
    
    /// <summary>
    /// NOUVEAU : Corrige automatiquement la position du modèle par rapport au collider
    /// </summary>
    void FixModelPosition()
    {
        // Trouve le modèle space_man_model
        Transform spaceManModel = transform.Find("space_man_model");
        if (spaceManModel == null)
        {
            Debug.LogWarning("⚠️ space_man_model non trouvé dans les enfants");
            return;
        }
        
        // Récupère le Capsule Collider sur cet objet
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            Debug.LogWarning("⚠️ Capsule Collider non trouvé sur Player");
            return;
        }
        
        // Calcule la position pour que les pieds du modèle touchent le sol
        // Le collider va du sol (Y=0) jusqu'à sa hauteur
        // Le modèle doit être positionné pour que ses pieds soient au bas du collider
        
        Vector3 originalPosition = spaceManModel.localPosition;
        
        // SOLUTION : Place le modèle pour que ses pieds soient au niveau du bas du collider
        float colliderBottom = capsule.center.y - (capsule.height / 2f);
        float newY = colliderBottom; // Les pieds du modèle au bas du collider
        
        spaceManModel.localPosition = new Vector3(originalPosition.x, newY, originalPosition.z);
        
        Debug.Log($"🔧 Position du modèle corrigée:");
        Debug.Log($"  Collider: Center Y={capsule.center.y:F2}, Height={capsule.height:F2}");
        Debug.Log($"  Bas du collider: Y={colliderBottom:F2}");
        Debug.Log($"  Modèle avant: Y={originalPosition.y:F2}");
        Debug.Log($"  Modèle après: Y={newY:F2}");
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
        
        if (GUILayout.Button("Debug Model Position"))
        {
            FixModelPosition();
        }
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Méthode manuelle pour corriger la position du modèle
    /// </summary>
    [ContextMenu("Fix Model Position")]
    public void ManualFixModelPosition()
    {
        FixModelPosition();
    }
}