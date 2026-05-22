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
    /// Modèle cloud par défaut. gpt-5.4-mini : palier rapide et économique de la
    /// génération GPT-5.4, adapté au dialogue PNJ (nombreux appels) tout en
    /// gardant une bonne qualité d'écriture. Alias glissant (toujours un id
    /// valide) ; on pourra épingler un snapshot daté plus tard. L'ancien
    /// gpt-4o-mini a été retiré côté OpenAI.
    /// </summary>
    public const string DefaultOpenAIModel = "gpt-5.4-mini";

    public OpenAIProvider(string apiKey)
        : base("OpenAI (Cloud)",
               "https://api.openai.com/v1/chat/completions",
               apiKey,
               DefaultOpenAIModel,
               requiresApiKey: true)
    {
    }
}
