using UnityEngine;

/// <summary>
/// Simple terrain cutout effect for URP
/// Creates a visual indicator when player is behind terrain
/// </summary>
[RequireComponent(typeof(Camera))]
public class SimpleTerrainCutout : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Terrain targetTerrain;
    
    [Header("Cutout Settings")]
    [Range(2f, 15f)]
    public float cutoutRadius = 5f;
    
    [Range(0.1f, 0.9f)]
    public float cutoutOpacity = 0.3f;
    
    [Header("Visual")]
    public Color cutoutColor = new Color(0.5f, 0.8f, 1f, 1f);
    public bool pulseEffect = true;
    [Range(0.5f, 3f)]
    public float pulseSpeed = 1f;
    [Tooltip("Use gradient texture for better visual")]
    public bool useGradientTexture = true;
    
    [Header("Performance")]
    public float updateInterval = 0.05f;
    
    [Header("Debug")]
    public bool debugMode = false;
    public bool alwaysShow = false;
    
    // Private
    private Camera cam;
    private GameObject cutoutVisual;
    private MeshRenderer cutoutRenderer;
    private Material cutoutMaterial;
    private float nextUpdate;
    private bool isShowing = false;
    private Vector3 currentPosition;
    private float currentAlpha = 0f;
    private Texture2D gradientTexture;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        
        // Auto-detect
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("[SimpleCutout] Found player: " + playerObj.name);
            }
        }
        
        if (targetTerrain == null)
        {
            targetTerrain = Terrain.activeTerrain;
            if (targetTerrain != null)
            {
                Debug.Log("[SimpleCutout] Found terrain: " + targetTerrain.name);
            }
        }
        
        if (player == null || targetTerrain == null)
        {
            Debug.LogError("[SimpleCutout] Missing player or terrain reference!");
            enabled = false;
            return;
        }
        
        CreateCutoutVisual();
    }
    
    void CreateCutoutVisual()
    {
        // Create a simple quad instead of cylinder
        cutoutVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cutoutVisual.name = "TerrainCutoutVisual";
        cutoutVisual.layer = LayerMask.NameToLayer("TransparentFX"); // Important for no shadows
        
        // Remove collider
        Destroy(cutoutVisual.GetComponent<Collider>());
        
        // Setup renderer
        cutoutRenderer = cutoutVisual.GetComponent<MeshRenderer>();
        
        // IMPORTANT: Disable shadows
        cutoutRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        cutoutRenderer.receiveShadows = false;
        
        // Try to find the best shader for transparency
        Shader transparentShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (transparentShader == null)
        {
            transparentShader = Shader.Find("Sprites/Default");
        }
        if (transparentShader == null)
        {
            transparentShader = Shader.Find("Unlit/Transparent");
        }
        
        Material mat = new Material(transparentShader);
        
        // Configure material for proper transparency
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        
        // Create gradient texture if needed
        if (useGradientTexture)
        {
            gradientTexture = CreateGradientTexture();
            mat.mainTexture = gradientTexture;
            mat.SetTexture("_BaseMap", gradientTexture);
            mat.SetTexture("_MainTex", gradientTexture);
        }
        
        // Set color with transparency
        Color color = cutoutColor;
        color.a = cutoutOpacity;
        mat.color = color;
        
        // Also try to set these properties in case they exist
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Mode", 2); // Fade mode
        mat.SetFloat("_Surface", 1); // Transparent surface
        
        cutoutMaterial = mat;
        cutoutRenderer.material = cutoutMaterial;
        
        // Rotate to lay flat on ground
        cutoutVisual.transform.rotation = Quaternion.Euler(90, 0, 0);
        
        // Scale it
        cutoutVisual.transform.localScale = new Vector3(cutoutRadius * 2f, cutoutRadius * 2f, 1f);
        
        // Hide initially
        cutoutVisual.SetActive(false);
        
        Debug.Log($"[SimpleCutout] Created visual with shader: {transparentShader.name}");
    }
    
    Texture2D CreateGradientTexture()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center) / (size * 0.5f);
                distance = Mathf.Clamp01(distance);
                
                // Create smooth gradient
                float alpha = 1f - distance;
                alpha = Mathf.Pow(alpha, 2f); // Smooth falloff
                
                // Add edge glow
                float edgeGlow = 1f - Mathf.Abs(distance - 0.8f) * 5f;
                edgeGlow = Mathf.Clamp01(edgeGlow) * 0.3f;
                alpha = Mathf.Max(alpha, edgeGlow);
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }
    
    void Update()
    {
        if (Time.time < nextUpdate) return;
        nextUpdate = Time.time + updateInterval;
        
        // Check if should show
        bool shouldShow = alwaysShow || CheckIfBehindTerrain();
        
        // Fade in/out
        float targetAlpha = shouldShow ? cutoutOpacity : 0f;
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 5f);
        
        // Update visual
        if (currentAlpha > 0.01f)
        {
            if (!isShowing)
            {
                cutoutVisual.SetActive(true);
                isShowing = true;
            }
            
            // Update position
            UpdateCutoutPosition();
            
            // Update alpha with pulse effect
            Color color = cutoutColor;
            
            if (pulseEffect && shouldShow)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
                color.a = currentAlpha * (0.5f + pulse * 0.5f);
            }
            else
            {
                color.a = currentAlpha;
            }
            
            // Apply color to material
            cutoutMaterial.color = color;
            cutoutMaterial.SetColor("_BaseColor", color);
            cutoutMaterial.SetColor("_Color", color);
        }
        else if (isShowing)
        {
            cutoutVisual.SetActive(false);
            isShowing = false;
        }
    }
    
    bool CheckIfBehindTerrain()
    {
        Vector3 camPos = cam.transform.position;
        Vector3 playerPos = player.position + Vector3.up;
        Vector3 direction = playerPos - camPos;
        
        RaycastHit hit;
        if (Physics.Raycast(camPos, direction.normalized, out hit, direction.magnitude))
        {
            if (hit.collider && hit.collider.gameObject == targetTerrain.gameObject)
            {
                currentPosition = hit.point;
                
                if (debugMode)
                {
                    Debug.DrawLine(camPos, hit.point, Color.red, updateInterval);
                    Debug.DrawLine(hit.point, playerPos, Color.yellow, updateInterval);
                }
                
                return true;
            }
        }
        
        if (debugMode)
        {
            Debug.DrawLine(camPos, playerPos, Color.green, updateInterval);
        }
        
        return false;
    }
    
    void UpdateCutoutPosition()
    {
        if (cutoutVisual == null) return;
        
        // Position at terrain height
        float height = targetTerrain.SampleHeight(currentPosition) + 0.5f;
        Vector3 pos = currentPosition;
        pos.y = height;
        
        cutoutVisual.transform.position = pos;
        
        // Keep it flat on ground
        cutoutVisual.transform.rotation = Quaternion.Euler(90, 0, 0);
        
        // Update scale
        float scale = cutoutRadius * 2f;
        cutoutVisual.transform.localScale = new Vector3(scale, scale, 1f);
    }
    
    void OnDestroy()
    {
        if (cutoutVisual != null)
            DestroyImmediate(cutoutVisual);
        
        if (cutoutMaterial != null)
            DestroyImmediate(cutoutMaterial);
            
        if (gradientTexture != null)
            DestroyImmediate(gradientTexture);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!debugMode) return;
        
        if (player != null && cam != null)
        {
            Gizmos.color = isShowing ? Color.red : Color.green;
            Gizmos.DrawLine(cam.transform.position, player.position);
        }
        
        if (isShowing)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(currentPosition, cutoutRadius);
        }
    }
}