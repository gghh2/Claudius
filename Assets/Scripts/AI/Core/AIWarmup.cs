using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Au lancement du jeu (MainMenu OU Game, peu importe), envoie une requete
/// minimale au provider IA pour amorcer la connexion : negociation TLS,
/// keep-alive HTTP, eventuel cache du modele cote serveur. Resultat : le
/// PREMIER vrai dialogue PNJ ne subit plus la latence de cold-start.
///
/// Singleton auto-bootstrappe (RuntimeInitializeOnLoadMethod) : aucun
/// cablage scene necessaire. Fire-and-forget — le resultat est ignore.
/// </summary>
public class AIWarmup : MonoBehaviour
{
    static bool warmedUp = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (warmedUp) return; // Une seule fois par session Play.
        var go = new GameObject("AIWarmup");
        DontDestroyOnLoad(go);
        go.AddComponent<AIWarmup>();
    }

    void Start()
    {
        if (warmedUp) { Destroy(gameObject); return; }
        warmedUp = true;
        StartCoroutine(Warmup());
    }

    IEnumerator Warmup()
    {
        // Attend un instant que le provider soit pret (config chargee, etc).
        // 0.5s laisse aussi le temps a la scene de finir son setup avant
        // qu'on consomme du reseau.
        yield return new WaitForSecondsRealtime(0.5f);

        var provider = AIService.Provider;
        if (provider == null || !provider.IsConfigured)
        {
            Debug.Log("[AIWarmup] Provider non configure, skip.");
            Destroy(gameObject);
            yield break;
        }

        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system", "Reply with exactly the single character: ."),
            new OpenAIMessage("user", "ping")
        };
        // Tres petit : 1-2 tokens, latence dominee par la connexion.
        var request = new AIRequest(messages, temperature: 0.0f, maxTokens: 2);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        yield return provider.Complete(request, response =>
        {
            sw.Stop();
            if (response.success)
                Debug.Log($"[AIWarmup] Warmup OK en {sw.Elapsed.TotalSeconds:N2}s ({provider.DisplayName}).");
            else
                Debug.LogWarning($"[AIWarmup] Echec : {response.error}");
        });

        // Plus besoin de ce GO, le warmup est fait.
        Destroy(gameObject);
    }
}
