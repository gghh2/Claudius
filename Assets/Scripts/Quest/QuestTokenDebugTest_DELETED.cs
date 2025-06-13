using UnityEngine;
using System.Text.RegularExpressions;

public class QuestTokenDebugTest : MonoBehaviour
{
    [ContextMenu("Test Direct Token")]
    public void TestDirectToken()
    {
        // Message exact des logs
        string testMessage = "[QUEST:FETCH:cargaison_volee:market:1] Bonjour voyageur ! J'ai justement besoin d'un coup de main. Seriez-vous prêt à récupérer une cargaison volée pour moi ?";
        
        Debug.Log("=== TEST DIRECT TOKEN ===");
        Debug.Log($"Message test: {testMessage}");
        
        // Test avec le regex exact
        string pattern = @"\[QUEST:FETCH:([^:]+):([^:]+):?([^\]]*)\]";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
        
        MatchCollection matches = regex.Matches(testMessage);
        Debug.Log($"Matches trouvés: {matches.Count}");
        
        if (matches.Count > 0)
        {
            Match match = matches[0];
            Debug.Log($"Match complet: {match.Value}");
            Debug.Log($"Objet: {match.Groups[1].Value}");
            Debug.Log($"Zone: {match.Groups[2].Value}");
            Debug.Log($"Quantité: {match.Groups[3].Value}");
        }
        
        // Test avec QuestTokenDetector
        if (QuestTokenDetector.Instance != null)
        {
            Debug.Log("\n=== Test avec QuestTokenDetector ===");
            var tokens = QuestTokenDetector.Instance.DetectQuestTokens(testMessage);
            Debug.Log($"Tokens détectés: {tokens.Count}");
        }
    }
    
    [ContextMenu("Test All Patterns")]
    public void TestAllPatterns()
    {
        string[] testMessages = {
            "[QUEST:FETCH:crystal:laboratory:3]",
            "[QUEST:DELIVERY:package:john:hangar]",
            "[QUEST:EXPLORE:ruins]",
            "[QUEST:TALK:scientist:laboratory]",
            "[QUEST:INTERACT:terminal:security]"
        };
        
        foreach (string msg in testMessages)
        {
            Debug.Log($"\nTest: {msg}");
            if (QuestTokenDetector.Instance != null)
            {
                var tokens = QuestTokenDetector.Instance.DetectQuestTokens(msg);
                Debug.Log($"Résultat: {tokens.Count} token(s)");
            }
        }
    }
}
