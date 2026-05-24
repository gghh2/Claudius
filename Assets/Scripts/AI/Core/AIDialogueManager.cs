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

    // Une conversation persistante (messages IA) par PNJ : ré-aborder un PNJ
    // reprend sa conversation au lieu de la réinitialiser → il se souvient.
    private Dictionary<string, List<OpenAIMessage>> conversationsByNpc = new Dictionary<string, List<OpenAIMessage>>();

    public static AIDialogueManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
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
        // Reprise : si une conversation existe déjà avec ce PNJ, on la continue
        // au lieu de la réinitialiser — le PNJ se souvient des échanges passés.
        if (conversationsByNpc.TryGetValue(npcData.name, out var existing) && existing.Count > 0)
        {
            currentConversation = existing;
            currentConversation.Add(new OpenAIMessage("user",
                "Le joueur revient vous parler. Accueillez-le comme une connaissance, en vous souvenant de vos échanges précédents."));
        }
        else
        {
            currentConversation = new List<OpenAIMessage>();
            conversationsByNpc[npcData.name] = currentConversation;

            currentConversation.Add(new OpenAIMessage("system", BuildSystemPrompt(npcData)));

            string initialUserMessage = "Le joueur s'approche de vous. Saluez-le de manière naturelle selon votre personnalité.";
            if (QuestJournal.Instance != null)
            {
                var npcActiveQuest = QuestJournal.Instance.GetActiveQuests()
                                                .FirstOrDefault(q => q.giverNPCName == npcData.name);
                if (npcActiveQuest != null)
                {
                    string questDetails = $"quête '{npcActiveQuest.description}' (Progression : {npcActiveQuest.GetProgressText()})";
                    initialUserMessage = $"Le joueur, qui a votre {questDetails}, s'approche de vous. Demandez-lui où il en est et encouragez-le selon sa progression.";
                    currentConversation.Add(new OpenAIMessage("system", $"RAPPEL : le joueur a une quête active avec vous : {questDetails}. Parlez-en en premier."));
                }
            }
            currentConversation.Add(new OpenAIMessage("user", initialUserMessage));
        }
        
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
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log($"[AI] Contexte ajouté pour {npcData.name}: {cleanMessage}");
        }

        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log($"[AI] Conversation IA initialisée avec contexte pour {npcData.name}");
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
        
        // Check for active quest FIRST
        string activeQuestInfo = "";
        
        if (QuestJournal.Instance != null)
        {
            var activeQuests = QuestJournal.Instance.GetActiveQuests();
            var npcActiveQuest = activeQuests.FirstOrDefault(q => q.giverNPCName == npcData.name);
            
            if (npcActiveQuest != null)
            {
                if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log($"[AI] 🎯 QUÊTE ACTIVE DÉTECTÉE: {npcActiveQuest.description} - Progression: {npcActiveQuest.GetProgressText()}");
                activeQuestInfo = $@"
QUÊTE EN COURS avec ce voyageur : {npcActiveQuest.description}
Progression : {npcActiveQuest.GetProgressText()}
Demandez-lui où il en est et encouragez-le. Ne proposez pas de nouvelle quête.
";
            }
        }
        
        if (configToUse == null)
        {
            Debug.LogError($"❌ Aucune config trouvée pour le rôle: {npcData.role}");
            
            // Fallback avec l'ancien système
            return $@"Vous incarnez un personnage d'un jeu d'aventure spatiale. Restez dans votre rôle, répondez en français, en 1 à 3 phrases.
{activeQuestInfo}
{gameContext}

VOTRE PERSONNAGE :
- Nom : {npcData.name}
- Rôle : {npcData.role}
- Description : {npcData.description}

INSTRUCTIONS :
- Incarnez ce personnage de manière cohérente, sans jamais sortir de votre rôle.
- Soyez naturel et engageant ; adaptez votre ton à votre rôle.
- Vous pouvez évoquer naturellement vos soucis, vos besoins ou vos problèmes au fil de la conversation.
- Vous n'attribuez JAMAIS de mission formelle et vous n'écrivez JAMAIS rien entre crochets (ni code, ni didascalie comme « [je souris] »). Contentez-vous de jouer votre personnage et de discuter.";
        }
        
        // Utilise la config appropriée — prompt de roleplay PUR (aucune quête).
        return $@"Vous incarnez un personnage d'un jeu d'aventure spatiale. Restez dans votre rôle, répondez en français, en 1 à 3 phrases.
{activeQuestInfo}
{configToUse.npcPersonality}

VOTRE PERSONNAGE :
- Nom : {npcData.name}
- Rôle : {npcData.role}
- Description : {npcData.description}

{configToUse.globalInstructions}

Vous discutez librement avec le voyageur. Vous pouvez évoquer naturellement vos
soucis, vos besoins ou vos problèmes au fil de la conversation — mais vous
n'attribuez JAMAIS de mission formelle et vous n'écrivez JAMAIS rien entre
crochets (ni code, ni didascalie comme « [je souris] »). Contentez-vous de
jouer votre personnage et de discuter.";
    }
    
    IEnumerator GetAIResponse(NPCData npcData, bool isWelcome)
    {
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowLoadingState(true);
        }

        // Passe par l'abstraction IA (AIService) au lieu d'appeler OpenAI en dur.
        // Le modèle n'est pas imposé : le provider actif applique le sien.
        var request = new AIRequest(
            new List<OpenAIMessage>(currentConversation),
            aiConfig.temperature, aiConfig.maxTokens);

        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log($"[AI] Envoi requête IA pour {npcData.name}");

        // Chronomètre la latence réelle de l'appel IA (diagnostic des délais).
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        yield return StartCoroutine(AIService.Provider.Complete(request, response =>
        {
            stopwatch.Stop();

            if (DialogueUI.Instance != null)
            {
                DialogueUI.Instance.ShowLoadingState(false);
            }

            if (response.success)
            {
                ProcessAIResponse(response.text, npcData, isWelcome, stopwatch.Elapsed.TotalSeconds);
            }
            else
            {
                Debug.LogError($"Erreur IA : {response.error}");
                UseFallback(npcData, isWelcome, "");
            }
        }));
    }

    // Appel 1 — traite la réponse de CHAT (roleplay pur). Ne détecte aucune
    // quête : la détection se fait dans l'appel d'analyse séparé (AnalyzeForQuest).
    void ProcessAIResponse(string aiContent, NPCData npcData, bool isWelcome, double responseSeconds)
    {
        if (string.IsNullOrEmpty(aiContent))
        {
            Debug.LogError("Réponse IA vide");
            UseFallback(npcData, isWelcome, "");
            return;
        }

        try
        {
            string aiResponse = aiContent.Trim();

            // Sécurité : le chat ne doit pas produire de token. Si le modèle en
            // glisse un malgré tout, on le retire de l'affichage — il ne crée
            // aucune quête (la détection est faite par l'appel d'analyse séparé).
            if (QuestTokenDetector.Instance != null)
                aiResponse = QuestTokenDetector.Instance.CleanMessageFromTokens(aiResponse);

            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log($"[AI] 🤖 Réponse de chat ({npcData.name}) en {responseSeconds:N1} s : {aiResponse}");

            currentConversation.Add(new OpenAIMessage("assistant", aiResponse));

            string formattedResponse = $"{npcData.name}: {aiResponse}";
            SaveMessageToHistory(npcData.name, formattedResponse, false);

            if (isWelcome)
                DialogueUI.Instance.StartAIDialogue(npcData, formattedResponse);
            else
                DialogueUI.Instance.ShowText(formattedResponse);

            // Appel 2 — analyse de quête séparée. Jamais sur le simple accueil :
            // le joueur n'a encore rien demandé.
            if (!isWelcome)
            {
                string playerMessage = currentConversation.LastOrDefault(m => m.role == "user")?.content ?? "(inconnu)";
                StartCoroutine(AnalyzeForQuest(npcData, playerMessage, aiResponse));
            }
            else
            {
                // L'accueil ne déclenche pas d'analyse de quête, mais on le
                // journalise quand même : c'est ici qu'on voit si un PNJ se
                // souvient du joueur (sa façon de le resaluer).
                MissionProposalLogger.Log(npcData.name, npcData.role, "(accueil du PNJ)",
                                          aiResponse, "(accueil — pas d'analyse de quête)",
                                          null, responseSeconds);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur traitement réponse de chat : {e.Message}");
            UseFallback(npcData, isWelcome, "");
        }
    }
    
    // Prompt de l'appel 2 : analyse de quête. Mono-tâche — décider si le joueur
    // cherche une mission et, le cas échéant, produire UN token. Aucun roleplay.
    string BuildQuestAnalysisPrompt(NPCData npcData)
    {
        return $@"Tu es un analyseur de quêtes pour un jeu d'aventure spatiale. On te donne la conversation entre un voyageur (le joueur) et un PNJ.

LE PNJ :
- Nom : {npcData.name}
- Rôle : {npcData.role}

TA TÂCHE : déterminer si, dans son DERNIER message, le joueur cherche une mission — soit en demandant explicitement du travail, soit en acceptant de s'occuper d'un problème que le PNJ vient d'évoquer.

- Si OUI : génère UNE quête cohérente avec la conversation, sous la forme d'UN SEUL token.
- Si NON : réponds exactement NONE.
- Dans le doute : NONE. Un simple bavardage, une question, une politesse, une remarque ne sont PAS une demande de mission.
- Ta réponse entière doit être SOIT un token, SOIT le mot NONE. Aucun autre texte.

FORMATS DE TOKEN :
[QUEST:FETCH:objet:zone:quantité]        — rapporter des objets
[QUEST:DELIVERY:objet:destinataire:zone] — livrer quelque chose à quelqu'un
[QUEST:EXPLORE:zone]                     — explorer une zone
[QUEST:TALK:personnage:zone]             — aller parler à quelqu'un
[QUEST:INTERACT:objet:zone]              — interagir avec un objet

ZONES VALIDES (utilise UNIQUEMENT celles-ci) : laboratory, hangar, market, security, residential, engineering, medical, storage, ruins

RÈGLES :
- La quête doit découler d'un sujet CONCRET de la conversation : un objet, un lieu, un problème ou un besoin réellement évoqué. Si le joueur exprime de l'intérêt mais qu'aucun sujet concret n'a été abordé, réponds NONE.
- FETCH : si le joueur parle d'UN seul objet, la quantité est 1.
- Le destinataire d'une DELIVERY et la cible d'un TALK sont des personnages avec un nom propre inventé (« Maître Orin », « Dame Sevra »), jamais un mot générique, jamais un lieu.
- INTERDIT : une quête TALK ou DELIVERY ne doit JAMAIS cibler {npcData.name} (le PNJ courant). On n'envoie pas le joueur parler à la personne avec qui il discute déjà — la cible est forcément un AUTRE personnage.";
    }

    // Appel 2 — lancé après la réponse de chat. Analyse la conversation et
    // décide si une quête doit être proposée. Le chat lui-même n'émet aucun token.
    IEnumerator AnalyzeForQuest(NPCData npcData, string playerMessage, string chatReply)
    {
        // Pas de seconde quête tant qu'une quête est déjà active avec ce PNJ.
        if (QuestJournal.Instance != null)
        {
            var active = QuestJournal.Instance.GetActiveQuests()
                                     .FirstOrDefault(q => q.giverNPCName == npcData.name);
            if (active != null)
                yield break;
        }

        // Transcript de la conversation, fourni comme un seul bloc à analyser.
        var sb = new StringBuilder();
        foreach (var m in currentConversation)
        {
            if (m.role == "user")
                sb.AppendLine($"Joueur : {m.content}");
            else if (m.role == "assistant")
                sb.AppendLine($"{npcData.name} : {m.content}");
        }

        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system", BuildQuestAnalysisPrompt(npcData)),
            new OpenAIMessage("user",
                $"Voici la conversation :\n\n{sb}\n" +
                "D'après le DERNIER message du joueur, cherche-t-il une mission ? " +
                "Réponds uniquement par un token [QUEST:...] ou par NONE.")
        };

        // Température basse : l'analyse doit être stable, pas créative.
        var request = new AIRequest(messages, 0.3f, 60);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        yield return StartCoroutine(AIService.Provider.Complete(request, response =>
        {
            stopwatch.Stop();
            if (response.success)
                ProcessQuestAnalysis(response.text, npcData, playerMessage, chatReply, stopwatch.Elapsed.TotalSeconds);
            else
                Debug.LogWarning($"[QuestAnalysis] Échec de l'appel : {response.error}");
        }));
    }

    // Traite la sortie de l'appel 2 : extrait un éventuel token (validé par
    // QuestTokenDetector), le journalise, et le transmet à l'UI si une quête en sort.
    void ProcessQuestAnalysis(string analysisOutput, NPCData npcData, string playerMessage, string chatReply, double seconds)
    {
        string raw = (analysisOutput ?? string.Empty).Trim();
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log($"[QuestAnalysis] Sortie ({seconds:N1} s) : {raw}");

        List<QuestToken> detectedQuests = null;
        if (QuestTokenDetector.Instance != null)
            detectedQuests = QuestTokenDetector.Instance.DetectQuestTokens(raw);

        MissionProposalLogger.Log(npcData.name, npcData.role, playerMessage,
                                  chatReply, raw, detectedQuests, seconds);

        if (detectedQuests != null && detectedQuests.Count > 0)
        {
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log($"[AI] 🎯 {detectedQuests.Count} quête(s) issue(s) de l'analyse");
            if (DialogueUI.Instance != null)
                DialogueUI.Instance.SetPendingQuests(detectedQuests, npcData.name);
        }
    }

    void UseFallback(NPCData npcData, bool isWelcome, string playerMessage)
    {
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log("[AI] Utilisation du mode fallback");
        
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
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log($"[AI] Message sauvé pour {npcName}: {formattedMessage}");
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

    /// <summary>
    /// Injecte un fait dans le contexte IA d'un PNJ (sans l'afficher dans
    /// l'historique visible du joueur). Utilisé p.ex. au retour d'une quête
    /// EXPLORE : le PNJ qui a donné la mission "sait" ce que le joueur a vu.
    /// </summary>
    public void InjectContextForNPC(string npcName, string fact)
    {
        if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(fact)) return;

        if (!conversationsByNpc.TryGetValue(npcName, out var ctx))
        {
            ctx = new List<OpenAIMessage>();
            conversationsByNpc[npcName] = ctx;
        }
        ctx.Add(new OpenAIMessage("system", $"[Information complémentaire à la prochaine reprise] {fact}"));

        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI))
            Debug.Log($"[AI] Contexte injecté pour {npcName} : {fact}");
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
        conversationsByNpc.Clear();
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.AI)) Debug.Log("[AI] Historique des conversations effacé");
    }

    /// <summary>Snapshot des conversations pour le save/load.</summary>
    public ConversationsSaveData GetSaveData()
    {
        var data = new ConversationsSaveData();
        foreach (var kvp in conversationHistories)
        {
            data.histories.Add(kvp.Value);
        }
        foreach (var kvp in conversationsByNpc)
        {
            data.contexts.Add(new ConversationContextEntry { npcName = kvp.Key, messages = new List<OpenAIMessage>(kvp.Value) });
        }
        return data;
    }

    /// <summary>Restaure les conversations depuis une sauvegarde.</summary>
    public void LoadSaveData(ConversationsSaveData data)
    {
        conversationHistories.Clear();
        conversationsByNpc.Clear();
        if (data == null) return;

        if (data.histories != null)
        {
            foreach (var h in data.histories)
            {
                if (h != null && !string.IsNullOrEmpty(h.npcName))
                    conversationHistories[h.npcName] = h;
            }
        }
        if (data.contexts != null)
        {
            foreach (var c in data.contexts)
            {
                if (c != null && !string.IsNullOrEmpty(c.npcName) && c.messages != null)
                    conversationsByNpc[c.npcName] = c.messages;
            }
        }
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
