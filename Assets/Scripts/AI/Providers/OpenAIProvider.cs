using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provider IA basé sur l'API OpenAI (chat completions).
/// Encapsule l'appel HTTP qui était auparavant dupliqué dans AIDialogueManager
/// et AdventureJournalUI.
/// </summary>
public class OpenAIProvider : IAIProvider
{
    const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    private readonly string apiKey;

    public OpenAIProvider(string apiKey)
    {
        this.apiKey = apiKey;
    }

    public string DisplayName => "OpenAI (Cloud)";

    public bool IsConfigured => !string.IsNullOrEmpty(apiKey);

    public IEnumerator Complete(AIRequest request, Action<AIResponse> onComplete)
    {
        var payload = new OpenAIRequest
        {
            model = request.model,
            messages = request.messages.ToArray(),
            temperature = request.temperature,
            max_tokens = request.maxTokens
        };
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        using (UnityWebRequest http = new UnityWebRequest(ApiUrl, "POST"))
        {
            http.uploadHandler = new UploadHandlerRaw(body);
            http.downloadHandler = new DownloadHandlerBuffer();
            http.SetRequestHeader("Content-Type", "application/json");
            http.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return http.SendWebRequest();

            if (http.result == UnityWebRequest.Result.Success)
            {
                string content = ExtractContent(Encoding.UTF8.GetString(http.downloadHandler.data));
                if (!string.IsNullOrEmpty(content))
                    onComplete?.Invoke(AIResponse.Ok(content));
                else
                    onComplete?.Invoke(AIResponse.Fail("Réponse OpenAI vide ou illisible"));
            }
            else
            {
                onComplete?.Invoke(AIResponse.Fail($"{http.error} (code {http.responseCode})"));
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
            Debug.LogError($"[OpenAIProvider] Parsing de la réponse échoué : {e.Message}");
        }
        return null;
    }
}
