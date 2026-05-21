/// <summary>
/// Provider IA cloud : l'API OpenAI (chat completions). C'est le mode « Cloud »
/// du jeu — qualité maximale, nécessite une clé API.
///
/// Toute la logique HTTP vit dans <see cref="OpenAICompatibleProvider"/> ; cette
/// classe n'est qu'un préréglage (URL + modèle par défaut).
/// </summary>
public class OpenAIProvider : OpenAICompatibleProvider
{
    /// <summary>
    /// Modèle cloud par défaut. gpt-4o-mini : moins cher ET de meilleure qualité
    /// que l'ancien gpt-3.5-turbo (cf. SPEC_LLM_local.md, « Gain rapide »).
    /// </summary>
    public const string DefaultOpenAIModel = "gpt-4o-mini";

    public OpenAIProvider(string apiKey)
        : base("OpenAI (Cloud)",
               "https://api.openai.com/v1/chat/completions",
               apiKey,
               DefaultOpenAIModel,
               requiresApiKey: true)
    {
    }
}
