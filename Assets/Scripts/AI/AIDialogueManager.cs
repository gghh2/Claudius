using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using System.Linq; // AJOUTÉ POUR LE SYSTÈME DE QUÊTE UNIQUE

// Classes pour l'historique des conversations
[System.Serializable]
public class ConversationHistory
{
    public string npcName;
    public List<string> messages = new List<string>();
    public bool hasSpokenBefore = false;
}

// Classes pour l'API OpenAI
[System.Serializable]
public class OpenAIMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class OpenAIRequest
{
    public string model;
    public OpenAIMessage[] messages;
    public float temperature;
    public int max_tokens;
}

[System.Serializable]
public class OpenAIResponse
{
    public OpenAIChoice[] choices;
}

[System.Serializable]
public class OpenAIChoice
{
    public OpenAIMessage message;
}

[System.Serializable]
public class AIConfig
{
    [Header("API Configuration")]
    public string apiKey = "";
    public string model = "gpt-3.5-turbo";
    [Range(0f, 1f)]
    public float temperature = 0.8f;
    public int maxTokens = 150;
}

public class AIDialogueManager : MonoBehaviour
{
    [Header("AI Settings")]
    public AIConfig aiConfig;
    
    [Header("Context")]
    [TextArea(3, 6)]
    public string gameContext = "Vous êtes dans un univers de space opera. Le joueur explore une station spatiale et rencontre différents personnages. Répondez en français et gardez vos réponses courtes (1-3 phrases maximum).";
    
    [Header("Conversation History")]
    private Dictionary<string, ConversationHistory> conversationHistories = new Dictionary<string, ConversationHistory>();
    
    private List<OpenAIMessage> currentConversation;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    
    public static AIDialogueManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (string.IsNullOrEmpty(aiConfig.apiKey))
        {
            Debug.LogWarning("Clé API OpenAI non configurée ! Mode fallback activé.");
        }
    }
    
    public void StartAIConversation(NPCData npcData)
    {
        // Initialise une nouvelle conversation
        currentConversation = new List<OpenAIMessage>();
        
        // Message système pour définir le contexte
        string systemPrompt = BuildSystemPrompt(npcData);
        currentConversation.Add(new OpenAIMessage { role = "system", content = systemPrompt });
        
        // Message pour déclencher la salutation
        currentConversation.Add(new OpenAIMessage { role = "user", content = "Le joueur s'approche de vous. Saluez-le de manière naturelle selon votre personnalité." });
        
        // Appel à l'IA ou fallback
        if (IsConfigured())
        {
            StartCoroutine(GetAIResponse(npcData, true));
        }
        else
        {
            UseFallback(npcData, true, "");
        }
    }
    
    public void ContinueAIConversation(NPCData npcData, string playerMessage)
    {
        if (currentConversation == null)
        {
            Debug.LogError("Conversation non initialisée !");
            return;
        }
        
        // Ajoute le message du joueur
        currentConversation.Add(new OpenAIMessage { role = "user", content = playerMessage });
        
        // Appel à l'IA ou fallback
        if (IsConfigured())
        {
            StartCoroutine(GetAIResponse(npcData, false));
        }
        else
        {
            UseFallback(npcData, false, playerMessage);
        }
    }
    
    public void InitializeConversationWithContext(NPCData npcData, string lastNPCMessage, int dialogueStep)
    {
        // Initialise une nouvelle conversation avec le contexte du dialogue précédent
        currentConversation = new List<OpenAIMessage>();
        
        // Message système pour définir le contexte
        string systemPrompt = BuildSystemPrompt(npcData);
        currentConversation.Add(new OpenAIMessage { role = "system", content = systemPrompt });
        
        // Ajoute le contexte du dialogue précédent si disponible
        if (!string.IsNullOrEmpty(lastNPCMessage) && dialogueStep > 0)
        {
            // Retire les préfixes comme "Dr. Elena Vasquez: " du message
            string cleanMessage = lastNPCMessage;
            if (cleanMessage.Contains(": "))
            {
                int colonIndex = cleanMessage.IndexOf(": ");
                if (colonIndex > 0)
                {
                    cleanMessage = cleanMessage.Substring(colonIndex + 2);
                }
            }
            
            // Ajoute le dernier message du NPC comme contexte
            currentConversation.Add(new OpenAIMessage { 
                role = "assistant", 
                content = cleanMessage 
            });
            
            Debug.Log($"Contexte ajouté pour {npcData.name}: {cleanMessage}");
        }
        
        Debug.Log($"Conversation IA initialisée avec contexte pour {npcData.name}");
    }
    
    // Garde aussi l'ancienne méthode pour les nouveaux dialogues
    public void InitializeConversation(NPCData npcData)
    {
        InitializeConversationWithContext(npcData, "", 0);
    }
    
    string BuildSystemPrompt(NPCData npcData)
    {
        string basePrompt = $@"{gameContext}

VOUS ÊTES:
- Nom: {npcData.name}
- Rôle: {npcData.role}
- Description: {npcData.description}

INSTRUCTIONS IMPORTANTES:
- Incarnez ce personnage de manière cohérente avec sa personnalité
- Répondez TOUJOURS en français
- Gardez vos réponses courtes (1-3 phrases maximum)
- Restez dans le thème space opera
- Soyez naturel et engageant
- Adaptez votre ton selon votre rôle
- Ne sortez jamais de votre rôle

SYSTÈME DE QUÊTES:
{GetQuestInstructionsForNPC(npcData.name)}

ZONES DISPONIBLES: laboratory, hangar, market, security, residential, engineering, medical, storage, ruins

{GetRoleSpecificQuestExamples(npcData.role)}

Vous êtes sur une planète extraterrestre et interagissez avec un voyageur.";

        return basePrompt;
    }

    string GetQuestInstructionsForNPC(string npcName)
    {
        // Vérifie si ce NPC a déjà donné une quête active
        if (QuestJournal.Instance != null)
        {
            var activeQuests = QuestJournal.Instance.GetActiveQuests();
            var npcActiveQuest = activeQuests.FirstOrDefault(q => q.giverNPCName == npcName);
            
            if (npcActiveQuest != null)
            {
                return $@"STATUT QUÊTE:
Vous avez déjà donné une mission à ce voyageur: ""{npcActiveQuest.questTitle}""
Progression: {npcActiveQuest.GetProgressText()}

NE DONNEZ PAS DE NOUVELLE QUÊTE. À la place:
- Demandez des nouvelles de la mission en cours
- Encouragez le voyageur  
- Donnez des conseils sur où chercher
- Montrez votre préoccupation ou satisfaction selon le progrès

EXEMPLES:
""Alors, avez-vous trouvé ce que je vous ai demandé ?""
""Comment se passe votre mission ?""
""J'espère que vous progressez bien dans votre recherche.""";
            }
            else
            {
                // Vérifie si une quête a été terminée
                var completedQuests = QuestJournal.Instance.GetCompletedQuests();
                var npcCompletedQuest = completedQuests.FirstOrDefault(q => q.giverNPCName == npcName);
                
                if (npcCompletedQuest != null)
                {
                    return @"STATUT QUÊTE:
Vous avez déjà donné une mission à ce voyageur qui l'a TERMINÉE.
Vous pouvez maintenant donner une NOUVELLE mission si approprié.

Vous pouvez donner des quêtes en utilisant ces tokens:
[QUEST:FETCH:nom_objet:zone:quantité] = Ramasser des objets
[QUEST:DELIVERY:objet:destinataire:zone] = Livrer quelque chose
[QUEST:EXPLORE:zone] = Explorer une zone
[QUEST:TALK:personnage:zone] = Parler à quelqu'un
[QUEST:INTERACT:objet:zone] = Interagir avec un objet";
                }
                else
                {
                    return @"STATUT QUÊTE:
Vous n'avez pas encore donné de mission à ce voyageur.
Vous pouvez donner des quêtes en utilisant ces tokens:

[QUEST:FETCH:nom_objet:zone:quantité] = Ramasser des objets
[QUEST:DELIVERY:objet:destinataire:zone] = Livrer quelque chose
[QUEST:EXPLORE:zone] = Explorer une zone
[QUEST:TALK:personnage:zone] = Parler à quelqu'un
[QUEST:INTERACT:objet:zone] = Interagir avec un objet";
                }
            }
        }
        
        return "Vous pouvez donner des quêtes si approprié.";
    }

    string GetRoleSpecificQuestExamples(string role)
    {
        switch (role.ToLower())
        {
            case "marchand":
                return @"EXEMPLES DE RÉPONSES AVEC TOKENS:
Joueur: ""Avez-vous du travail pour moi ?""
Vous: ""Justement ! Récupérez ce colis urgent pour moi [QUEST:FETCH:colis_urgent:hangar:1] et je vous paierai bien.""

Joueur: ""Comment puis-je vous aider ?""
Vous: ""J'attends une livraison importante. Allez la chercher ! [QUEST:FETCH:marchandise_rare:storage:2]""";

            case "scientifique":
                return @"EXEMPLES DE RÉPONSES AVEC TOKENS:
Joueur: ""Avez-vous besoin d'aide ?""
Vous: ""Mes échantillons ont disparu ! Retrouvez-les [QUEST:FETCH:echantillon_alien:laboratory:3] s'il vous plaît.""

Joueur: ""Du travail ?""
Vous: ""Ce terminal est en panne depuis des jours [QUEST:INTERACT:terminal_recherche:laboratory]. Pouvez-vous le réparer ?""";

            case "garde impérial":
                return @"EXEMPLES DE RÉPONSES AVEC TOKENS:
Joueur: ""Une mission pour moi ?""
Vous: ""Activité suspecte détectée. Inspectez les ruines [QUEST:EXPLORE:ruins] et rapportez-moi vos découvertes.""

Joueur: ""Comment aider ?""
Vous: ""Vérifiez ce terminal de sécurité [QUEST:INTERACT:console_securite:security]. Il affiche des erreurs.""";

            default:
                return @"EXEMPLES GÉNÉRIQUES:
""Aidez-moi à récupérer mes affaires [QUEST:FETCH:objet_personnel:residential:1]""
""Explorez cette zone suspecte [QUEST:EXPLORE:hangar]""
""Parlez à mon contact [QUEST:TALK:informateur:market]""";
        }
    }
    
    IEnumerator GetAIResponse(NPCData npcData, bool isWelcome)
    {
        // Indique le chargement
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowLoadingState(true);
        }
        
        // Prépare la requête
        OpenAIRequest request = new OpenAIRequest
        {
            model = aiConfig.model,
            messages = currentConversation.ToArray(),
            temperature = aiConfig.temperature,
            max_tokens = aiConfig.maxTokens
        };
        
        string jsonData = JsonUtility.ToJson(request);
        Debug.Log($"Envoi requête OpenAI pour {npcData.name}");
        
        // Appel API
        yield return StartCoroutine(CallOpenAI(jsonData, npcData, isWelcome));
    }
    
    IEnumerator CallOpenAI(string jsonData, NPCData npcData, bool isWelcome)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {aiConfig.apiKey}");
            
            yield return request.SendWebRequest();
            
            // Désactive le chargement
            if (DialogueUI.Instance != null)
            {
                DialogueUI.Instance.ShowLoadingState(false);
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                ProcessAIResponse(responseText, npcData, isWelcome);
            }
            else
            {
                Debug.LogError($"Erreur API OpenAI: {request.error}");
                Debug.LogError($"Code: {request.responseCode}");
                if (request.downloadHandler.data != null)
                {
                    string errorText = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                    Debug.LogError($"Réponse: {errorText}");
                }
                
                // Fallback en cas d'erreur
                UseFallback(npcData, isWelcome, "");
            }
        }
    }
    
    void ProcessAIResponse(string jsonResponse, NPCData npcData, bool isWelcome)
    {
        try
        {
            OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);
            
            if (response.choices != null && response.choices.Length > 0)
            {
                string aiResponse = response.choices[0].message.content.Trim();
                
                // DÉTECTION DES QUÊTES (mais pas création immédiate)
                List<QuestToken> detectedQuests = null;
                if (QuestTokenDetector.Instance != null)
                {
                    detectedQuests = QuestTokenDetector.Instance.DetectQuestTokens(aiResponse);
                    
                    if (detectedQuests.Count > 0)
                    {
                        Debug.Log($"🎯 {detectedQuests.Count} quête(s) détectée(s)");
                        
                        // Nettoie le message des tokens AVANT de l'afficher
                        aiResponse = QuestTokenDetector.Instance.CleanMessageFromTokens(aiResponse);
                    }
                }
                
                // Ajoute la réponse à l'historique
                currentConversation.Add(new OpenAIMessage { role = "assistant", content = aiResponse });
                
                Debug.Log($"IA ({npcData.name}): {aiResponse}");
                
                string formattedResponse = $"{npcData.name}: {aiResponse}";
                SaveMessageToHistory(npcData.name, formattedResponse, false);
                
                // Affiche dans l'UI
                if (isWelcome)
                {
                    DialogueUI.Instance.StartAIDialogue(npcData, formattedResponse);
                }
                else
                {
                    DialogueUI.Instance.ShowText(formattedResponse);
                }
                
                // MAINTENANT envoie les quêtes à DialogueUI APRÈS l'affichage
                if (detectedQuests != null && detectedQuests.Count > 0)
                {
                    Debug.Log($"📋 Envoi de {detectedQuests.Count} quête(s) à DialogueUI");
                    DialogueUI.Instance.SetPendingQuests(detectedQuests, npcData.name);
                }
            }
            else
            {
                Debug.LogError("Réponse OpenAI vide");
                UseFallback(npcData, isWelcome, "");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur parsing OpenAI: {e.Message}");
            UseFallback(npcData, isWelcome, "");
        }
    }
    
    void UseFallback(NPCData npcData, bool isWelcome, string playerMessage)
    {
        Debug.Log("Utilisation du mode fallback");
        
        string fallbackResponse;
        
        if (isWelcome)
        {
            fallbackResponse = GetFallbackWelcome(npcData);
            DialogueUI.Instance.StartAIDialogue(npcData, fallbackResponse);
        }
        else
        {
            fallbackResponse = GetFallbackResponse(npcData, playerMessage);
            DialogueUI.Instance.ShowAIResponse(fallbackResponse);
        }
    }
    
    string GetFallbackWelcome(NPCData npcData)
    {
        switch (npcData.role.ToLower())
        {
            case "marchand":
                return $"[Fallback] Salutations ! Je suis {npcData.name}. Mes marchandises n'attendent que vous !";
            case "scientifique":
                return $"[Fallback] Fascinant ! {npcData.name} ici. Mes recherches progressent bien.";
            case "garde impérial":
                return $"[Fallback] {npcData.name}, sécurité impériale. Vos papiers, s'il vous plaît.";
            default:
                return $"[Fallback] Bonjour, je suis {npcData.name}. Comment puis-je vous aider ?";
        }
    }
    
    string GetFallbackResponse(NPCData npcData, string playerMessage)
    {
        string message = playerMessage.ToLower();
        
        if (message.Contains("bonjour") || message.Contains("salut"))
        {
            return "[Fallback] Bonjour à vous aussi ! En quoi puis-je vous être utile ?";
        }
        else if (message.Contains("merci"))
        {
            return "[Fallback] De rien ! C'est toujours un plaisir d'aider.";
        }
        else if (message.Contains("au revoir"))
        {
            return "[Fallback] Au revoir ! Que votre voyage soit sûr.";
        }
        else
        {
            switch (npcData.role.ToLower())
            {
                case "marchand":
                    return "[Fallback] Intéressant... Voulez-vous voir mes dernières acquisitions ?";
                case "scientifique":
                    return "[Fallback] Hmm, cela me rappelle mes recherches sur les anomalies spatiales.";
                case "garde impérial":
                    return "[Fallback] Je note votre demande. Respectez les protocoles.";
                default:
                    return "[Fallback] C'est une perspective intéressante. Continuez...";
            }
        }
    }
    
    // ========== MÉTHODES POUR L'HISTORIQUE ==========
    
    public void SaveMessageToHistory(string npcName, string message, bool isPlayer = false)
    {
        if (!conversationHistories.ContainsKey(npcName))
        {
            conversationHistories[npcName] = new ConversationHistory { npcName = npcName };
        }
        
        string formattedMessage = isPlayer ? $"Vous: {message}" : message;
        conversationHistories[npcName].messages.Add(formattedMessage);
        conversationHistories[npcName].hasSpokenBefore = true;
        
        Debug.Log($"Message sauvé pour {npcName}: {formattedMessage}");
    }
    
    public ConversationHistory GetConversationHistory(string npcName)
    {
        if (conversationHistories.ContainsKey(npcName))
        {
            return conversationHistories[npcName];
        }
        return null;
    }
    
    public bool HasSpokenToNPC(string npcName)
    {
        return conversationHistories.ContainsKey(npcName) && conversationHistories[npcName].hasSpokenBefore;
    }
    
    // ========== MÉTHODES UTILITAIRES ==========
    
    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(aiConfig.apiKey);
    }
    
    public void ResetConversation()
    {
        currentConversation?.Clear();
    }
    
    // Méthode pour effacer tout l'historique (optionnel)
    public void ClearAllHistory()
    {
        conversationHistories.Clear();
        Debug.Log("Historique des conversations effacé");
    }
}