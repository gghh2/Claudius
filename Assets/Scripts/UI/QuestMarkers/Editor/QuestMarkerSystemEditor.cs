using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(QuestMarkerSystem))]
public class QuestMarkerSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        QuestMarkerSystem system = (QuestMarkerSystem)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quest Marker System", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Configuration section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        
        system.config = (QuestMarkerConfig)EditorGUILayout.ObjectField(
            "Config Asset", 
            system.config, 
            typeof(QuestMarkerConfig), 
            false
        );
        
        if (system.config == null)
        {
            EditorGUILayout.HelpBox(
                "No configuration asset assigned!\n" +
                "Create one: Right Click → Create → Quest System → Quest Marker Configuration\n" +
                "Then place it in a 'Resources' folder or assign it here.",
                MessageType.Warning
            );
            
            if (GUILayout.Button("Create Configuration Asset"))
            {
                CreateConfigAsset();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Configuration loaded from asset. Changes in the asset will be applied on play.",
                MessageType.Info
            );
            
            if (GUILayout.Button("Edit Configuration"))
            {
                Selection.activeObject = system.config;
            }
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Runtime controls
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Reload Configuration"))
            {
                system.ReloadConfiguration();
            }
            
            if (GUILayout.Button("Refresh All Markers"))
            {
                system.RefreshMarkers();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Default inspector
        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
    
    void CreateConfigAsset()
    {
        // Create the asset
        QuestMarkerConfig config = ScriptableObject.CreateInstance<QuestMarkerConfig>();
        
        // Create Resources folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        
        // Save the asset
        string path = "Assets/Resources/QuestMarkerConfig.asset";
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select it
        Selection.activeObject = config;
        
        Debug.Log($"[QuestMarkerSystem] Created configuration asset at: {path}");
    }
}
