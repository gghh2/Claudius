using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Le joueur à suivre
    
    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(10f, 10f, -10f); // Position relative à la caméra
    public float smoothSpeed = 0.125f; // Vitesse de suivi
    
    [Header("Follow Mode")]
    [Tooltip("Maintient toujours la même distance entre la caméra et le joueur")]
    public bool maintainConstantDistance = true;
    
    [Tooltip("Distance fixe à maintenir avec le joueur")]
    public float fixedDistance = 14.14f; // Distance par défaut (correspond à offset 10,10,-10)
    
    [Header("Zoom Settings")]
    public bool enableZoom = true;
    public float zoomSpeed = 1f;
    [Tooltip("Caméra ortho : taille orthographique. Caméra perspective : distance " +
        "minimum (le zoom déplace la cam le long de son offset).")]
    public float minSize = 2f;
    [Tooltip("Caméra ortho : taille orthographique. Caméra perspective : distance " +
        "maximum (cam la plus éloignée du joueur).")]
    public float maxSize = 15f;
    public float defaultSize = 5f;
    public float zoomSmoothness = 5f;
    [Tooltip("Caméra perspective : multiplicateur de zoom (le scroll est moins " +
        "réactif que pour l'ortho car on bouge sur 14m de distance par défaut).")]
    public float perspectiveZoomSpeedMultiplier = 5f;
    
    [Header("Boundaries (Optional)")]
    public bool useBoundaries = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minZ = -10f;
    public float maxZ = 10f;
    
    private Camera cam;
    private float targetSize;
    
    void Start()
    {
        // Récupère la caméra
        cam = GetComponent<Camera>();

        // PAS de force-to-orthographic — la caméra peut être perspective ou
        // ortho selon le choix du projet. Le zoom s'adapte ci-dessous.
        
        // Trouve le target automatiquement
        if (target == null)
        {
            PlayerControllerCC player = FindFirstObjectByType<PlayerControllerCC>();
            if (player != null)
            {
                target = player.transform;
                Debug.Log("Target automatiquement assigné au joueur");
            }
            else
            {
                Debug.LogWarning("Aucun joueur trouvé ! Assignez manuellement le target.");
            }
        }
        
        // Calcule la distance fixe basée sur l'offset initial
        if (maintainConstantDistance)
        {
            fixedDistance = offset.magnitude;
            Debug.Log($"Distance fixe calculée: {fixedDistance:F2}");
        }
        
        // Initialise le zoom selon le mode caméra.
        if (cam.orthographic)
        {
            cam.orthographicSize = defaultSize;
            targetSize = defaultSize;
        }
        else
        {
            // Perspective : la "taille" est interprétée comme une distance.
            // On démarre à la distance fixe par défaut, et le zoom modifie
            // fixedDistance dans la plage [minSize, maxSize].
            targetSize = Mathf.Clamp(fixedDistance, minSize, maxSize);
            fixedDistance = targetSize;
        }
    }
    
    void Update()
    {
        HandleZoomInput();
        UpdateCameraZoom();
    }



    void HandleZoomInput()
    {
        if (!enableZoom) return;
        
        // Check if any UI is open through UnifiedUIManager
        if (UnifiedUIManager.Instance != null && UnifiedUIManager.Instance.IsBlockingGameplay())
        {
            return; // No zoom when any UI panel is open
        }
        
        // Récupère l'input de la molette
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (scrollInput != 0f)
        {
            // En perspective, on agit sur la distance (échelle 2-15m typiquement,
            // donc multiplicateur pour rendre le scroll utile).
            float speed = zoomSpeed * (cam.orthographic ? 1f : perspectiveZoomSpeedMultiplier);
            targetSize -= scrollInput * speed;
            targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
        }
        
        // Raccourcis clavier
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetZoom();
        }
    }
    
    void UpdateCameraZoom()
    {
        // Transition fluide vers la taille/distance cible selon le mode caméra.
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSmoothness);
        }
        else
        {
            // Perspective : c'est la fixedDistance qui change, le LateUpdate
            // applique alors la nouvelle position de caméra.
            fixedDistance = Mathf.Lerp(fixedDistance, targetSize, Time.deltaTime * zoomSmoothness);
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 desiredPosition;
        
        if (maintainConstantDistance)
        {
            // NOUVEAU : Maintient toujours la même distance
            // La direction reste la même (offset normalisé)
            Vector3 direction = offset.normalized;
            
            // Position désirée = position du joueur + direction * distance fixe
            desiredPosition = target.position + direction * fixedDistance;
        }
        else
        {
            // Ancien comportement : suit avec l'offset fixe
            desiredPosition = target.position + offset;
        }
        
        // Applique les limites si activées
        if (useBoundaries)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minZ, maxZ);
        }
        
        // Mouvement fluide vers la position désirée
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
    
    // Méthodes publiques utiles
    public void SetZoom(float size)
    {
        targetSize = Mathf.Clamp(size, minSize, maxSize);
    }
    
    public void ResetZoom()
    {
        targetSize = defaultSize;
        //Debug.Log("Zoom resetté");
    }
    
    public float GetCurrentZoom()
    {
        return cam.orthographic ? cam.orthographicSize : fixedDistance;
    }

    // Zoom instantané (sans transition)
    public void SetZoomInstant(float size)
    {
        targetSize = Mathf.Clamp(size, minSize, maxSize);
        if (cam.orthographic)
            cam.orthographicSize = targetSize;
        else
            fixedDistance = targetSize;
    }
}