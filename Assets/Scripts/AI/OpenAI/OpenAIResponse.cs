// Assets/Scripts/AI/OpenAI/OpenAIResponse.cs
[System.Serializable]
public class OpenAIResponse
{
    public OpenAIChoice[] choices;
}

[System.Serializable]
public class OpenAIChoice
{
    public OpenAIMessage message;
}