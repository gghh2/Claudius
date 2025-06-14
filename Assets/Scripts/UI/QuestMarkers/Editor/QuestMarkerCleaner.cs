using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
/// <summary>
/// Outil de nettoyage pour le système de marqueurs de quête
/// </summary>
public class QuestMarkerCleaner : EditorWindow
{
    private List<string> filesToDelete = new List<string>();
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Quest System/Clean Quest Marker Files")]
    public static void ShowWindow()
    {
        GetWindow<QuestMarkerCleaner>("Quest Marker Cleaner");
    }
    
    void OnEnable()
    {
        FindObsoleteFiles();
    }
    
    void FindObsoleteFiles()
    {
        filesToDelete.Clear();
        
        string[] obsoleteFiles = new string[]
        {
            "UltraSimpleQuestMarker.cs",
            "SimpleQuestMarkerSystem.cs",
            "QuestMarkerUI.cs",
            "QuestMarkerInitializer.cs",
            "QuestMarkerSystemMigration.cs",
            "SimpleMarkerTest.cs",
            "QuestMarkerDiagnostic.cs",
            "QuestMarkerConfig.cs",
            "QuestZoneTagFixer.cs.backup",
            "README_QUEST_MARKERS.txt",
            "README_DEBUG_SOLUTION.md",
            "QuestMarkerSystem_OLD.cs.backup"
        };
        
        string markersPath = "Assets/Scripts/UI/QuestMarkers/";
        
        foreach (string fileName in obsoleteFiles)
        {
            string fullPath = markersPath + fileName;
            if (File.Exists(fullPath))
            {
                filesToDelete.Add(fullPath);
                
                // Vérifie aussi le fichier .meta
                string metaPath = fullPath + ".meta";
                if (File.Exists(metaPath))
                {
                    filesToDelete.Add(metaPath);
                }
            }
        }
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Quest Marker System - Nettoyage", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "Cet outil va supprimer les anciens fichiers du système de marqueurs de quête.\n" +
            "Le nouveau système utilise uniquement QuestMarkerSystem.cs", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (filesToDelete.Count == 0)
        {
            EditorGUILayout.LabelField("Aucun fichier obsolète trouvé !", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Vérifier à nouveau"))
            {
                FindObsoleteFiles();
            }
        }
        else
        {
            EditorGUILayout.LabelField($"Fichiers à supprimer ({filesToDelete.Count}):", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (string file in filesToDelete)
            {
                EditorGUILayout.LabelField("• " + file);
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Supprimer tous les fichiers obsolètes", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirmation",
                    $"Êtes-vous sûr de vouloir supprimer {filesToDelete.Count} fichiers ?\n\n" +
                    "Cette action est irréversible !",
                    "Supprimer", "Annuler"))
                {
                    DeleteObsoleteFiles();
                }
            }
            GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button("Annuler", GUILayout.Height(30), GUILayout.Width(80)))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fichiers conservés:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• QuestMarkerSystem.cs - Le nouveau système unifié");
        EditorGUILayout.LabelField("• QuestMarkerDebugger.cs - Outil de debug (optionnel)");
        EditorGUILayout.LabelField("• UltraSimpleQuestMarkerFixed.cs - Backup (optionnel)");
    }
    
    void DeleteObsoleteFiles()
    {
        int deletedCount = 0;
        
        foreach (string file in filesToDelete)
        {
            try
            {
                File.Delete(file);
                deletedCount++;
                Debug.Log($"[QuestMarkerCleaner] Supprimé: {file}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuestMarkerCleaner] Erreur lors de la suppression de {file}: {e.Message}");
            }
        }
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Nettoyage terminé",
            $"{deletedCount} fichiers supprimés avec succès.\n\n" +
            "N'oubliez pas d'ajouter QuestMarkerSystem sur un GameObject dans votre scène.",
            "OK");
        
        FindObsoleteFiles();
    }
}
#endif
