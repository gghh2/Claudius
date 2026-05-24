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
    
    [Header("Mouse Look (rotation caméra clic-droit)")]
    [Tooltip("Maintenir le clic droit fait pivoter la caméra autour du joueur.")]
    public bool enableMouseLook = true;
    [Tooltip("Sensibilité horizontale (yaw).")]
    public float yawSensitivity = 4f;
    [Tooltip("Sensibilité verticale (pitch).")]
    public float pitchSensitivity = 2.5f;
    [Tooltip("Pitch minimum (en degrés, vue par-dessous → plonge).")]
    public float pitchMin = 10f;
    [Tooltip("Pitch maximum (vue plus rasante).")]
    public float pitchMax = 75f;

    float yaw, pitch;
    bool yawPitchInitialized = false;

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
        HandleMouseLook();
    }

    void HandleMouseLook()
    {
        if (!enableMouseLook) return;
        // Bloque la rotation quand un panel UI est ouvert (sinon le joueur
        // bouge la caméra sans s'en rendre compte en cliquant dans des menus).
        if (UnifiedUIManager.Instance != null && UnifiedUIManager.Instance.IsBlockingGameplay())
            return;

        // Initialise yaw/pitch depuis l'offset courant la 1re frame.
        if (!yawPitchInitialized)
        {
            Vector3 dir = offset.normalized;
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            // Pitch positif quand la cam est au-dessus du joueur.
            pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            yawPitchInitialized = true;
        }

        if (Input.GetMouseButton(1)) // bouton droit maintenu
        {
            yaw += Input.GetAxis("Mouse X") * yawSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * pitchSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            // Reconstruit l'offset (direction) depuis yaw/pitch ; magnitude
            // conservée pour ne pas perturber le zoom.
            float radYaw = yaw * Mathf.Deg2Rad;
            float radPitch = pitch * Mathf.Deg2Rad;
            float cp = Mathf.Cos(radPitch);
            Vector3 dir = new Vector3(Mathf.Sin(radYaw) * cp, Mathf.Sin(radPitch), Mathf.Cos(radYaw) * cp);
            offset = dir * offset.magnitude;
        }
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

        // Oriente la caméra vers le joueur (utile dès qu'on active la rotation
        // souris, sinon la cam garde sa rotation d'origine et le cadrage est
        // cassé). Sans mouseLook : pas d'effet visible.
        if (enableMouseLook && target != null)
        {
            transform.LookAt(target.position + Vector3.up * 1f);
        }
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