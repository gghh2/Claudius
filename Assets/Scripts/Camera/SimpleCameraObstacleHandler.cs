using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version corrigée pour les builds - sans champs non-sérialisables
/// </summary>
public class SimpleCameraObstacleHandler : MonoBehaviour
{
    [Header("Configuration")]
    public Transform player;
    public float transparencyLevel = 0.25f;
    public float fadeSpeed = 5f;
    public LayerMask obstacleLayer = -1;
    
    [Header("Exclusion Settings")]
    [Tooltip("Utilise les Tags pour exclure des objets")]
    public bool useTagExclusion = true;
    
    [Tooltip("Tags exclus de la transparence")]
    public string[] excludeTags = { "NPC", "Companion" };
    
    [Tooltip("Utilise le composant ExcludeFromTransparency")]
    public bool useComponentExclusion = true;
    
    [Header("Advanced")]
    public bool useMultipleRays = true;
    public int rayCount = 5;
    public float raySpread = 0.5f;
    
    [Header("Debug")]
    public bool debugMode = false;
    
    // Private fields - NOT serialized
    private Camera cam;
    private Dictionary<Renderer, MaterialState> affectedRenderers = new Dictionary<Renderer, MaterialState>();
    private List<Renderer> activeObstacles = new List<Renderer>();
    
    private class MaterialState
    {
        public Material[] originalMaterials;
        public Material[] transparentMaterials;
        public float currentAlpha = 1f;
        public bool isFadingOut = false;
    }
    
    void Start()
    {
        // Auto-detect player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("SimpleCameraObstacleHandler: No player found! Please assign the player transform or tag your player GameObject with 'Player'");
                enabled = false;
                return;
            }
        }
        
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }
    
    /// <summary>
    /// Vérifie si un GameObject doit être exclu de la transparence
    /// </summary>
    bool IsExcluded(GameObject obj)
    {
        // 1. Vérification par Tag (sur l'objet ET ses parents)
        if (useTagExclusion && excludeTags != null)
        {
            // Vérifie le tag sur toute la hiérarchie parent
            Transform current = obj.transform;
            while (current != null)
            {
                foreach (string tag in excludeTags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        try 
                        {
                            if (current.CompareTag(tag))
                            {
                                return true;
                            }
                        }
                        catch (System.Exception)
                        {
                            // Le tag n'existe pas dans le projet - on ignore silencieusement
                        }
                    }
                }
                
                current = current.parent;
            }
        }
        
        // 2. Vérification par Component (aussi sur les parents)
        if (useComponentExclusion)
        {
            if (obj.GetComponentInParent<ExcludeFromTransparency>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    void LateUpdate()
    {
        if (player == null || cam == null) return;
        
        // Clear active obstacles list
        activeObstacles.Clear();
        
        // Perform raycasts
        if (useMultipleRays)
        {
            PerformMultipleRaycasts();
        }
        else
        {
            PerformSingleRaycast();
        }
        
        // Update all material states
        UpdateMaterialStates();
    }
    
    void PerformSingleRaycast()
    {
        Vector3 direction = player.position - cam.transform.position;
        RaycastHit[] hits = Physics.RaycastAll(cam.transform.position, direction.normalized, direction.magnitude, obstacleLayer);
        
        foreach (RaycastHit hit in hits)
        {
            ProcessHit(hit);
        }
    }
    
    void PerformMultipleRaycasts()
    {
        Vector3 playerPos = player.position;
        Vector3 camPos = cam.transform.position;
        
        // Center ray
        Vector3 direction = playerPos - camPos;
        RaycastHit[] hits = Physics.RaycastAll(camPos, direction.normalized, direction.magnitude, obstacleLayer);
        foreach (RaycastHit hit in hits) ProcessHit(hit);
        
        // Additional rays in a circle pattern
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;
        
        for (int i = 0; i < rayCount; i++)
        {
            float angle = (i / (float)rayCount) * 360f * Mathf.Deg2Rad;
            Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * raySpread;
            Vector3 targetPos = playerPos + offset;
            direction = targetPos - camPos;
            
            hits = Physics.RaycastAll(camPos, direction.normalized, direction.magnitude, obstacleLayer);
            foreach (RaycastHit hit in hits) ProcessHit(hit);
        }
    }
    
    void ProcessHit(RaycastHit hit)
    {
        // Skip if it's the player
        if (hit.transform == player || hit.transform.IsChildOf(player)) return;
        
        // Vérification de l'exclusion
        if (IsExcluded(hit.collider.gameObject))
        {
            return;
        }
        
        Renderer renderer = hit.collider.GetComponent<Renderer>();
        if (renderer == null) renderer = hit.collider.GetComponentInChildren<Renderer>();
        
        if (renderer != null && renderer.enabled && !activeObstacles.Contains(renderer))
        {
            activeObstacles.Add(renderer);
            
            // Initialize if new
            if (!affectedRenderers.ContainsKey(renderer))
            {
                InitializeRenderer(renderer);
            }
            
            // Mark as active (should be transparent)
            if (affectedRenderers.ContainsKey(renderer))
            {
                affectedRenderers[renderer].isFadingOut = false;
            }
        }
    }
    
    void InitializeRenderer(Renderer renderer)
    {
        MaterialState state = new MaterialState();
        state.originalMaterials = renderer.sharedMaterials;
        state.transparentMaterials = new Material[state.originalMaterials.Length];
        
        // Create transparent versions of each material
        for (int i = 0; i < state.originalMaterials.Length; i++)
        {
            if (state.originalMaterials[i] != null)
            {
                // Create instance
                state.transparentMaterials[i] = new Material(state.originalMaterials[i]);
                
                // Configure for transparency
                ConfigureTransparentMaterial(state.transparentMaterials[i]);
            }
        }
        
        affectedRenderers[renderer] = state;
    }
    
    void ConfigureTransparentMaterial(Material mat)
    {
        if (mat == null) return;
        
        // Stocke le shader original
        string originalShaderName = mat.shader.name;
        
        // IMPORTANT : Force le changement vers un shader qui supporte la transparence
        // si le shader actuel ne le supporte pas
        if (originalShaderName.Contains("Mobile/Diffuse") || 
            originalShaderName.Contains("Mobile/Bumped") ||
            originalShaderName.Contains("Unlit") ||
            !mat.HasProperty("_Mode"))
        {
            // Change vers le shader Standard qui supporte la transparence
            Shader standardShader = Shader.Find("Standard");
            if (standardShader != null)
            {
                mat.shader = standardShader;
            }
        }
        
        // Configure la transparence selon le shader
        string shaderName = mat.shader.name.ToLower();
        
        if (shaderName.Contains("standard"))
        {
            // Standard shader configuration
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            // NOUVEAU : Désactive le culling pour voir les deux faces
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }
        else if (shaderName.Contains("universal render pipeline") || shaderName.Contains("urp"))
        {
            // URP configuration
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            
            // NOUVEAU : Désactive le culling pour URP
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }
        else
        {
            // Generic transparency settings
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            
            // NOUVEAU : Tente de désactiver le culling même pour les shaders génériques
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }
        
        // Set initial alpha
        if (mat.HasProperty("_Color"))
        {
            Color color = mat.color;
            color.a = transparencyLevel;
            mat.color = color;
        }
        else if (mat.HasProperty("_BaseColor"))
        {
            Color color = mat.GetColor("_BaseColor");
            color.a = transparencyLevel;
            mat.SetColor("_BaseColor", color);
        }
    }
    
    void UpdateMaterialStates()
    {
        List<Renderer> toRemove = new List<Renderer>();
        
        foreach (var kvp in affectedRenderers)
        {
            Renderer renderer = kvp.Key;
            MaterialState state = kvp.Value;
            
            if (renderer == null || !renderer.enabled)
            {
                toRemove.Add(renderer);
                continue;
            }
            
            // Check if this renderer should be transparent
            bool shouldBeTransparent = activeObstacles.Contains(renderer);
            
            if (!shouldBeTransparent)
            {
                state.isFadingOut = true;
            }
            
            // Update alpha
            float targetAlpha = state.isFadingOut ? 1f : transparencyLevel;
            state.currentAlpha = Mathf.Lerp(state.currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            
            // Apply materials and alpha
            if (state.currentAlpha < 0.99f)
            {
                // Use transparent materials
                renderer.materials = state.transparentMaterials;
                
                // Update alpha on all materials
                foreach (Material mat in renderer.materials)
                {
                    if (mat != null)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color color = mat.color;
                            color.a = state.currentAlpha;
                            mat.color = color;
                        }
                        else if (mat.HasProperty("_BaseColor"))
                        {
                            Color color = mat.GetColor("_BaseColor");
                            color.a = state.currentAlpha;
                            mat.SetColor("_BaseColor", color);
                        }
                    }
                }
            }
            else if (state.isFadingOut)
            {
                // Restore original materials
                renderer.sharedMaterials = state.originalMaterials;
                toRemove.Add(renderer);
            }
        }
        
        // Clean up fully restored renderers
        foreach (Renderer renderer in toRemove)
        {
            if (affectedRenderers.TryGetValue(renderer, out MaterialState state))
            {
                // Destroy temporary materials
                foreach (Material mat in state.transparentMaterials)
                {
                    if (mat != null) 
                    {
                        DestroyImmediate(mat);
                    }
                }
            }
            affectedRenderers.Remove(renderer);
        }
    }
    
    void OnDisable()
    {
        // Restore all renderers
        foreach (var kvp in affectedRenderers)
        {
            Renderer renderer = kvp.Key;
            MaterialState state = kvp.Value;
            
            if (renderer != null)
            {
                renderer.sharedMaterials = state.originalMaterials;
            }
            
            // Clean up temporary materials
            foreach (Material mat in state.transparentMaterials)
            {
                if (mat != null) 
                {
                    DestroyImmediate(mat);
                }
            }
        }
        
        affectedRenderers.Clear();
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null || cam == null) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cam.transform.position, player.position);
        
        if (useMultipleRays)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle = (i / (float)rayCount) * 360f * Mathf.Deg2Rad;
                Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * raySpread;
                Vector3 targetPos = player.position + offset;
                Gizmos.DrawLine(cam.transform.position, targetPos);
            }
        }
    }
}
