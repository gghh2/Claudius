using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rend les objets entre la caméra et le joueur semi-transparents
/// pour éviter que le joueur soit caché
/// </summary>
public class CameraObstacleTransparency : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Le transform du joueur à suivre (auto-détecté si non assigné)")]
    public Transform playerTransform;
    
    [Header("Transparency Settings")]
    [Tooltip("Opacité des objets qui cachent le joueur (0 = invisible, 1 = opaque)")]
    [Range(0f, 1f)]
    public float transparencyLevel = 0.3f;
    
    [Tooltip("Vitesse de transition vers la transparence")]
    [Range(0.1f, 5f)]
    public float fadeSpeed = 2f;
    
    [Header("Detection Settings")]
    [Tooltip("Layers à vérifier pour les obstacles")]
    public LayerMask obstacleLayerMask = -1; // Tous les layers par défaut
    
    [Tooltip("Distance supplémentaire au-delà du joueur pour le raycast")]
    [Range(0f, 2f)]
    public float extraRayDistance = 0.5f;
    
    [Tooltip("Rayon de la sphère de détection autour du joueur")]
    [Range(0.1f, 2f)]
    public float detectionRadius = 0.5f;
    
    [Header("Performance")]
    [Tooltip("Fréquence de mise à jour (fois par seconde)")]
    [Range(10f, 60f)]
    public float updateFrequency = 30f;
    
    [Header("Debug")]
    public bool showDebugRays = false;
    public bool showDebugLogs = false;
    
    // Cache des objets actuellement transparents
    private Dictionary<Renderer, ObstacleInfo> transparentObjects = new Dictionary<Renderer, ObstacleInfo>();
    private List<Renderer> objectsToRestore = new List<Renderer>();
    
    // Timer pour la fréquence de mise à jour
    private float updateTimer = 0f;
    private float updateInterval;
    
    // Camera reference
    private Camera mainCamera;
    
    // Classe pour stocker les infos des objets modifiés
    private class ObstacleInfo
    {
        public Dictionary<Material, float> originalAlpha = new Dictionary<Material, float>();
        public Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();
        public bool wasTransparent = false;
        public float currentAlpha = 1f;
        
        public ObstacleInfo(Renderer renderer)
        {
            // Sauvegarde les valeurs originales
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    originalColors[mat] = color;
                    originalAlpha[mat] = color.a;
                }
            }
        }
    }
    
    void Start()
    {
        // Auto-détecte le joueur si non assigné
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("✅ CameraObstacleTransparency: Joueur auto-détecté");
            }
            else
            {
                Debug.LogError("❌ CameraObstacleTransparency: Aucun joueur trouvé ! Assignez playerTransform ou taggez votre joueur 'Player'");
                enabled = false;
                return;
            }
        }
        
        // Récupère la caméra
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("❌ CameraObstacleTransparency: Aucune caméra trouvée !");
                enabled = false;
                return;
            }
        }
        
        // Calcule l'intervalle de mise à jour
        updateInterval = 1f / updateFrequency;
        
        Debug.Log($"🎥 CameraObstacleTransparency initialisé - Transparence: {transparencyLevel}, Fréquence: {updateFrequency}Hz");
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        // Limite la fréquence de mise à jour pour les performances
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;
        
        // Effectue la détection
        CheckForObstacles();
        
        // Met à jour les transparences avec interpolation
        UpdateTransparencies();
    }
    
    void CheckForObstacles()
    {
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 playerPosition = playerTransform.position;
        Vector3 direction = playerPosition - cameraPosition;
        float distance = direction.magnitude + extraRayDistance;
        
        // Reset la liste des objets à restaurer
        objectsToRestore.Clear();
        objectsToRestore.AddRange(transparentObjects.Keys);
        
        // Raycast principal de la caméra vers le joueur
        RaycastHit[] hits = Physics.RaycastAll(
            cameraPosition, 
            direction.normalized, 
            distance, 
            obstacleLayerMask
        );
        
        if (showDebugRays)
        {
            Debug.DrawRay(cameraPosition, direction.normalized * distance, Color.yellow);
        }
        
        // SphereCast pour une détection plus large autour du joueur
        RaycastHit[] sphereHits = Physics.SphereCastAll(
            cameraPosition,
            detectionRadius,
            direction.normalized,
            distance,
            obstacleLayerMask
        );
        
        // Combine les deux arrays de hits
        HashSet<Collider> allHitColliders = new HashSet<Collider>();
        foreach (RaycastHit hit in hits)
        {
            allHitColliders.Add(hit.collider);
        }
        foreach (RaycastHit hit in sphereHits)
        {
            allHitColliders.Add(hit.collider);
        }
        
        // Traite tous les objets touchés
        foreach (Collider collider in allHitColliders)
        {
            // Ignore le joueur lui-même
            if (collider.transform == playerTransform || 
                collider.transform.IsChildOf(playerTransform)) 
                continue;
            
            Renderer renderer = collider.GetComponent<Renderer>();
            if (renderer == null) 
                renderer = collider.GetComponentInChildren<Renderer>();
            
            if (renderer != null)
            {
                // Marque cet objet comme devant être transparent
                if (!transparentObjects.ContainsKey(renderer))
                {
                    MakeTransparent(renderer);
                }
                
                // Retire de la liste des objets à restaurer
                objectsToRestore.Remove(renderer);
            }
        }
        
        // Restaure les objets qui ne sont plus dans le chemin
        foreach (Renderer renderer in objectsToRestore)
        {
            RestoreOpacity(renderer);
        }
    }
    
    void MakeTransparent(Renderer renderer)
    {
        if (showDebugLogs)
            Debug.Log($"🫥 Rend transparent: {renderer.name}");
        
        ObstacleInfo info = new ObstacleInfo(renderer);
        transparentObjects[renderer] = info;
        
        // Change le mode de rendu des matériaux
        foreach (Material mat in renderer.materials)
        {
            // Active la transparence
            SetMaterialTransparent(mat);
        }
    }
    
    void RestoreOpacity(Renderer renderer)
    {
        if (transparentObjects.TryGetValue(renderer, out ObstacleInfo info))
        {
            if (showDebugLogs)
                Debug.Log($"👁️ Restaure l'opacité: {renderer.name}");
            
            // Marque pour restauration progressive
            info.wasTransparent = true;
        }
    }
    
    void UpdateTransparencies()
    {
        List<Renderer> toRemove = new List<Renderer>();
        
        foreach (var kvp in transparentObjects)
        {
            Renderer renderer = kvp.Key;
            ObstacleInfo info = kvp.Value;
            
            if (renderer == null)
            {
                toRemove.Add(renderer);
                continue;
            }
            
            // Calcule l'alpha cible
            float targetAlpha = info.wasTransparent ? 1f : transparencyLevel;
            
            // Interpole vers l'alpha cible
            info.currentAlpha = Mathf.Lerp(info.currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            
            // Applique l'alpha à tous les matériaux
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color") && info.originalColors.ContainsKey(mat))
                {
                    Color color = info.originalColors[mat];
                    color.a = info.currentAlpha;
                    mat.color = color;
                }
            }
            
            // Si l'objet est complètement restauré, on le retire
            if (info.wasTransparent && Mathf.Approximately(info.currentAlpha, 1f))
            {
                // Restaure le mode de rendu opaque
                foreach (Material mat in renderer.materials)
                {
                    if (info.originalColors.ContainsKey(mat))
                    {
                        SetMaterialOpaque(mat);
                        mat.color = info.originalColors[mat];
                    }
                }
                toRemove.Add(renderer);
            }
        }
        
        // Nettoie les objets complètement restaurés
        foreach (Renderer renderer in toRemove)
        {
            transparentObjects.Remove(renderer);
        }
    }
    
    void SetMaterialTransparent(Material material)
    {
        // Change le mode de rendu en Transparent
        material.SetFloat("_Mode", 3);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
    
    void SetMaterialOpaque(Material material)
    {
        // Restaure le mode de rendu Opaque
        material.SetFloat("_Mode", 0);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
    }
    
    void OnDisable()
    {
        // Restaure tous les objets à leur état original
        foreach (var kvp in transparentObjects)
        {
            Renderer renderer = kvp.Key;
            ObstacleInfo info = kvp.Value;
            
            if (renderer != null)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (info.originalColors.ContainsKey(mat))
                    {
                        SetMaterialOpaque(mat);
                        mat.color = info.originalColors[mat];
                    }
                }
            }
        }
        
        transparentObjects.Clear();
    }
    
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null || mainCamera == null) return;
        
        // Dessine la sphère de détection
        Gizmos.color = Color.cyan;
        Vector3 direction = (playerTransform.position - mainCamera.transform.position).normalized;
        float distance = Vector3.Distance(mainCamera.transform.position, playerTransform.position);
        
        // Dessine plusieurs sphères le long du chemin
        int sphereCount = 5;
        for (int i = 0; i < sphereCount; i++)
        {
            float t = (float)i / (sphereCount - 1);
            Vector3 pos = mainCamera.transform.position + direction * (distance * t);
            Gizmos.DrawWireSphere(pos, detectionRadius);
        }
        
        // Dessine la ligne principale
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(mainCamera.transform.position, playerTransform.position);
    }
}
