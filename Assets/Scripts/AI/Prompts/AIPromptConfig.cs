using UnityEngine;

[CreateAssetMenu(fileName = "AIPromptConfig", menuName = "AI/Prompt Configuration")]
public class AIPromptConfig : ScriptableObject
{
    [Header("🎭 Personnalité du PNJ")]  // ← Changé !
    [TextArea(3, 6)]
    public string npcPersonality = "Description de la personnalité spécifique de ce PNJ";
    
    [Header("📜 Instructions Globales")]  // ← Pour TOUS les PNJs
    [TextArea(5, 10)]
    public string globalInstructions = @"INSTRUCTIONS IMPORTANTES:
- Incarnez ce personnage de manière cohérente
- Répondez TOUJOURS en français
- Gardez vos réponses courtes (1-3 phrases maximum)";
    
    [Header("🎯 Système de Quêtes")]
    [TextArea(10, 20)]
    public string questInstructions = "...";
    
    [Header("💬 Dialogues et Exemples Spécifiques")]  // ← Plus clair !
    [TextArea(10, 20)]
    public string roleSpecificExamples = "Exemples de dialogues pour CE rôle particulier";
}