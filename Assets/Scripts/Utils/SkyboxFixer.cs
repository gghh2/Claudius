using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Diagnostique et corrige les problèmes de skybox
/// </summary>
public class SkyboxFixer : MonoBehaviour
{
    [Header("Diagnostic")]
    public bool runDiagnostic = true;
    public bool autoFix = false;
    
    [Header("Fix Options")]
    public Color fallbackColor = new Color(0.5f, 0.7f, 1f, 1f); // Bleu ciel
    public Material defaultSkyboxMaterial;
    
    void Start()
    {
        if (runDiagnostic)
            DiagnoseSkyboxIssues();
            
        if (autoFix)
            AttemptAutoFix();
    }
    
    void DiagnoseSkyboxIssues()
    {
        Debug.Log("=== 🌌 DIAGNOSTIC SKYBOX ===");
        
        // Vérifie les caméras
        Camera[] cameras = FindObjectsOfType<Camera>();
        Debug.Log($"📷 {cameras.Length} caméra(s) trouvée(s)");
        
        foreach (Camera cam in cameras)
        {
            Debug.Log($"\n📷 Caméra: {cam.name}");
            Debug.Log($"  - Clear Flags: {cam.clearFlags}");
            Debug.Log($"  - Background Color: {cam.backgroundColor}");
            Debug.Log($"  - Depth: {cam.depth}");
            
            if (cam.clearFlags == CameraClearFlags.Nothing)
            {
                Debug.LogError($"  ❌ PROBLÈME: Clear Flags sur 'Don't Clear' !");
            }
            else if (cam.clearFlags == CameraClearFlags.Depth && cam.depth == -1)
            {
                Debug.LogWarning($"  ⚠️ Clear Flags sur 'Depth only' pour la caméra principale");
            }
        }
        
        // Vérifie la skybox globale
        Material skybox = RenderSettings.skybox;
        if (skybox == null)
        {
            Debug.LogError("❌ PROBLÈME: Aucune skybox assignée dans RenderSettings !");
        }
        else
        {
            Debug.Log($"\n🌌 Skybox actuelle: {skybox.name}");
            Debug.Log($"  - Shader: {skybox.shader.name}");
        }
        
        // Vérifie l'ambient
        Debug.Log($"\n💡 Paramètres d'ambiance:");
        Debug.Log($"  - Ambient Mode: {RenderSettings.ambientMode}");
        Debug.Log($"  - Ambient Color: {RenderSettings.ambientLight}");
        
        Debug.Log("\n=== FIN DU DIAGNOSTIC ===");
    }
    
    void AttemptAutoFix()
    {
        Debug.Log("\n🔧 Tentative de correction automatique...");
        
        // Trouve la caméra principale
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = FindObjectOfType<Camera>();
        }
        
        if (mainCam != null)
        {
            // Fix les Clear Flags
            if (mainCam.clearFlags == CameraClearFlags.Nothing || 
                mainCam.clearFlags == CameraClearFlags.Depth)
            {
                mainCam.clearFlags = CameraClearFlags.Skybox;
                mainCam.backgroundColor = fallbackColor;
                Debug.Log("✅ Clear Flags corrigés sur 'Skybox'");
            }
            
            // Si pas de skybox, utilise une couleur unie
            if (RenderSettings.skybox == null)
            {
                if (defaultSkyboxMaterial != null)
                {
                    RenderSettings.skybox = defaultSkyboxMaterial;
                    Debug.Log("✅ Skybox par défaut assignée");
                }
                else
                {
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = fallbackColor;
                    Debug.Log("✅ Utilisation d'une couleur unie comme fallback");
                }
            }
        }
        else
        {
            Debug.LogError("❌ Aucune caméra trouvée pour la correction !");
        }
    }
    
    [ContextMenu("Force Fix Skybox")]
    public void ForceFixSkybox()
    {
        Camera mainCam = Camera.main ?? FindObjectOfType<Camera>();
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = fallbackColor;
            Debug.Log("✅ Skybox forcée en couleur unie");
        }
    }
    
    [ContextMenu("Create Procedural Skybox")]
    public void CreateProceduralSkybox()
    {
        // Crée un material de skybox procédural
        Material skyboxMat = new Material(Shader.Find("Skybox/Procedural"));
        skyboxMat.name = "ProceduralSkybox_Generated";
        
        // Configure les paramètres
        skyboxMat.SetFloat("_SunSize", 0.04f);
        skyboxMat.SetFloat("_AtmosphereThickness", 0.7f);
        skyboxMat.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.5f));
        skyboxMat.SetColor("_GroundColor", new Color(0.369f, 0.349f, 0.341f));
        skyboxMat.SetFloat("_Exposure", 0.9f);
        
        // Assigne la skybox
        RenderSettings.skybox = skyboxMat;
        DynamicGI.UpdateEnvironment();
        
        Debug.Log("✅ Skybox procédurale créée et assignée !");
        
        // Met aussi à jour la caméra
        Camera mainCam = Camera.main ?? FindObjectOfType<Camera>();
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.Skybox;
        }
    }
}
