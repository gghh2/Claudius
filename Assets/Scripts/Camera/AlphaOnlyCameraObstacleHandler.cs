using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version qui fonctionne en modifiant uniquement l'alpha sans changer les shaders
/// Plus compatible avec les builds
/// </summary>
public class AlphaOnlyCameraObstacleHandler : MonoBehaviour
{
    [Header("Configuration")]
    public Transform player;
    public float transparencyLevel = 0.3f;
    public float fadeSpeed = 5f;
    public LayerMask obstacleLayer = -1;
    
    [Header("Detection")]
    public float detectionRadius = 0.5f;
    public bool useSphereCast = true;
    
    private Camera cam;
    private Dictionary<Renderer, RendererInfo> modifiedRenderers = new Dictionary<Renderer, RendererInfo>();
    private HashSet<Renderer> currentObstacles = new HashSet<Renderer>();
    private HashSet<Renderer> previousObstacles = new HashSet<Renderer>();
    
    private class RendererInfo
    {
        public Dictionary<int, Color> originalColors = new Dictionary<int, Color>();
        public float currentAlpha = 1f;
        public bool needsRestore = false;
        
        public RendererInfo(Renderer renderer)
        {
            // Store original colors
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material mat = renderer.sharedMaterials[i];
                if (mat != null)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        originalColors[i] = mat.GetColor("_Color");
                    }
                    else if (mat.HasProperty("_BaseColor"))
                    {
                        originalColors[i] = mat.GetColor("_BaseColor");
                    }
                }
            }
        }
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
                Debug.Log("[AlphaObstacle] Player found: " + player.name);
            }
            else
            {
                Debug.LogError("[AlphaObstacle] No player found! Tag your player with 'Player' tag.");
                enabled = false;
                return;
            }
        }
        
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        
        if (cam == null)
        {
            Debug.LogError("[AlphaObstacle] No camera found!");
            enabled = false;
        }
    }
    
    void LateUpdate()
    {
        if (player == null || cam == null) return;
        
        // Swap obstacle sets
        var temp = previousObstacles;
        previousObstacles = currentObstacles;
        currentObstacles = temp;
        currentObstacles.Clear();
        
        // Detect obstacles
        DetectObstacles();
        
        // Update renderer states
        UpdateRenderers();
    }
    
    void DetectObstacles()
    {
        Vector3 direction = player.position - cam.transform.position;
        float distance = direction.magnitude;
        
        if (useSphereCast)
        {
            // SphereCast for better coverage
            RaycastHit[] hits = Physics.SphereCastAll(
                cam.transform.position,
                detectionRadius,
                direction.normalized,
                distance,
                obstacleLayer
            );
            
            foreach (RaycastHit hit in hits)
            {
                ProcessHit(hit.collider);
            }
        }
        else
        {
            // Simple raycast
            RaycastHit[] hits = Physics.RaycastAll(
                cam.transform.position,
                direction.normalized,
                distance,
                obstacleLayer
            );
            
            foreach (RaycastHit hit in hits)
            {
                ProcessHit(hit.collider);
            }
        }
    }
    
    void ProcessHit(Collider collider)
    {
        // Skip player
        if (collider.transform == player || collider.transform.IsChildOf(player))
            return;
        
        // Get renderer
        Renderer renderer = collider.GetComponent<Renderer>();
        if (renderer == null)
            renderer = collider.GetComponentInChildren<Renderer>();
        
        if (renderer != null && renderer.enabled)
        {
            currentObstacles.Add(renderer);
            
            // Initialize if new
            if (!modifiedRenderers.ContainsKey(renderer))
            {
                modifiedRenderers[renderer] = new RendererInfo(renderer);
            }
        }
    }
    
    void UpdateRenderers()
    {
        List<Renderer> toRemove = new List<Renderer>();
        
        foreach (var kvp in modifiedRenderers)
        {
            Renderer renderer = kvp.Key;
            RendererInfo info = kvp.Value;
            
            if (renderer == null)
            {
                toRemove.Add(renderer);
                continue;
            }
            
            bool isObstacle = currentObstacles.Contains(renderer);
            float targetAlpha = isObstacle ? transparencyLevel : 1f;
            
            // Smooth transition
            info.currentAlpha = Mathf.Lerp(info.currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            
            // Apply alpha to materials
            ApplyAlpha(renderer, info);
            
            // Mark for removal if fully opaque and not an obstacle
            if (!isObstacle && Mathf.Approximately(info.currentAlpha, 1f))
            {
                info.needsRestore = true;
                toRemove.Add(renderer);
            }
        }
        
        // Clean up and restore
        foreach (Renderer renderer in toRemove)
        {
            if (modifiedRenderers.TryGetValue(renderer, out RendererInfo info))
            {
                RestoreRenderer(renderer, info);
                modifiedRenderers.Remove(renderer);
            }
        }
    }
    
    void ApplyAlpha(Renderer renderer, RendererInfo info)
    {
        // Get current materials (not shared to avoid affecting other objects)
        Material[] materials = renderer.materials;
        
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat != null && info.originalColors.ContainsKey(i))
            {
                Color originalColor = info.originalColors[i];
                Color newColor = originalColor;
                newColor.a = originalColor.a * info.currentAlpha;
                
                // Apply to appropriate color property
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", newColor);
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", newColor);
                }
                
                // Enable transparency keywords if needed
                if (info.currentAlpha < 0.99f)
                {
                    // Try to enable alpha blending
                    if (mat.HasProperty("_Mode"))
                    {
                        mat.SetFloat("_Mode", 3); // Transparent
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.renderQueue = 3000;
                    }
                }
            }
        }
        
        // Apply back to renderer
        renderer.materials = materials;
    }
    
    void RestoreRenderer(Renderer renderer, RendererInfo info)
    {
        if (renderer == null) return;
        
        // Get materials
        Material[] materials = renderer.materials;
        
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat != null && info.originalColors.ContainsKey(i))
            {
                Color originalColor = info.originalColors[i];
                
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", originalColor);
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", originalColor);
                }
                
                // Try to restore opaque mode
                if (mat.HasProperty("_Mode"))
                {
                    mat.SetFloat("_Mode", 0); // Opaque
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = -1;
                }
            }
        }
        
        renderer.materials = materials;
    }
    
    void OnDisable()
    {
        // Restore all renderers
        foreach (var kvp in modifiedRenderers)
        {
            RestoreRenderer(kvp.Key, kvp.Value);
        }
        modifiedRenderers.Clear();
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null || cam == null) return;
        
        // Draw detection line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cam.transform.position, player.position);
        
        if (useSphereCast)
        {
            // Draw sphere cast visualization
            Vector3 direction = (player.position - cam.transform.position).normalized;
            float distance = Vector3.Distance(cam.transform.position, player.position);
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                Vector3 pos = cam.transform.position + direction * (distance * t);
                Gizmos.DrawWireSphere(pos, detectionRadius);
            }
        }
    }
}
