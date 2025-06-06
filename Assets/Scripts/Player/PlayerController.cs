using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    
    [Header("Jump")]
    public float jumpForce = 8f;
    public LayerMask groundLayer = 1; // Layer du sol
    public float groundCheckDistance = 0.2f;
    
    [Header("Jump Effects")]
    public ParticleSystem jumpEffect; // Effet de particules au saut
    public AudioClip jumpSound; // Son du saut - GLISSE TON FICHIER AUDIO ICI
    public AudioSource audioSource; // Source audio - OPTIONNEL si tu veux utiliser une autre AudioSource
    
    private Vector3 moveDirection;
    private Rigidbody rb;
    private bool isGrounded = false;
    
    // NOUVEAU : Référence au système de pas
    private FootstepSystem footstepSystem;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        
        // NOUVEAU: Assure-toi que le joueur a le bon tag
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
            Debug.Log("✅ Tag 'Player' assigné automatiquement");
        }
        
        // Si pas d'AudioSource assignée, en crée une automatiquement
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // NOUVEAU : Récupère le système de pas
        footstepSystem = GetComponent<FootstepSystem>();
        if (footstepSystem == null)
        {
            Debug.LogWarning("⚠️ Aucun FootstepSystem trouvé sur le joueur. Ajoutez le composant FootstepSystem pour les bruits de pas.");
        }
        else
        {
            Debug.Log("🦶 FootstepSystem détecté et connecté");
        }
    }
    
    void Update()
    {
        HandleInput();
        CheckGrounded();
    }
    
    void FixedUpdate()
    {
        MovePlayer();
    }
    
    void HandleInput()
    {
        // Mouvement horizontal
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Conversion pour vue isométrique
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        
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
    
    void Jump()
    {
        // Applique une force vers le haut
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        // NOUVEAU : Désactive temporairement les pas pendant le saut
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepsEnabled(false);
            
            // NOUVEAU : Effet de particules au décollage
            footstepSystem.PlayJumpParticles();
            
            // Réactive après une courte durée
            Invoke(nameof(ReenableFootsteps), 0.3f);
        }
        
        // Effets visuels
        if (jumpEffect != null)
            jumpEffect.Play();
        
        // Effets sonores
        if (jumpSound != null && audioSource != null)
            audioSource.PlayOneShot(jumpSound);
        
        Debug.Log("🦘 Saut !");
    }
    
    /// <summary>
    /// NOUVEAU : Réactive les bruits de pas après un saut
    /// </summary>
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
        
        // NOUVEAU : Son d'atterrissage
        if (!wasGrounded && isGrounded && footstepSystem != null)
        {
            // Force un pas quand on atterrit
            footstepSystem.ForceFootstep();
            
            // NOUVEAU : Effet de particules à l'atterrissage
            footstepSystem.PlayLandingParticles();
        }
        
        // Debug visuel (optionnel)
        Debug.DrawRay(rayStart, Vector3.down * (groundCheckDistance + 0.1f), 
                     isGrounded ? Color.green : Color.red);
    }
    
    /// <summary>
    /// NOUVEAU : Active/désactive les bruits de pas manuellement
    /// </summary>
    public void ToggleFootsteps(bool enabled)
    {
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepsEnabled(enabled);
            Debug.Log($"🦶 Bruits de pas {(enabled ? "activés" : "désactivés")}");
        }
    }
    
    /// <summary>
    /// NOUVEAU : Ajuste le volume des pas
    /// </summary>
    public void SetFootstepVolume(float volume)
    {
        if (footstepSystem != null)
        {
            footstepSystem.SetFootstepVolume(volume);
            Debug.Log($"🦶 Volume des pas: {volume:F2}");
        }
    }
    
    /// <summary>
    /// NOUVEAU : Méthode pour obtenir la vitesse actuelle (utile pour autres systèmes)
    /// </summary>
    public float GetCurrentSpeed()
    {
        return new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
    }
    
    /// <summary>
    /// NOUVEAU : Vérifie si le joueur bouge
    /// </summary>
    public bool IsMoving()
    {
        return GetCurrentSpeed() > 0.1f;
    }
}