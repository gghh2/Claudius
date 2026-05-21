using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu éditeur pour consulter le log de debug des missions proposées par les
/// PNJ (fichier écrit par <see cref="MissionProposalLogger"/>).
/// </summary>
public static class MissionLogMenu
{
    [MenuItem("Tools/Claudius/IA/Ouvrir le log des missions")]
    static void OpenLog()
    {
        string path = MissionProposalLogger.FilePath;
        if (File.Exists(path))
            EditorUtility.RevealInFinder(path);
        else
            Debug.Log($"[IA] Aucun log de missions pour l'instant : {path}");
    }

    [MenuItem("Tools/Claudius/IA/Vider le log des missions")]
    static void ClearLog()
    {
        MissionProposalLogger.Clear();
        Debug.Log("[IA] Log des missions vidé.");
    }
}
