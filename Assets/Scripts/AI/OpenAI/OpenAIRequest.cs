// Assets/Scripts/AI/OpenAI/OpenAIRequest.cs
[System.Serializable]
public class OpenAIRequest
{
    public string model;
    public OpenAIMessage[] messages;
    public float temperature;
    // Les noms de champs sont sérialisés tels quels dans le JSON envoyé à
    // l'API. Les modèles GPT-5.x exigent « max_completion_tokens » et
    // rejettent l'ancien « max_tokens ».
    public int max_completion_tokens;
}