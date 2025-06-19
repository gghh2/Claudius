using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Outil pour optimiser les ombres URP
/// </summary>
public class URPShadowOptimizer : MonoBehaviour
{
    [Header("Diagnostic")]
    [SerializeField] private UniversalRenderPipelineAsset currentURPAsset;
    [SerializeField] private int currentShadowResolution;
    [SerializeField] private float currentShadowDistance;
    
    void Start()
    {
        DiagnoseShadowSettings();
    }
    
    void DiagnoseShadowSettings()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            currentURPAsset = urpAsset;
            // Les propriétés sont cachées, on doit utiliser la réflexion ou les menus
            Debug.Log($"URP Asset trouvé : {urpAsset.name}");
        }
    }
    
#if UNITY_EDITOR
    [ContextMenu("Ouvrir les paramètres d'ombres")]
    void OpenShadowSettings()
    {
        if (currentURPAsset != null)
        {
            Selection.activeObject = currentURPAsset;
            EditorGUIUtility.PingObject(currentURPAsset);
        }
    }
#endif
}

#if UNITY_EDITOR
/// <summary>
/// Fenêtre d'optimisation des ombres URP
/// </summary>
public class URPShadowSettingsWindow : EditorWindow
{
    private UniversalRenderPipelineAsset urpAsset;
    private Vector2 scrollPos;
    
    // Paramètres recommandés
    private readonly (string name, string description, string setting)[] shadowSettings = new[]
    {
        ("Shadow Resolution", "Augmentez pour moins de crénelage", "Lighting → Main Light → Shadow Resolution"),
        ("Shadow Distance", "Réduisez pour plus de détails", "Shadows → Max Distance"),
        ("Shadow Cascades", "4 cascades pour une meilleure qualité", "Shadows → Cascade Count"),
        ("Soft Shadows", "Activez pour des ombres plus douces", "Shadows → Soft Shadows"),
        ("Shadow Bias", "Ajustez si ombres détachées", "Lighting → Main Light → Shadow Near Plane Offset")
    };
    
    [MenuItem("Tools/URP/Shadow Settings Optimizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<URPShadowSettingsWindow>("URP Shadow Optimizer");
        window.minSize = new Vector2(500, 600);
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Optimiseur d'Ombres URP", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Sélection de l'asset URP
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("1. Asset URP actuel", EditorStyles.boldLabel);
        
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Pipeline Asset", urpAsset, typeof(UniversalRenderPipelineAsset), false);
        EditorGUI.EndDisabledGroup();
        
        if (urpAsset == null)
        {
            EditorGUILayout.HelpBox("Aucun URP Asset trouvé !", MessageType.Error);
            return;
        }
        
        if (GUILayout.Button("Sélectionner l'Asset URP"))
        {
            Selection.activeObject = urpAsset;
            EditorGUIUtility.PingObject(urpAsset);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Guide des paramètres
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Paramètres à ajuster", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        foreach (var setting in shadowSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(setting.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(setting.description, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Emplacement :", GUILayout.Width(80));
            EditorGUILayout.LabelField(setting.setting, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Paramètres recommandés
        DrawRecommendedSettings();
        
        // Actions rapides
        DrawQuickActions();
    }
    
    void DrawRecommendedSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("3. Valeurs recommandées", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "Pour des ombres de qualité :\n\n" +
            "• Shadow Resolution : 2048 ou 4096\n" +
            "• Max Distance : 50-150 (selon votre scène)\n" +
            "• Cascade Count : 4 Cascades\n" +
            "• Cascade Split : (0.05, 0.15, 0.3)\n" +
            "• Soft Shadows : ✓ Activé\n" +
            "• Depth Bias : 1.0\n" +
            "• Normal Bias : 0.5", 
            MessageType.Info);
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawQuickActions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions rapides", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Appliquer paramètres Haute Qualité", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirmation", 
                "Ceci va modifier votre URP Asset pour des ombres haute qualité.\n" +
                "Continuez ?", "Oui", "Non"))
            {
                ApplyHighQualitySettings();
            }
        }
        
        if (GUILayout.Button("Appliquer paramètres Équilibrés"))
        {
            if (EditorUtility.DisplayDialog("Confirmation", 
                "Ceci va modifier votre URP Asset pour un bon équilibre qualité/performance.\n" +
                "Continuez ?", "Oui", "Non"))
            {
                ApplyBalancedSettings();
            }
        }
        
        EditorGUILayout.Space();
        
        // Autres optimisations
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Autres optimisations", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Ajuster les lumières de la scène"))
        {
            AdjustSceneLights();
        }
        
        if (GUILayout.Button("Optimiser la caméra"))
        {
            OptimizeCamera();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void ApplyHighQualitySettings()
    {
        // Note : Les propriétés de l'URP Asset sont souvent en lecture seule
        // Il faut les modifier manuellement dans l'Inspector
        
        EditorUtility.DisplayDialog("Instructions", 
            "Sélectionnez votre URP Asset et appliquez ces paramètres :\n\n" +
            "Lighting:\n" +
            "• Main Light → Shadow Resolution : 4096\n\n" +
            "Shadows:\n" +
            "• Max Distance : 100\n" +
            "• Cascade Count : 4 Cascades\n" +
            "• Soft Shadows : ✓\n" +
            "• Conservative Enclosing Sphere : ✓\n\n" +
            "Quality:\n" +
            "• HDR : ✓\n" +
            "• Anti Aliasing : FXAA", 
            "OK");
        
        Selection.activeObject = urpAsset;
    }
    
    void ApplyBalancedSettings()
    {
        EditorUtility.DisplayDialog("Instructions", 
            "Sélectionnez votre URP Asset et appliquez ces paramètres :\n\n" +
            "Lighting:\n" +
            "• Main Light → Shadow Resolution : 2048\n\n" +
            "Shadows:\n" +
            "• Max Distance : 75\n" +
            "• Cascade Count : 2 Cascades\n" +
            "• Soft Shadows : ✓\n\n" +
            "Quality:\n" +
            "• Anti Aliasing : FXAA", 
            "OK");
        
        Selection.activeObject = urpAsset;
    }
    
    void AdjustSceneLights()
    {
        Light[] lights = FindObjectsOfType<Light>();
        int adjusted = 0;
        
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                // Ajuster les bias pour réduire l'acné d'ombre
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
                light.shadowNearPlane = 0.2f;
                
                // Activer les soft shadows
                light.shadows = LightShadows.Soft;
                
                EditorUtility.SetDirty(light);
                adjusted++;
            }
        }
        
        EditorUtility.DisplayDialog("Lumières ajustées", 
            $"{adjusted} lumière(s) directionnelle(s) optimisée(s) pour URP.", 
            "OK");
    }
    
    void OptimizeCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var additionalData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (additionalData == null)
            {
                additionalData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            
            // Activer l'anti-aliasing
            additionalData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            additionalData.antialiasingQuality = AntialiasingQuality.High;
            
            // Activer le post-processing si nécessaire
            additionalData.renderPostProcessing = true;
            
            EditorUtility.SetDirty(additionalData);
            
            EditorUtility.DisplayDialog("Caméra optimisée", 
                "Paramètres de la caméra principale ajustés pour URP.", 
                "OK");
        }
    }
}

/// <summary>
/// Menu contextuel pour les lumières
/// </summary>
[CustomEditor(typeof(Light))]
public class LightShadowHelper : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        Light light = (Light)target;
        
        if (light.type == LightType.Directional)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optimisation Ombres URP", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Appliquer paramètres optimaux"))
            {
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
                light.shadowNearPlane = 0.2f;
                light.shadows = LightShadows.Soft;
                
                EditorUtility.SetDirty(light);
            }
        }
    }
}
#endif

/// <summary>
/// Composant pour tester différents paramètres d'ombres en runtime
/// </summary>
public class ShadowQualityTester : MonoBehaviour
{
    [Header("Test en Runtime")]
    [Range(0.01f, 2f)]
    public float shadowBias = 0.05f;
    
    [Range(0f, 1f)]
    public float shadowNormalBias = 0.4f;
    
    [Range(0.1f, 10f)]
    public float shadowNearPlane = 0.2f;
    
    private Light directionalLight;
    
    void Start()
    {
        directionalLight = GetComponent<Light>();
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
        }
    }
    
    void Update()
    {
        if (directionalLight != null && directionalLight.type == LightType.Directional)
        {
            directionalLight.shadowBias = shadowBias;
            directionalLight.shadowNormalBias = shadowNormalBias;
            directionalLight.shadowNearPlane = shadowNearPlane;
        }
    }
}
