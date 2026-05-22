using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provider IA pour toute API exposant le format « chat completions » d'OpenAI.
///
/// OpenAI lui-même, mais aussi Ollama (endpoint <c>/v1/chat/completions</c>) ou
/// tout serveur compatible : seules l'URL, la clé et le modèle par défaut
/// changent. Cette classe porte toute la logique HTTP ; les providers concrets
/// (<see cref="OpenAIProvider"/>, <see cref="OllamaProvider"/>) ne sont que des
/// préréglages.
/// </summary>
public class OpenAICompatibleProvider : IAIProvider
{
    private readonly string baseUrl;
    private readonly string apiKey;
    private readonly string defaultModel;
    private readonly bool requiresApiKey;

    public string DisplayName { get; }

    /// <summary>Modèle appliqué quand la requête n'en impose pas (AIRequest.model null/vide).</summary>
    public string DefaultModel => defaultModel;

    public OpenAICompatibleProvider(string displayName, string baseUrl, string apiKey,
                                    string defaultModel, bool requiresApiKey)
    {
        this.DisplayName = displayName;
        this.baseUrl = baseUrl;
        this.apiKey = apiKey;
        this.defaultModel = defaultModel;
        this.requiresApiKey = requiresApiKey;
    }

    public bool IsConfigured => !requiresApiKey || !string.IsNullOrEmpty(apiKey);

    public IEnumerator Complete(AIRequest request, Action<AIResponse> onComplete)
    {
        var payload = new OpenAIRequest
        {
            model = string.IsNullOrEmpty(request.model) ? defaultModel : request.model,
            messages = request.messages.ToArray(),
            temperature = request.temperature,
            max_tokens = request.maxTokens
        };
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        using (UnityWebRequest http = new UnityWebRequest(baseUrl, "POST"))
        {
            http.uploadHandler = new UploadHandlerRaw(body);
            http.downloadHandler = new DownloadHandlerBuffer();
            http.SetRequestHeader("Content-Type", "application/json");

            // Pas de clé pour un serveur local (Ollama) : on n'envoie l'en-tête
            // d'authentification que si une clé est réellement fournie.
            if (!string.IsNullOrEmpty(apiKey))
                http.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return http.SendWebRequest();

            if (http.result == UnityWebRequest.Result.Success)
            {
                string content = ExtractContent(Encoding.UTF8.GetString(http.downloadHandler.data));
                if (!string.IsNullOrEmpty(content))
                    onComplete?.Invoke(AIResponse.Ok(content));
                else
                    onComplete?.Invoke(AIResponse.Fail($"Réponse {DisplayName} vide ou illisible"));
            }
            else
            {
                // Le corps de la réponse contient l'explication de l'API
                // (message d'erreur JSON d'OpenAI, etc.) — indispensable pour
                // diagnostiquer un 4xx (clé invalide, quota, région...).
                string errorBody = http.downloadHandler?.text;
                string detail = string.IsNullOrEmpty(errorBody) ? "" : $" — {errorBody.Trim()}";
                onComplete?.Invoke(AIResponse.Fail($"{http.error} (code {http.responseCode}){detail}"));
            }
        }
    }

    static string ExtractContent(string json)
    {
        try
        {
            OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(json);
            if (response != null && response.choices != null && response.choices.Length > 0)
                return response.choices[0].message.content;
        }
        catch (Exception e)
        {
            Debug.LogError($"[OpenAICompatibleProvider] Parsing de la réponse échoué : {e.Message}");
        }
        return null;
    }
}
