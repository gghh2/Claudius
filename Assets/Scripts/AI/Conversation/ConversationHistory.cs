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