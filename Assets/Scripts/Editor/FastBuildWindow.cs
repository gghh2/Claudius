using UnityEngine;
using UnityEditor;

/// <summary>
/// Window pour fast build accessible via Window menu
/// </summary>
public class FastBuildWindow : EditorWindow
{
    [MenuItem("Window/Fast Build Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<FastBuildWindow>("Fast Build");
        window.minSize = new Vector2(300, 200);
    }
    
    void OnGUI()
    {
        GUILayout.Label("Fast Build Tool", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Cet outil désactive temporairement les variantes de shaders pour accélérer le build.",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        // Gros bouton
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("BUILD RAPIDE", GUILayout.Height(50)))
        {
            PerformFastBuild();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);
        
        // Build normal
        if (GUILayout.Button("Build Settings (normal)", GUILayout.Height(30)))
        {
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }
    }
    
    void PerformFastBuild()
    {
        Debug.Log("🚀 Démarrage du build rapide...");
        
        // Configuration rapide
        PlayerSettings.SetManagedStrippingLevel(
            BuildTargetGroup.Standalone, 
            UnityEditor.ManagedStrippingLevel.High
        );
        
        // Build
        string path = EditorUtility.SaveFilePanel(
            "Sauvegarder le build",
            "",
            "Claudius",
            "exe"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(
                    EditorBuildSettings.scenes,
                    scene => scene.path
                ),
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            
            BuildPipeline.BuildPlayer(options);
        }
    }
}
