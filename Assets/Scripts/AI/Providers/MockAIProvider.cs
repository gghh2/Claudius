using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Faux provider IA : renvoie des réponses scriptées, sans appel réseau.
///
/// Sert à tester la logique du jeu (parsing des tokens de quête, journal,
/// flux de dialogue) de façon DÉTERMINISTE — sans coût API et sans l'aléa
/// d'un vrai LLM. Pour l'activer : AIService.Provider = new MockAIProvider { ... };
/// </summary>
public class MockAIProvider : IAIProvider
{
    /// <summary>Réponse renvoyée à chaque appel. Modifiable pour scripter un cas de test.</summary>
    public string ScriptedResponse =
        "Bonjour, voyageur. Je n'ai rien de particulier à te demander pour l'instant.";

    /// <summary>Latence simulée (secondes) pour imiter un appel réseau.</summary>
    public float SimulatedDelay = 0.2f;

    public string DisplayName => "Mock (test)";

    public bool IsConfigured => true;

    public IEnumerator Complete(AIRequest request, Action<AIResponse> onComplete)
    {
        // WaitForSecondsRealtime : insensible à Time.timeScale (les panneaux UI
        // mettent le jeu en pause, timeScale = 0).
        if (SimulatedDelay > 0f)
            yield return new WaitForSecondsRealtime(SimulatedDelay);

        onComplete?.Invoke(AIResponse.Ok(ScriptedResponse));
    }
}
