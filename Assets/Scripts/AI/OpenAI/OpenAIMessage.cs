// Assets/Scripts/AI/OpenAI/OpenAIMessage.cs
[System.Serializable]
public class OpenAIMessage
{
    public string role;
    public string content;
    
    public OpenAIMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}