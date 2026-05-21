using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Requête générique vers un moteur d'IA conversationnelle.
/// Indépendante du backend (OpenAI, LLM local, mock...).
/// </summary>
public class AIRequest
{
    public List<OpenAIMessage> messages;

    /// <summary>
    /// Modèle à utiliser. Laisser null/vide pour que le provider applique son
    /// modèle par défaut — c'est le cas normal. Ne le renseigner que pour
    /// forcer ponctuellement un modèle précis.
    /// </summary>
    public string model;

    public float temperature = 0.8f;
    public int maxTokens = 150;

    public AIRequest(List<OpenAIMessage> messages, float temperature, int maxTokens, string model = null)
    {
        this.messages = messages;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
        this.model = model;
    }
}

/// <summary>
/// Résultat d'une complétion IA.
/// </summary>
public class AIResponse
{
    public bool success;
    public string text;
    public string error;

    public static AIResponse Ok(string text) => new AIResponse { success = true, text = text };
    public static AIResponse Fail(string error) => new AIResponse { success = false, error = error };
}

/// <summary>
/// Abstraction d'un moteur d'IA conversationnelle.
/// Implémentations : OpenAI (cloud), LLM local, mock de test...
/// Permet de changer de backend sans toucher au code de jeu.
/// </summary>
public interface IAIProvider
{
    /// <summary>Nom lisible (menu Options, logs).</summary>
    string DisplayName { get; }

    /// <summary>Vrai si le provider est utilisable (clé API présente, modèle chargé...).</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Lance une complétion. À exécuter via StartCoroutine depuis un MonoBehaviour.
    /// <paramref name="onComplete"/> est invoqué avec le résultat (succès ou échec).
    /// </summary>
    IEnumerator Complete(AIRequest request, Action<AIResponse> onComplete);
}
