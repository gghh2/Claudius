// Assets/Scripts/AI/Core/AIDialogueManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIDialogueManager : MonoBehaviour
{
    [Header("AI Settings")]
    public AIConfig aiConfig;
    
    [Header("Prompt Configuration")]
    public AIPromptConfig promptConfig;
    public AIPromptConfig marchandPromptConfig;
    public AIPromptConfig scientifiquePromptConfig;
    public AIPromptConfig gardePromptConfig;
    public AIPromptConfig defaultPromptConfig; // Fallback
    
    [Header("Context")]
    [TextArea(3, 6)]
    public string gameContext = "Vous êtes dans un univers de space opera. Le joueur explore une station spatiale et rencontre différents personnages. Répondez en français et gardez vos réponses courtes (1-3 phrases maximum).";
    
    [Header("API Status")]
    [SerializeField] private bool apiKeyLoaded = false;
    [SerializeField] private string apiKeySource = "Non chargée";
    
    // Private variables
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
        LoadAPIKey();
    }
    
    void LoadAPIKey()
    {
        apiKeyLoaded = false;
        apiKeySource = "Non chargée";
        
        // Tentative 1 : Charge depuis APIConfig
        try
        {
            string configKey = APIConfig.OPENAI_API_KEY;
            
            if (!string.IsNullOrEmpty(configKey) && 
                configKey != "sk-REMPLACEZ_MOI" && 
                configKey != "sk-VOTRE_CLE_API_ICI")
            {
                aiConfig.apiKey = configKey;
                apiKeyLoaded = true;
                apiKeySource = "APIConfig.cs";
                
                if (aiConfig.showApiStatus)
                {
                    Debug.Log($"✅ Clé API OpenAI chargée depuis {apiKeySource}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Impossible de charger APIConfig.cs : {e.Message}");
        }
        
        if (!apiKeyLoaded)
        {
            Debug.LogWarning("⚠️ Clé API OpenAI non configurée ! Mode fallback activé.");
        }
    }
    
    public void StartAIConversation(NPCData npcData)
    {
        currentConversation = new List<OpenAIMessage>();
        
        string systemPrompt = BuildSystemPrompt(npcData);
        currentConversation.Add(new OpenAIMessage("system", systemPrompt));
        currentConversation.Add(new OpenAIMessage("user", "Le joueur s'approche de vous. Saluez-le de manière naturelle selon votre personnalité."));
        
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
        
        currentConversation.Add(new OpenAIMessage("user", playerMessage));
        
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
        currentConversation = new List<OpenAIMessage>();
        
        string systemPrompt = BuildSystemPrompt(npcData);
        currentConversation.Add(new OpenAIMessage("system", systemPrompt));
        
        if (!string.IsNullOrEmpty(lastNPCMessage) && dialogueStep > 0)
        {
            string cleanMessage = lastNPCMessage;
            if (cleanMessage.Contains(": "))
            {
                int colonIndex = cleanMessage.IndexOf(": ");
                if (colonIndex > 0)
                {
                    cleanMessage = cleanMessage.Substring(colonIndex + 2);
                }
            }
            
            currentConversation.Add(new OpenAIMessage("assistant", cleanMessage));
            Debug.Log($"Contexte ajouté pour {npcData.name}: {cleanMessage}");
        }
        
        Debug.Log($"Conversation IA initialisée avec contexte pour {npcData.name}");
    }
    
    public void InitializeConversation(NPCData npcData)
    {
        InitializeConversationWithContext(npcData, "", 0);
    }
    
    AIPromptConfig GetConfigForRole(string role)
    {
        switch (role.ToLower())
        {
            case "marchand":
            case "trader":
                return marchandPromptConfig;
                
            case "scientifique":
            case "scientist":
            case "chercheur":
                return scientifiquePromptConfig;
                
            case "garde":
            case "garde impérial":
            case "guard":
            case "security":
                return gardePromptConfig;
                
            default:
                return defaultPromptConfig;
        }
    }

    string BuildSystemPrompt(NPCData npcData)
    {
        AIPromptConfig configToUse = GetConfigForRole(npcData.role);
        
        if (configToUse == null)
        {
            Debug.LogError($"❌ Aucune config trouvée pour le rôle: {npcData.role}");
            
            // Fallback avec l'ancien système
            return $@"🔴 INSTRUCTION CRITIQUE: Quand on vous demande une mission/quête/travail, vous DEVEZ inclure un token [QUEST:...] dans votre réponse!

{gameContext}

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
- IMPORTANT: Quand vous donnez une quête, INCLUEZ TOUJOURS le token [QUEST:...] dans votre réponse!

SYSTÈME DE QUÊTES:
{GetQuestInstructionsForNPC(npcData.name)}

ZONES DISPONIBLES: laboratory, hangar, market, security, residential, engineering, medical, storage, ruins

{GetRoleSpecificQuestExamples(npcData.role)}

Vous êtes sur une planète extraterrestre et interagissez avec un voyageur.";
        }
        
        // Utilise la config appropriée
        return $@"🔴 INSTRUCTION CRITIQUE: Quand on vous demande une mission/quête/travail, vous DEVEZ inclure un token [QUEST:...] dans votre réponse!

{configToUse.npcPersonality}

VOUS ÊTES:
- Nom: {npcData.name}
- Rôle: {npcData.role}
- Description: {npcData.description}

{configToUse.globalInstructions}

🔴 RÈGLE ABSOLUE: Pour donner une quête, vous DEVEZ inclure un token [QUEST:TYPE:params] dans votre réponse!

SYSTÈME DE QUÊTES:
{GetQuestInstructionsForNPC(npcData.name)}

ZONES DISPONIBLES: laboratory, hangar, market, security, residential, engineering, medical, storage, ruins

EXEMPLES POUR VOTRE RÔLE:
{configToUse.roleSpecificExamples}";
    }
    
    string GetQuestInstructionsForNPC(string npcName)
    {
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
- Montrez votre préoccupation ou satisfaction selon le progrès";
            }
            else
            {
                var completedQuests = QuestJournal.Instance.GetCompletedQuests();
                var npcCompletedQuest = completedQuests.FirstOrDefault(q => q.giverNPCName == npcName);
                
                if (npcCompletedQuest != null)
                {
                    return @"STATUT QUÊTE:
Vous avez déjà donné une mission à ce voyageur qui l'a TERMINÉE.
Vous pouvez maintenant donner une NOUVELLE mission si approprié.

⚠️ OBLIGATOIRE: Pour créer une quête, vous DEVEZ inclure un token dans votre réponse!

FORMAT DES TOKENS:
[QUEST:FETCH:nom_objet:zone:quantité] = Ramasser des objets
[QUEST:DELIVERY:objet:destinataire:zone] = Livrer quelque chose
[QUEST:EXPLORE:zone] = Explorer une zone
[QUEST:TALK:personnage:zone] = Parler à quelqu'un
[QUEST:INTERACT:objet:zone] = Interagir avec un objet

🔴 SANS TOKEN, AUCUNE QUÊTE NE SERA CRÉÉE!";
                }
            }
        }
        
        return @"STATUT QUÊTE:
Vous n'avez pas encore donné de mission à ce voyageur.

⚠️ OBLIGATOIRE: Pour créer une quête, vous DEVEZ inclure un token dans votre réponse!

FORMAT DES TOKENS:
[QUEST:FETCH:nom_objet:zone:quantité] = Ramasser des objets
[QUEST:DELIVERY:objet:destinataire:zone] = Livrer quelque chose
[QUEST:EXPLORE:zone] = Explorer une zone
[QUEST:TALK:personnage:zone] = Parler à quelqu'un
[QUEST:INTERACT:objet:zone] = Interagir avec un objet

EXEMPLE CORRECT: 'J'ai besoin d'aide ! Récupérez mes outils [QUEST:FETCH:outils:hangar:3] dans le hangar.'
EXEMPLE INCORRECT: 'J'ai besoin que vous récupériez mes outils.' (PAS DE TOKEN = PAS DE QUÊTE!)

🔴 SANS TOKEN, AUCUNE QUÊTE NE SERA CRÉÉE!";
    }
    
    string GetAvailableQuestOptionsForAI()
    {
        if (QuestZoneManager.Instance == null)
        {
            Debug.LogWarning("QuestZoneManager.Instance is null - using default zones");
            return "ZONES DISPONIBLES: laboratory, hangar, market, security, residential, engineering, medical, storage, ruins";
        }
        
        var availableOptions = QuestZoneManager.Instance.GetAvailableQuestOptions();
        
        if (availableOptions.Count == 0)
        {
            Debug.LogWarning("No quest zones available!");
            return "AUCUNE ZONE DE QUÊTE DISPONIBLE ACTUELLEMENT";
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("QUÊTES POSSIBLES ACTUELLEMENT:");
        sb.AppendLine("(Utilisez UNIQUEMENT les zones listées ci-dessous pour chaque type de quête)");
        
        foreach (var kvp in availableOptions)
        {
            QuestType questType = kvp.Key;
            List<QuestZone> zones = kvp.Value;
            
            sb.AppendLine($"\n{questType}:");
            foreach (var zone in zones)
            {
                sb.AppendLine($"  - {zone.zoneName} (type: {zone.zoneType})");
            }
        }
        
        sb.AppendLine("\nIMPORTANT: NE PROPOSEZ QUE DES QUÊTES POUR LES ZONES LISTÉES CI-DESSUS!");
        
        // Add specific warning if only FETCH is available
        if (availableOptions.Count == 1 && availableOptions.ContainsKey(QuestType.FETCH))
        {
            sb.AppendLine("\nATTENTION: Actuellement, SEULES les quêtes FETCH sont disponibles!");
            sb.AppendLine("Vous DEVEZ donner une quête de type FETCH (ramasser des objets).");
            sb.AppendLine("N'essayez PAS de donner des quêtes EXPLORE, DELIVERY, TALK ou INTERACT!");
        }
        
        return sb.ToString();
    }
    
    string GetRoleSpecificQuestExamples(string role)
    {
        switch (role.ToLower())
        {
            case "marchand":
                return @"EXEMPLES DE RÉPONSES AVEC TOKENS:
Joueur: ""Avez-vous du travail pour moi ?""
Vous: ""Justement ! Récupérez ce colis urgent pour moi [QUEST:FETCH:colis_urgent:hangar:1] et je vous paierai bien.""

AUTRES EXEMPLES:
- ""J'ai besoin de marchandises ! Trouvez-moi [QUEST:FETCH:cristaux_rares:market:5] au marché.""
- ""Livrez ce paquet [QUEST:DELIVERY:paquet_secret:garde_imperial:security] au garde impérial.""

⚠️ RAPPEL CRUCIAL: Le token [QUEST:...] DOIT être dans votre message sinon AUCUNE quête ne sera créée!";

            case "scientifique":
                return @"EXEMPLES DE RÉPONSES AVEC TOKENS:
Joueur: ""Avez-vous besoin d'aide ?""
Vous: ""Mes échantillons ont disparu ! Retrouvez-les [QUEST:FETCH:echantillon_alien:laboratory:3] s'il vous plaît.""

AUTRES EXEMPLES:
- ""Explorez cette zone mystérieuse [QUEST:EXPLORE:ruins] et rapportez vos découvertes.""
- ""Allez parler à mon assistant [QUEST:TALK:assistant_perdu:medical] dans la baie médicale.""

⚠️ RAPPEL CRUCIAL: Le token [QUEST:...] DOIT être dans votre message sinon AUCUNE quête ne sera créée!";

            case "garde impérial":
                return @"EXEMPLES DE RÉPONSES AVEC TOKENS:
Joueur: ""Une mission pour moi ?""
Vous: ""Activité suspecte détectée. Inspectez les ruines [QUEST:EXPLORE:ruins] et rapportez-moi vos découvertes.""

AUTRES EXEMPLES:
- ""Récupérez l'artefact ancien [QUEST:FETCH:artefact_ancien:ruins:1] dans les ruines.""
- ""Interagissez avec le terminal de sécurité [QUEST:INTERACT:terminal_securite:security] pour vérifier les accès.""

⚠️ RAPPEL CRUCIAL: Le token [QUEST:...] DOIT être dans votre message sinon AUCUNE quête ne sera créée!";

            default:
                return @"EXEMPLES GÉNÉRIQUES:
- ""Aidez-moi à récupérer mes affaires [QUEST:FETCH:objet_personnel:residential:1]""
- ""Explorez cette zone suspecte [QUEST:EXPLORE:hangar]""
- ""Parlez à mon contact [QUEST:TALK:informateur:market] au marché""

⚠️ RAPPEL CRUCIAL: Le token [QUEST:...] DOIT être dans votre message sinon AUCUNE quête ne sera créée!";
        }
    }
    
    IEnumerator GetAIResponse(NPCData npcData, bool isWelcome)
    {
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowLoadingState(true);
        }
        
        OpenAIRequest request = new OpenAIRequest
        {
            model = aiConfig.model,
            messages = currentConversation.ToArray(),
            temperature = aiConfig.temperature,
            max_tokens = aiConfig.maxTokens
        };
        
        string jsonData = JsonUtility.ToJson(request);
        Debug.Log($"Envoi requête OpenAI pour {npcData.name}");
        
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
                
                Debug.Log($"🤖 Réponse IA brute:");
                Debug.Log(aiResponse);
                
                // Détection des quêtes
                List<QuestToken> detectedQuests = null;
                if (QuestTokenDetector.Instance != null)
                {
                    detectedQuests = QuestTokenDetector.Instance.DetectQuestTokens(aiResponse);
                    
                    if (detectedQuests != null && detectedQuests.Count > 0)
                    {
                        Debug.Log($"🎯 {detectedQuests.Count} quête(s) détectée(s)");
                        foreach (var quest in detectedQuests)
                        {
                            Debug.Log($"  - Type: {quest.questType}, Zone: {quest.zoneName}, Description: {quest.description}");
                        }
                        aiResponse = QuestTokenDetector.Instance.CleanMessageFromTokens(aiResponse);
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Aucune quête détectée dans la réponse de l'IA");
                        Debug.Log($"Réponse complète: {aiResponse}");
                    }
                }
                
                currentConversation.Add(new OpenAIMessage("assistant", aiResponse));
                
                Debug.Log($"IA ({npcData.name}): {aiResponse}");
                
                string formattedResponse = $"{npcData.name}: {aiResponse}";
                SaveMessageToHistory(npcData.name, formattedResponse, false);
                
                if (isWelcome)
                {
                    DialogueUI.Instance.StartAIDialogue(npcData, formattedResponse);
                }
                else
                {
                    DialogueUI.Instance.ShowText(formattedResponse);
                }
                
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
    
    public bool IsConfigured()
    {
        return apiKeyLoaded && !string.IsNullOrEmpty(aiConfig.apiKey);
    }
    
    public void ResetConversation()
    {
        currentConversation?.Clear();
    }
    
    public void ClearAllHistory()
    {
        conversationHistories.Clear();
        Debug.Log("Historique des conversations effacé");
    }
    
    [ContextMenu("Reload API Key")]
    public void ReloadAPIKey()
    {
        LoadAPIKey();
    }
    
    [ContextMenu("Show API Status")]
    public void ShowAPIStatus()
    {
        Debug.Log($"=== API STATUS ===");
        Debug.Log($"Clé chargée: {(apiKeyLoaded ? "✅" : "❌")}");
        Debug.Log($"Source: {apiKeySource}");
        Debug.Log($"Longueur clé: {aiConfig.apiKey?.Length ?? 0} caractères");
        
        if (apiKeyLoaded && !string.IsNullOrEmpty(aiConfig.apiKey))
        {
            string maskedKey = aiConfig.apiKey.Substring(0, 7) + "..." + aiConfig.apiKey.Substring(aiConfig.apiKey.Length - 4);
            Debug.Log($"Clé masquée: {maskedKey}");
        }
    }
}
