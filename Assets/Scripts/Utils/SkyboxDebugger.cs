using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Test et diagnostic avancé pour les problèmes de skybox
/// </summary>
public class SkyboxDebugger : MonoBehaviour
{
    [Header("Tests")]
    public bool testDefaultSkybox = false;
    public bool testProceduralSkybox = false;
    public bool testSolidColor = false;
    public bool showSkyboxInfo = false;
    
    [Header("Fallback")]
    public Color solidColorFallback = new Color(0.2f, 0.3f, 0.5f);
    
    private Material originalSkybox;
    private CameraClearFlags originalClearFlags;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main ?? GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Aucune caméra trouvée !");
            return;
        }
        
        // Sauvegarde les paramètres originaux
        originalSkybox = RenderSettings.skybox;
        originalClearFlags = mainCamera.clearFlags;
        
        DiagnoseSkybox();
    }
    
    void DiagnoseSkybox()
    {
        Debug.Log("=== 🌌 DIAGNOSTIC SKYBOX AVANCÉ ===");
        
        // Info sur la skybox actuelle
        if (RenderSettings.skybox != null)
        {
            Material skybox = RenderSettings.skybox;
            Debug.Log($"📦 Skybox Material: {skybox.name}");
            Debug.Log($"🎨 Shader: {skybox.shader.name}");
            
            // Vérifie les textures
            string[] textureProperties = { "_Tex", "_MainTex", "_FrontTex", "_BackTex", 
                                         "_LeftTex", "_RightTex", "_UpTex", "_DownTex" };
            
            foreach (string prop in textureProperties)
            {
                if (skybox.HasProperty(prop))
                {
                    Texture tex = skybox.GetTexture(prop);
                    if (tex != null)
                    {
                        Debug.Log($"  ✅ {prop}: {tex.name} ({tex.width}x{tex.height})");
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ {prop}: NULL");
                    }
                }
            }
            
            // Vérifie le render queue
            Debug.Log($"📊 Render Queue: {skybox.renderQueue}");
            
            // Vérifie si le shader existe
            if (skybox.shader == null)
            {
                Debug.LogError("❌ SHADER NULL !");
            }
            else if (skybox.shader.name.Contains("Hidden/"))
            {
                Debug.LogError("❌ Shader caché/erreur !");
            }
        }
        else
        {
            Debug.LogError("❌ Aucune skybox assignée dans RenderSettings !");
        }
        
        // Info sur la caméra
        Debug.Log($"\n📷 Camera: {mainCamera.name}");
        Debug.Log($"  - Clear Flags: {mainCamera.clearFlags}");
        Debug.Log($"  - Culling Mask: {mainCamera.cullingMask}");
        Debug.Log($"  - Background: {mainCamera.backgroundColor}");
        
        // Info sur le rendering
        Debug.Log($"\n🎮 Rendering:");
        Debug.Log($"  - Color Space: {QualitySettings.activeColorSpace}");
        Debug.Log($"  - Render Pipeline: {GraphicsSettings.renderPipelineAsset?.name ?? "Built-in"}");
        
        Debug.Log("=== FIN DIAGNOSTIC ===\n");
    }
    
    void Update()
    {
        // Test skybox par défaut Unity
        if (testDefaultSkybox)
        {
            testDefaultSkybox = false;
            TestDefaultSkybox();
        }
        
        // Test skybox procédurale
        if (testProceduralSkybox)
        {
            testProceduralSkybox = false;
            TestProceduralSkybox();
        }
        
        // Test solid color
        if (testSolidColor)
        {
            testSolidColor = false;
            TestSolidColor();
        }
        
        // Affiche les infos
        if (showSkyboxInfo && Input.GetKeyDown(KeyCode.F9))
        {
            DiagnoseSkybox();
        }
    }
    
    void TestDefaultSkybox()
    {
        Debug.Log("🧪 Test avec Default-Skybox...");
        
        // Cherche la skybox par défaut d'Unity
        Material defaultSkybox = Resources.Load<Material>("Default-Skybox");
        if (defaultSkybox == null)
        {
            // Essaie de la créer
            Shader skyboxShader = Shader.Find("Skybox/Procedural");
            if (skyboxShader != null)
            {
                defaultSkybox = new Material(skyboxShader);
                defaultSkybox.name = "Test_ProceduralSkybox";
            }
        }
        
        if (defaultSkybox != null)
        {
            RenderSettings.skybox = defaultSkybox;
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            DynamicGI.UpdateEnvironment();
            Debug.Log("✅ Skybox par défaut appliquée");
        }
        else
        {
            Debug.LogError("❌ Impossible de charger la skybox par défaut");
        }
    }
    
    void TestProceduralSkybox()
    {
        Debug.Log("🧪 Test avec skybox procédurale...");
        
        Shader skyboxShader = Shader.Find("Skybox/Procedural");
        if (skyboxShader == null)
        {
            Debug.LogError("❌ Shader 'Skybox/Procedural' introuvable !");
            return;
        }
        
        Material proceduralSkybox = new Material(skyboxShader);
        proceduralSkybox.name = "Test_ProceduralSkybox";
        
        // Paramètres pour un ciel spatial
        proceduralSkybox.SetFloat("_SunSize", 0.02f);
        proceduralSkybox.SetFloat("_SunSizeConvergence", 5f);
        proceduralSkybox.SetFloat("_AtmosphereThickness", 0.1f);
        proceduralSkybox.SetColor("_SkyTint", new Color(0.1f, 0.1f, 0.2f)); // Bleu foncé
        proceduralSkybox.SetColor("_GroundColor", new Color(0.05f, 0.05f, 0.1f)); // Très sombre
        proceduralSkybox.SetFloat("_Exposure", 0.3f); // Sombre pour l'espace
        
        RenderSettings.skybox = proceduralSkybox;
        mainCamera.clearFlags = CameraClearFlags.Skybox;
        DynamicGI.UpdateEnvironment();
        
        Debug.Log("✅ Skybox procédurale spatiale appliquée");
    }
    
    void TestSolidColor()
    {
        Debug.Log("🧪 Test avec couleur unie...");
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = solidColorFallback;
        Debug.Log("✅ Couleur unie appliquée");
    }
    
    [ContextMenu("Reset to Original")]
    void ResetToOriginal()
    {
        if (originalSkybox != null)
        {
            RenderSettings.skybox = originalSkybox;
        }
        if (mainCamera != null)
        {
            mainCamera.clearFlags = originalClearFlags;
        }
        DynamicGI.UpdateEnvironment();
        Debug.Log("🔄 Paramètres originaux restaurés");
    }
    
    [ContextMenu("Force Create Simple Skybox")]
    void ForceCreateSimpleSkybox()
    {
        // Crée une skybox 6-sided simple avec des couleurs
        Shader skyboxShader = Shader.Find("Skybox/6 Sided");
        if (skyboxShader == null)
        {
            Debug.LogError("Shader Skybox/6 Sided introuvable !");
            return;
        }
        
        Material simpleSkybox = new Material(skyboxShader);
        simpleSkybox.name = "SimpleSkybox_Generated";
        
        // Crée une texture simple de couleur
        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++)
        {
            colors[i] = new Color(0.1f, 0.1f, 0.3f); // Bleu foncé
        }
        tex.SetPixels(colors);
        tex.Apply();
        
        // Applique la texture sur toutes les faces
        simpleSkybox.SetTexture("_FrontTex", tex);
        simpleSkybox.SetTexture("_BackTex", tex);
        simpleSkybox.SetTexture("_LeftTex", tex);
        simpleSkybox.SetTexture("_RightTex", tex);
        simpleSkybox.SetTexture("_UpTex", tex);
        simpleSkybox.SetTexture("_DownTex", tex);
        
        RenderSettings.skybox = simpleSkybox;
        mainCamera.clearFlags = CameraClearFlags.Skybox;
        DynamicGI.UpdateEnvironment();
        
        Debug.Log("✅ Skybox simple créée !");
    }
}
