using UnityEngine;

/// <summary>
/// Exemple de contrôles personnalisés sans utiliser Input.GetAxis
/// </summary>
public class CustomPlayerControls : MonoBehaviour
{
    [Header("Custom Key Bindings")]
    [Tooltip("Touche pour aller en avant")]
    public KeyCode forwardKey = KeyCode.W;
    
    [Tooltip("Touche pour aller en arrière")]
    public KeyCode backwardKey = KeyCode.S;
    
    [Tooltip("Touche pour aller à gauche")]
    public KeyCode leftKey = KeyCode.A;
    
    [Tooltip("Touche pour aller à droite")]
    public KeyCode rightKey = KeyCode.D;
    
    [Tooltip("Touche pour sauter")]
    public KeyCode jumpKey = KeyCode.Space;
    
    [Tooltip("Touche pour sprinter")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    
    private Vector3 moveDirection;
    private bool isSprinting;
    
    void Update()
    {
        // Récupère les inputs personnalisés
        float horizontal = 0f;
        float vertical = 0f;
        
        // Gestion horizontale
        if (Input.GetKey(leftKey))
            horizontal = -1f;
        else if (Input.GetKey(rightKey))
            horizontal = 1f;
            
        // Gestion verticale
        if (Input.GetKey(backwardKey))
            vertical = -1f;
        else if (Input.GetKey(forwardKey))
            vertical = 1f;
            
        // Calcul de la direction
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        
        // Sprint
        isSprinting = Input.GetKey(sprintKey);
        
        // Saut
        if (Input.GetKeyDown(jumpKey))
        {
            Debug.Log("Jump!");
        }
    }
    
    void FixedUpdate()
    {
        // Applique le mouvement
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        transform.Translate(moveDirection * currentSpeed * Time.fixedDeltaTime);
    }
}
