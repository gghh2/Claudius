/// <summary>
/// Point d'accès central au moteur d'IA actif.
///
/// Tout le code de jeu passe par <see cref="Provider"/> au lieu d'appeler un
/// backend en dur. Basculer de moteur (OpenAI cloud, LLM local, mock de test)
/// = réassigner cette propriété, sans toucher au reste du code.
/// </summary>
public static class AIService
{
    private static IAIProvider provider;

    /// <summary>
    /// Le provider actif. Par défaut : OpenAI, clé lue depuis APIConfig.
    /// </summary>
    public static IAIProvider Provider
    {
        get
        {
            if (provider == null)
                provider = new OpenAIProvider(APIConfig.OPENAI_API_KEY);
            return provider;
        }
        set { provider = value; }
    }
}
