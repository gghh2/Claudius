using UnityEngine;

/// <summary>
/// Point d'accès central au moteur d'IA actif.
///
/// Tout le code de jeu passe par <see cref="Provider"/> au lieu d'appeler un
/// backend en dur. Le provider est résolu à partir du backend choisi (clé
/// PlayerPrefs <see cref="BackendPrefKey"/>) : basculer Cloud / Local ne touche
/// aucun code appelant.
/// </summary>
public static class AIService
{
    /// <summary>Clé PlayerPrefs qui mémorise le backend IA choisi.</summary>
    public const string BackendPrefKey = "ai_backend";

    public const string BackendCloud = "cloud";
    public const string BackendOllama = "ollama";

    private static IAIProvider provider;

    /// <summary>
    /// Le provider actif. Résolu à la demande depuis le backend mémorisé.
    /// Peut aussi être forcé directement (tests : <c>AIService.Provider = new MockAIProvider()</c>).
    /// </summary>
    public static IAIProvider Provider
    {
        get
        {
            if (provider == null)
                provider = Resolve();
            return provider;
        }
        set { provider = value; }
    }

    /// <summary>
    /// Oublie le provider courant : il sera re-résolu au prochain accès.
    /// À appeler après un changement de backend.
    /// </summary>
    public static void Refresh()
    {
        provider = null;
    }

    static IAIProvider Resolve()
    {
        string backend = PlayerPrefs.GetString(BackendPrefKey, BackendCloud);
        switch (backend)
        {
            case BackendOllama:
                return new OllamaProvider();
            default:
                return new OpenAIProvider(APIConfig.OPENAI_API_KEY);
        }
    }
}
