using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gère la transparence des objets entre la caméra et le joueur (Compatible URP)
/// </summary>
public class URPCameraObstacleHandler : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    
    [Header("Transparence")]
    [Range(0f, 1f)]
    [SerializeField] private float transparentAlpha = 0.3f;
    [SerializeField] private float fadeSpeed = 5f;
    
    [Header("Détection")]
    [SerializeField] private float raycastPadding = 0.5f;
    [SerializeField] private bool useSphereCast = true;
    [SerializeField] private float sphereRadius = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = false;
    
    // Cache des objets transparents
    private Dictionary<Renderer, ObstacleInfo> transparentObjects = new Dictionary<Renderer, ObstacleInfo>();
    private List<Renderer> objectsToRestore = new List<Renderer>();
    
    private class ObstacleInfo
    {
        public Material[] originalMaterials;
        public Material[] transparentMaterials;
        public float currentAlpha;
        public bool isTransparent;
        
        public ObstacleInfo(Renderer renderer)
        {
            originalMaterials = renderer.sharedMaterials;
            transparentMaterials = new Material[originalMaterials.Length];
            currentAlpha = 1f;
            isTransparent = false;
            
            // Créer des copies des matériaux pour la transparence
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                if (originalMaterials[i] != null)
                {
                    transparentMaterials[i] = new Material(originalMaterials[i]);
                    SetupTransparentMaterial(transparentMaterials[i]);
                }
            }
        }
        
        private void SetupTransparentMaterial(Material mat)
        {
            // Configuration pour URP
            if (mat.shader.name.Contains("Universal Render Pipeline"))
            {
                // Activer la transparence
                mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                mat.SetFloat("_Blend", 0); // 0 = Alpha
                mat.SetFloat("_ZWrite", 0);
                mat.SetFloat("_AlphaClip", 0);
                
                // Définir le mode de rendu
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                
                // Activer les mots-clés nécessaires
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            // Support pour les shaders Built-in au cas où
            else
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
        }
        
        public void UpdateAlpha(float targetAlpha, float deltaTime, float fadeSpeed)
        {
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, deltaTime * fadeSpeed);
            
            foreach (var mat in transparentMaterials)
            {
                if (mat != null)
                {
                    Color color = mat.color;
                    color.a = currentAlpha;
                    mat.color = color;
                    
                    // Pour URP, on doit aussi mettre à jour _BaseColor
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color baseColor = mat.GetColor("_BaseColor");
                        baseColor.a = currentAlpha;
                        mat.SetColor("_BaseColor", baseColor);
                    }
                }
            }
        }
        
        public void Cleanup()
        {
            foreach (var mat in transparentMaterials)
            {
                if (mat != null)
                {
                    Object.Destroy(mat);
                }
            }
        }
    }
    
    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        if (cam == null)
        {
            cam = Camera.main;
        }
    }
    
    void Update()
    {
        if (player == null || cam == null) return;
        
        CheckForObstacles();
        UpdateTransparentObjects();
        CleanupRestoredObjects();
    }
    
    void CheckForObstacles()
    {
        Vector3 direction = player.position - cam.transform.position;
        float distance = direction.magnitude - raycastPadding;
        direction.Normalize();
        
        RaycastHit[] hits;
        
        if (useSphereCast)
        {
            hits = Physics.SphereCastAll(cam.transform.position, sphereRadius, direction, distance, obstacleLayerMask);
        }
        else
        {
            hits = Physics.RaycastAll(cam.transform.position, direction, distance, obstacleLayerMask);
        }
        
        // Marquer tous les objets actuels comme "à restaurer"
        foreach (var kvp in transparentObjects)
        {
            kvp.Value.isTransparent = false;
        }
        
        // Traiter les objets touchés
        foreach (var hit in hits)
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                if (!transparentObjects.ContainsKey(renderer))
                {
                    // Nouvel objet à rendre transparent
                    var info = new ObstacleInfo(renderer);
                    transparentObjects.Add(renderer, info);
                    renderer.materials = info.transparentMaterials;
                }
                
                transparentObjects[renderer].isTransparent = true;
            }
        }
        
        if (showDebugRays)
        {
            Debug.DrawRay(cam.transform.position, direction * distance, Color.red);
        }
    }
    
    void UpdateTransparentObjects()
    {
        objectsToRestore.Clear();
        
        foreach (var kvp in transparentObjects)
        {
            var renderer = kvp.Key;
            var info = kvp.Value;
            
            if (renderer == null)
            {
                objectsToRestore.Add(renderer);
                continue;
            }
            
            float targetAlpha = info.isTransparent ? transparentAlpha : 1f;
            info.UpdateAlpha(targetAlpha, Time.deltaTime, fadeSpeed);
            
            // Si l'objet est redevenu opaque, le marquer pour restauration
            if (!info.isTransparent && Mathf.Approximately(info.currentAlpha, 1f))
            {
                objectsToRestore.Add(renderer);
            }
        }
    }
    
    void CleanupRestoredObjects()
    {
        foreach (var renderer in objectsToRestore)
        {
            if (transparentObjects.TryGetValue(renderer, out var info))
            {
                // Restaurer les matériaux originaux
                if (renderer != null)
                {
                    renderer.materials = info.originalMaterials;
                }
                
                // Nettoyer les matériaux temporaires
                info.Cleanup();
                
                // Retirer du dictionnaire
                transparentObjects.Remove(renderer);
            }
        }
    }
    
    void OnDestroy()
    {
        // Nettoyer tous les matériaux créés
        foreach (var kvp in transparentObjects)
        {
            if (kvp.Key != null)
            {
                kvp.Key.materials = kvp.Value.originalMaterials;
            }
            kvp.Value.Cleanup();
        }
        transparentObjects.Clear();
    }
    
    void OnDrawGizmosSelected()
    {
        if (cam != null && player != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 direction = player.position - cam.transform.position;
            Gizmos.DrawLine(cam.transform.position, player.position);
            
            if (useSphereCast)
            {
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(cam.transform.position, sphereRadius);
                Gizmos.DrawWireSphere(player.position, sphereRadius);
            }
        }
    }
}
