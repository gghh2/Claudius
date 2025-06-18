using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Outil pour identifier le pipeline de rendu utilisé
/// </summary>
public class RenderPipelineDetector : MonoBehaviour
{
    [Header("Pipeline Info")]
    [SerializeField] private string currentPipeline = "Non détecté";
    [SerializeField] private bool isURP = false;
    [SerializeField] private bool isHDRP = false;
    [SerializeField] private bool isBuiltIn = false;
    
    void Start()
    {
        DetectPipeline();
    }
    
    void DetectPipeline()
    {
        // Méthode 1 : Vérifier le RenderPipelineAsset actuel
        if (GraphicsSettings.currentRenderPipeline == null)
        {
            currentPipeline = "Built-in Render Pipeline (Standard)";
            isBuiltIn = true;
        }
        else
        {
            string pipelineName = GraphicsSettings.currentRenderPipeline.GetType().Name;
            
            if (pipelineName.Contains("UniversalRenderPipelineAsset"))
            {
                currentPipeline = "Universal Render Pipeline (URP)";
                isURP = true;
            }
            else if (pipelineName.Contains("HDRenderPipelineAsset"))
            {
                currentPipeline = "High Definition Render Pipeline (HDRP)";
                isHDRP = true;
            }
            else
            {
                currentPipeline = $"Custom Pipeline: {pipelineName}";
            }
        }
        
        Debug.Log($"🎨 Pipeline de rendu détecté : {currentPipeline}");
        
        // Affiche des infos supplémentaires
        ShowPipelineFeatures();
    }
    
    void ShowPipelineFeatures()
    {
        Debug.Log("=== Caractéristiques du Pipeline ===");
        
        if (isBuiltIn)
        {
            Debug.Log("• Shaders : Standard et Legacy");
            Debug.Log("• Post-Processing : Post Processing Stack v2");
            Debug.Log("• Performance : Moyen");
            Debug.Log("• Compatibilité : Maximum (mobile, PC, consoles)");
        }
        else if (isURP)
        {
            Debug.Log("• Shaders : Lit et Unlit (Shader Graph)");
            Debug.Log("• Post-Processing : Volume system intégré");
            Debug.Log("• Performance : Optimisé (excellent pour mobile)");
            Debug.Log("• Compatibilité : Très bonne");
            Debug.Log("• Features : 2D Renderer, Forward Rendering");
        }
        else if (isHDRP)
        {
            Debug.Log("• Shaders : Lit, Unlit, et avancés (Shader Graph)");
            Debug.Log("• Post-Processing : Volume system avancé");
            Debug.Log("• Performance : Demande des GPU puissants");
            Debug.Log("• Compatibilité : PC et consoles haut de gamme");
            Debug.Log("• Features : Ray Tracing, Advanced lighting");
        }
    }
    
    public bool IsURP() => isURP;
    public bool IsHDRP() => isHDRP;
    public bool IsBuiltIn() => isBuiltIn;
    
#if UNITY_EDITOR
    [ContextMenu("Afficher les infos du pipeline")]
    void ShowPipelineInfo()
    {
        DetectPipeline();
        
        EditorUtility.DisplayDialog("Pipeline de Rendu", 
            $"Votre projet utilise :\n\n{currentPipeline}\n\n" +
            $"URP : {(isURP ? "✓" : "✗")}\n" +
            $"HDRP : {(isHDRP ? "✓" : "✗")}\n" +
            $"Built-in : {(isBuiltIn ? "✓" : "✗")}", 
            "OK");
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(RenderPipelineDetector))]
public class RenderPipelineDetectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Détecter le Pipeline", GUILayout.Height(30)))
        {
            RenderPipelineDetector detector = (RenderPipelineDetector)target;
            detector.SendMessage("ShowPipelineInfo");
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(GetPipelineHelp(), MessageType.Info);
    }
    
    string GetPipelineHelp()
    {
        if (GraphicsSettings.currentRenderPipeline == null)
        {
            return "Built-in RP : Pipeline classique d'Unity. Utilisez Post Processing Stack v2 pour les effets.";
        }
        
        string pipelineName = GraphicsSettings.currentRenderPipeline.GetType().Name;
        
        if (pipelineName.Contains("Universal"))
        {
            return "URP : Pipeline optimisé pour la performance. Utilisez le Volume system pour les effets post-process.";
        }
        else if (pipelineName.Contains("HD"))
        {
            return "HDRP : Pipeline haute qualité pour PC/consoles. Effets avancés disponibles.";
        }
        
        return "Pipeline personnalisé détecté.";
    }
}
#endif
