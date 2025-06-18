using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

/// <summary>
/// Assistant pour la conversion vers URP
/// </summary>
public class URPConversionHelper : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/URP Conversion Helper")]
    public static void ShowConversionWindow()
    {
        URPConversionWindow.ShowWindow();
    }
}

public class URPConversionWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private bool[] stepCompleted = new bool[8];
    
    public static void ShowWindow()
    {
        var window = GetWindow<URPConversionWindow>("URP Conversion Helper");
        window.minSize = new Vector2(500, 600);
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Assistant de Conversion URP", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "Cet assistant vous guide dans la conversion de votre projet vers URP.\n" +
            "IMPORTANT : Sauvegardez votre projet avant de commencer !", 
            MessageType.Warning);
        
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Étape 1 : Vérification
        DrawStep(0, "Vérifier l'état actuel", () =>
        {
            CheckCurrentPipeline();
        });
        
        // Étape 2 : Installation
        DrawStep(1, "Installer URP Package", () =>
        {
            Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest");
            EditorUtility.DisplayDialog("Package Manager", 
                "1. Ouvrez Window → Package Manager\n" +
                "2. Sélectionnez 'Unity Registry'\n" +
                "3. Recherchez 'Universal RP'\n" +
                "4. Cliquez sur Install", 
                "Compris");
        });
        
        // Étape 3 : Créer Pipeline Asset
        DrawStep(2, "Créer URP Pipeline Asset", () =>
        {
            CreateURPAsset();
        });
        
        // Étape 4 : Assigner Pipeline
        DrawStep(3, "Assigner le Pipeline", () =>
        {
            AssignPipeline();
        });
        
        // Étape 5 : Convertir Matériaux
        DrawStep(4, "Convertir les Matériaux", () =>
        {
            ConvertMaterials();
        });
        
        // Étape 6 : Setup Post-Processing
        DrawStep(5, "Configurer Post-Processing", () =>
        {
            SetupPostProcessing();
        });
        
        // Étape 7 : Ajuster Lighting
        DrawStep(6, "Ajuster l'Éclairage", () =>
        {
            AdjustLighting();
        });
        
        // Étape 8 : Vérification finale
        DrawStep(7, "Vérification Finale", () =>
        {
            FinalCheck();
        });
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        // Bouton de reset
        if (GUILayout.Button("Réinitialiser les étapes"))
        {
            stepCompleted = new bool[8];
        }
    }
    
    void DrawStep(int index, string title, System.Action action)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        
        // Checkbox
        stepCompleted[index] = EditorGUILayout.Toggle(stepCompleted[index], GUILayout.Width(20));
        
        // Titre
        EditorGUILayout.LabelField($"Étape {index + 1}: {title}", 
            stepCompleted[index] ? EditorStyles.boldLabel : EditorStyles.label);
        
        // Bouton
        if (GUILayout.Button("Exécuter", GUILayout.Width(80)))
        {
            action?.Invoke();
            stepCompleted[index] = true;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    void CheckCurrentPipeline()
    {
        string message = "Pipeline actuel : ";
        
        if (GraphicsSettings.currentRenderPipeline == null)
        {
            message += "Built-in Render Pipeline";
        }
        else
        {
            message += GraphicsSettings.currentRenderPipeline.GetType().Name;
        }
        
        EditorUtility.DisplayDialog("État du Pipeline", message, "OK");
    }
    
    void CreateURPAsset()
    {
        // Créer le dossier Settings s'il n'existe pas
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }
        
        // Menu pour créer l'asset
        EditorUtility.DisplayDialog("Créer URP Asset",
            "1. Dans le Project, clic droit\n" +
            "2. Create → Rendering → URP Asset (with Universal Renderer)\n" +
            "3. Nommez-le 'UniversalRenderPipelineAsset'\n" +
            "4. Placez-le dans le dossier Assets/Settings",
            "Compris");
    }
    
    void AssignPipeline()
    {
        // Chercher l'asset URP
        string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
            
            if (urpAsset != null)
            {
                GraphicsSettings.defaultRenderPipeline = urpAsset;
                QualitySettings.renderPipeline = urpAsset;
                
                EditorUtility.DisplayDialog("Succès", 
                    "Pipeline URP assigné avec succès !\n" +
                    "Les shaders vont être recompilés...", 
                    "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Erreur", 
                "Aucun URP Asset trouvé. Créez-en un d'abord.", 
                "OK");
        }
    }
    
    void ConvertMaterials()
    {
        if (EditorUtility.DisplayDialog("Convertir les Matériaux",
            "Voulez-vous convertir tous les matériaux du projet vers URP ?\n\n" +
            "Cela va modifier tous vos matériaux !",
            "Convertir", "Annuler"))
        {
            // Utiliser le convertisseur built-in d'Unity
            BuiltInToURPMaterialConverter.Convert();
            
            EditorUtility.DisplayDialog("Conversion",
                "Conversion lancée. Vérifiez la console pour les détails.",
                "OK");
        }
    }
    
    void SetupPostProcessing()
    {
        EditorUtility.DisplayDialog("Configuration Post-Processing",
            "Pour configurer le post-processing :\n\n" +
            "1. GameObject → Volume → Global Volume\n" +
            "2. Créez un nouveau Profile\n" +
            "3. Add Override → Post-processing → [Effet]\n\n" +
            "Sur la caméra :\n" +
            "- Rendering → Post Processing : ✓",
            "Compris");
        
        // Créer automatiquement un Global Volume
        if (EditorUtility.DisplayDialog("Créer Volume",
            "Voulez-vous créer un Global Volume maintenant ?",
            "Créer", "Non"))
        {
            GameObject volume = new GameObject("Global Volume");
            volume.AddComponent<Volume>().isGlobal = true;
            Selection.activeGameObject = volume;
        }
    }
    
    void AdjustLighting()
    {
        // Trouver toutes les lumières
        Light[] lights = FindObjectsOfType<Light>();
        
        string message = $"Trouvé {lights.Length} lumière(s).\n\n";
        message += "URP gère l'éclairage différemment :\n";
        message += "• Divisez l'intensité par 2 généralement\n";
        message += "• Configurez 'Additional Lights' dans l'URP Asset\n\n";
        message += "Ajuster automatiquement l'intensité ?";
        
        if (EditorUtility.DisplayDialog("Ajustement Éclairage", message, "Ajuster", "Non"))
        {
            foreach (var light in lights)
            {
                light.intensity *= 0.5f;
                EditorUtility.SetDirty(light);
            }
            
            EditorUtility.DisplayDialog("Fait", 
                "Intensité des lumières divisée par 2.", 
                "OK");
        }
    }
    
    void FinalCheck()
    {
        string report = "=== Rapport de Conversion ===\n\n";
        
        // Pipeline
        bool isURP = GraphicsSettings.currentRenderPipeline != null && 
                     GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
        report += $"✓ Pipeline URP actif : {(isURP ? "OUI" : "NON")}\n";
        
        // Matériaux
        Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
        int urpMaterials = 0;
        foreach (var mat in materials)
        {
            if (mat.shader != null && mat.shader.name.Contains("Universal"))
                urpMaterials++;
        }
        report += $"✓ Matériaux URP : {urpMaterials}/{materials.Length}\n";
        
        // Caméras
        Camera[] cameras = FindObjectsOfType<Camera>();
        report += $"✓ Caméras trouvées : {cameras.Length}\n";
        
        // Volumes
        Volume[] volumes = FindObjectsOfType<Volume>();
        report += $"✓ Volumes post-process : {volumes.Length}\n";
        
        EditorUtility.DisplayDialog("Vérification Finale", report, "OK");
    }
}

// Classe helper pour la conversion des matériaux
public static class BuiltInToURPMaterialConverter
{
    public static void Convert()
    {
        // Cette méthode utilise l'API de conversion d'Unity
        Debug.Log("Début de la conversion des matériaux vers URP...");
        
        // Appeler le menu de conversion d'Unity
        EditorApplication.ExecuteMenuItem("Edit/Rendering/Materials/Convert Selected Built-in Materials to URP");
    }
}
#endif
