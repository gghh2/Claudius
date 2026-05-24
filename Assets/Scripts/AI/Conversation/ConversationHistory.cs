// Assets/Scripts/AI/Conversation/ConversationHistory.cs
using System.Collections.Generic;

[System.Serializable]
public class ConversationHistory
{
    public string npcName;
    public List<string> messages = new List<string>();
    public bool hasSpokenBefore = false;

    public ConversationHistory()
    {
        messages = new List<string>();
    }
}

[System.Serializable]
public class ConversationContextEntry
{
    public string npcName;
    public List<OpenAIMessage> messages = new List<OpenAIMessage>();
}

[System.Serializable]
public class ConversationsSaveData
{
    public List<ConversationHistory> histories = new List<ConversationHistory>();
    public List<ConversationContextEntry> contexts = new List<ConversationContextEntry>();
}