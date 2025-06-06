// Assets/Scripts/AI/OpenAI/OpenAIRequest.cs
[System.Serializable]
public class OpenAIRequest
{
    public string model;
    public OpenAIMessage[] messages;
    public float temperature;
    public int max_tokens;
}