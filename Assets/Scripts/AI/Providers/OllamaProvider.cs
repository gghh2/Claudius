/// <summary>
/// Provider IA local via Ollama (https://ollama.com).
///
/// Ollama tourne comme service local et expose une API compatible OpenAI sur
/// le port 11434. Il sert de moteur « Local » en développement — et de banc
/// d'essai pour mesurer les petits modèles avant l'intégration embarquée
/// (LLMUnity, Phase 4).
///
/// Prérequis côté machine : Ollama installé, puis <c>ollama pull &lt;modèle&gt;</c>.
/// Toute la logique HTTP vit dans <see cref="OpenAICompatibleProvider"/>.
/// </summary>
public class OllamaProvider : OpenAICompatibleProvider
{
    public const string DefaultEndpoint = "http://localhost:11434/v1/chat/completions";
    public const string DefaultOllamaModel = "qwen2.5:7b";

    public OllamaProvider(string model = DefaultOllamaModel, string endpoint = DefaultEndpoint)
        : base("LLM local (Ollama)",
               endpoint,
               apiKey: null,
               model,
               requiresApiKey: false)
    {
    }
}
