using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// Helper to create fast test builds by temporarily disabling shader compilation
/// </summary>
public class FastBuildHelper : EditorWindow
{
    private bool originalFogStripping;
    private bool originalInstancingStripping;
    
    [MenuItem("Build/Fast Test Build Window")]
    public static void ShowWindow()
    {
        GetWindow<FastBuildHelper>("Fast Build Helper");
    }
    
    [MenuItem("Build/Quick Windows Build %&b")] // Ctrl+Alt+B
    public static void QuickBuild()
    {
        Debug.Log("🚀 Starting Quick Build...");
        
        // Save current settings
        var originalFog = UnityEngine.Rendering.GraphicsSettings.fogStripping;
        var originalLightmap = UnityEngine.Rendering.GraphicsSettings.lightmapStripping;
        
        try
        {
            // Apply fast settings
            UnityEngine.Rendering.GraphicsSettings.fogStripping = UnityEngine.Rendering.ShaderStrippingMode.StripAll;
            UnityEngine.Rendering.GraphicsSettings.lightmapStripping = UnityEngine.Rendering.ShaderStrippingMode.StripAll;
            
            // Get scenes from build settings
            string[] scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }
            
            // Build options for fast test
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/QuickTest/Claudius.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | 
                         BuildOptions.ConnectWithProfiler |
                         BuildOptions.AllowDebugging
            };
            
            // Create build directory if it doesn't exist
            string buildDir = System.IO.Path.GetDirectoryName(buildOptions.locationPathName);
            if (!System.IO.Directory.Exists(buildDir))
            {
                System.IO.Directory.CreateDirectory(buildDir);
            }
            
            // Build
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"✅ Build succeeded in {report.summary.totalTime.TotalMinutes:F1} minutes!");
                Debug.Log($"📁 Build location: {buildOptions.locationPathName}");
                
                // Open build folder
                EditorUtility.RevealInFinder(buildOptions.locationPathName);
            }
            else
            {
                Debug.LogError($"❌ Build failed: {report.summary.result}");
            }
        }
        finally
        {
            // Always restore settings
            UnityEngine.Rendering.GraphicsSettings.fogStripping = originalFog;
            UnityEngine.Rendering.GraphicsSettings.lightmapStripping = originalLightmap;
        }
    }
    
    void OnGUI()
    {
        GUILayout.Label("Fast Build Helper", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "This tool temporarily disables shader variants to speed up builds.\n" +
            "Use for testing only - not for final builds!",
            MessageType.Warning
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🚀 Fast Test Build", GUILayout.Height(40)))
        {
            QuickBuild();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Shortcuts:", EditorStyles.boldLabel);
        GUILayout.Label("• Ctrl+Alt+B : Quick Build");
        GUILayout.Label("• This reduces shader compilation from hours to minutes");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Open Build Settings"))
        {
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }
        
        if (GUILayout.Button("Open Graphics Settings"))
        {
            SettingsService.OpenProjectSettings("Project/Graphics");
        }
    }
}