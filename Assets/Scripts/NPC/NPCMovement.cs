using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float waitTimeMin = 2f;
    public float waitTimeMax = 5f;
    
    [Header("Movement Area")]
    public float movementRadius = 5f;
    public bool showMovementArea = true; // Pour visualiser la zone dans l'éditeur
    
    [Header("Debug")]
    [Tooltip("Affiche les gizmos de mouvement dans la scène")]
    public bool showGizmos = true;
    // Debug logs sont maintenant gérés par GlobalDebugManager
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isStopped = false; // Quand en dialogue
    private float waitTimer = 0f;
    private Rigidbody rb;
    private NPC npcScript;
    
    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition;
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure le Rigidbody pour éviter les problèmes physiques
        rb.freezeRotation = true;
        rb.drag = 5f; // Arrêt plus naturel
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Empêche la rotation
        rb.useGravity = true; // Important pour rester au sol
        
        npcScript = GetComponent<NPC>();
        
        // Commence avec un délai aléatoire
        waitTimer = Random.Range(waitTimeMin, waitTimeMax);
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.NPC))
            Debug.Log($"{gameObject.name} - Position de départ : {startPosition}");
    }
    
    void Update()
    {
        // Ne bouge pas si en dialogue
        if (isStopped)
        {
            rb.velocity = Vector3.zero;
            return;
        }
        
        if (!isMoving)
        {
            // Phase d'attente
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                ChooseNewTarget();
            }
        }
        else
        {
            // Phase de mouvement
            MoveToTarget();
        }
    }
    
    void ChooseNewTarget()
    {
        // Génère une position aléatoire dans le rayon autorisé
        Vector2 randomCircle = Random.insideUnitCircle * movementRadius;
        Vector3 randomPosition = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Vérifie que la position est accessible (pas d'obstacles)
        if (IsPositionValid(randomPosition))
        {
            targetPosition = randomPosition;
            isMoving = true;
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.NPC))
                Debug.Log($"{gameObject.name} - Nouvelle cible : {targetPosition}");
        }
        else
        {
            // Si la position n'est pas valide, réessaie bientôt
            waitTimer = 0.5f;
        }
    }
    
    void MoveToTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Garde le mouvement horizontal
        
        // Distance à la cible
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        if (distanceToTarget > 0.5f)
        {
            // NOUVEAU: Utilise MovePosition au lieu de velocity
            Vector3 newPosition = transform.position + (direction * moveSpeed * Time.deltaTime);
            rb.MovePosition(newPosition);
            
            // Optionnel : garde velocity pour compatibilité
            rb.velocity = direction * moveSpeed;
            
            // Fait tourner le NPC vers sa direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        else
        {
            // Arrivé à destination
            rb.velocity = Vector3.zero;
            isMoving = false;
            waitTimer = Random.Range(waitTimeMin, waitTimeMax);
            
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.NPC))
                Debug.Log($"{gameObject.name} - Arrivé à destination, attente de {waitTimer:F1}s");
        }
    }
    
    bool IsPositionValid(Vector3 position)
    {
        // Vérifie qu'il n'y a pas d'obstacles (optionnel)
        // Tu peux ajouter des vérifications plus complexes ici
        
        // Vérifie que la position reste dans les limites du monde
        // (ajuste selon tes besoins)
        return true;
    }
    
    // Méthodes publiques pour contrôler le mouvement
    public void StopMovement()
    {
        isStopped = true;
        rb.velocity = Vector3.zero;
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.NPC))
            Debug.Log($"{gameObject.name} - Mouvement arrêté (dialogue)");
    }
    
    public void ResumeMovement()
    {
        isStopped = false;
        isMoving = false;
        waitTimer = Random.Range(0.5f, 2f); // Reprend bientôt
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.NPC))
            Debug.Log($"{gameObject.name} - Mouvement repris");
    }
    
    // Pour visualiser la zone de mouvement dans l'éditeur
    void OnDrawGizmosSelected()
    {
        if (showGizmos && showMovementArea)
        {
            Vector3 center = Application.isPlaying ? startPosition : transform.position;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, movementRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, 0.2f); // Point central
            
            if (Application.isPlaying && GlobalDebugManager.IsDebugEnabled(DebugSystem.NPC))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetPosition, 0.3f); // Position cible
            }
        }
    }
}