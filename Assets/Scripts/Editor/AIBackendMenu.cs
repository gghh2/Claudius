using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu éditeur pour choisir le backend IA en développement (Cloud / Ollama).
///
/// Écrit la préférence lue par <see cref="AIService"/>. À régler AVANT d'entrer
/// en Play. La coche indique le backend actif.
///
/// Provisoire : sera remplacé par le sélecteur in-game des Options (Phase 3).
/// </summary>
public static class AIBackendMenu
{
    const string Cloud = "Tools/Claudius/IA/Backend : Cloud (OpenAI)";
    const string Ollama = "Tools/Claudius/IA/Backend : Local (Ollama)";

    [MenuItem(Cloud)]
    static void SetCloud() => SetBackend(AIService.BackendCloud, "Cloud (OpenAI)");

    [MenuItem(Cloud, true)]
    static bool ValidateCloud()
    {
        Menu.SetChecked(Cloud, Current() == AIService.BackendCloud);
        return true;
    }

    [MenuItem(Ollama)]
    static void SetOllama() => SetBackend(AIService.BackendOllama, "Local (Ollama)");

    [MenuItem(Ollama, true)]
    static bool ValidateOllama()
    {
        Menu.SetChecked(Ollama, Current() == AIService.BackendOllama);
        return true;
    }

    static string Current()
    {
        return PlayerPrefs.GetString(AIService.BackendPrefKey, AIService.BackendCloud);
    }

    static void SetBackend(string backend, string label)
    {
        PlayerPrefs.SetString(AIService.BackendPrefKey, backend);
        PlayerPrefs.Save();
        AIService.Refresh();
        Debug.Log($"[IA] Backend → {label}");
    }
}
