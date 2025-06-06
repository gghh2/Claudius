using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Le joueur à suivre
    
    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(10f, 10f, -10f); // Position relative à la caméra
    public float smoothSpeed = 0.125f; // Vitesse de suivi
    public bool followX = true;
    public bool followZ = true;
    public bool followY = false;
    
    [Header("Zoom Settings (Orthographic)")]
    public bool enableZoom = true;
    public float zoomSpeed = 1f;
    public float minSize = 2f;   // Zoom avant (plus proche)
    public float maxSize = 15f;  // Zoom arrière (vue d'ensemble)
    public float defaultSize = 5f;
    public float zoomSmoothness = 5f;
    
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
        
        // Assure-toi qu'elle est en mode orthographique
        if (!cam.orthographic)
        {
            cam.orthographic = true;
            Debug.Log("Caméra mise en mode orthographique");
        }
        
        // Trouve le target automatiquement
        if (target == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
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
        
        // Initialise le zoom
        cam.orthographicSize = defaultSize;
        targetSize = defaultSize;
    }
    
    void Update()
    {
        HandleZoomInput();
        UpdateCameraZoom();
    }



    void HandleZoomInput()
	{
	    if (!enableZoom) return;
	    
	    // BONUS : Désactive le zoom si dialogue ouvert OU historique ouvert
	    if (DialogueUI.Instance != null)
	    {
	        bool dialogueOpen = DialogueUI.Instance.IsDialogueOpen();
	        
	        // Vérifie aussi si l'historique est ouvert (bonus)
	        bool historyOpen = false;
	        if (DialogueUI.Instance.historyPanel != null)
	        {
	            historyOpen = DialogueUI.Instance.historyPanel.activeInHierarchy;
	        }
	        
	        if (dialogueOpen || historyOpen)
	        {
	            return; // Pas de zoom pendant les dialogues ou l'historique
	        }
	    }
	    
	    // ALTERNATIVE ROBUSTE : Gestion de la molette ET des raccourcis
	    
	    // Récupère l'input de la molette
	    float scrollInput = Input.GetAxis("Mouse ScrollWheel");
	    
	    if (scrollInput != 0f)
	    {
	        // Ajuste le zoom cible
	        targetSize -= scrollInput * zoomSpeed;
	        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
	        
	        //Debug.Log($"Zoom - Taille cible: {targetSize:F1}");
	    }
	    
	    // Raccourcis clavier (aussi désactivés pendant les dialogues)
	    if (Input.GetKeyDown(KeyCode.R))
	    {
	        ResetZoom();
	        //Debug.Log("Zoom resetté");
	    }
	    
	    // BONUS SUPPLÉMENTAIRE : Raccourcis + et - pour le zoom
	    if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
	    {
	        targetSize -= Time.deltaTime * zoomSpeed; // + pour zoom avant
	        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
	    }
	    
	    if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
	    {
	        targetSize += Time.deltaTime * zoomSpeed; // - pour zoom arrière
	        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
	    }
	}
    
    void UpdateCameraZoom()
    {
        // Transition fluide vers la taille cible
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSmoothness);
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Position désirée
        Vector3 desiredPosition = target.position + offset;
        
        // Applique les restrictions d'axes
        Vector3 currentPos = transform.position;
        
        if (!followX) desiredPosition.x = currentPos.x;
        if (!followY) desiredPosition.y = currentPos.y;
        if (!followZ) desiredPosition.z = currentPos.z;
        
        // Applique les limites si activées
        if (useBoundaries)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minZ, maxZ);
        }
        
        // Mouvement fluide vers la position désirée
        Vector3 smoothedPosition = Vector3.Lerp(currentPos, desiredPosition, smoothSpeed);
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
        return cam.orthographicSize;
    }
    
    // Zoom instantané (sans transition)
    public void SetZoomInstant(float size)
    {
        targetSize = Mathf.Clamp(size, minSize, maxSize);
        cam.orthographicSize = targetSize;
    }
}