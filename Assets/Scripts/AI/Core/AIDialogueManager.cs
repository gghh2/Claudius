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
        
        // Check if there's an active quest with this NPC
        string initialUserMessage = "Le joueur s'approche de vous. ";
        
        if (QuestJournal.Instance != null)
        {
            var activeQuests = QuestJournal.Instance.GetActiveQuests();
            var npcActiveQuest = activeQuests.FirstOrDefault(q => q.giverNPCName == npcData.name);
            
            if (npcActiveQuest != null)
            {
                // Force the AI to talk about the active quest
                string questDetails = $"quête '{npcActiveQuest.description}' (Progression: {npcActiveQuest.GetProgressText()})";
                initialUserMessage = $"Le joueur qui a votre {questDetails} s'approche de vous. VOUS DEVEZ ABSOLUMENT lui demander comment se passe cette mission et l'encourager selon sa progression. NE proposez PAS de nouvelle quête.";
                
                // Add a system message to reinforce this
                currentConversation.Add(new OpenAIMessage("system", $"RAPPEL IMPORTANT: Le joueur a une quête active avec vous: {questDetails}. Vous DEVEZ en parler en premier!"));
            }
            else
            {
                initialUserMessage = "Le joueur s'approche de vous. Saluez-le de manière naturelle selon votre personnalité.";
            }
        }
        else
        {
            initialUserMessage = "Le joueur s'approche de vous. Saluez-le de manière naturelle selon votre personnalité.";
        }
        
        currentConversation.Add(new OpenAIMessage("user", initialUserMessage));
        
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
        
        // Check for active quest FIRST
        string activeQuestInfo = "";
        
        if (QuestJournal.Instance != null)
        {
            var activeQuests = QuestJournal.Instance.GetActiveQuests();
            var npcActiveQuest = activeQuests.FirstOrDefault(q => q.giverNPCName == npcData.name);
            
            if (npcActiveQuest != null)
            {
                Debug.Log($"🎯 QUÊTE ACTIVE DÉTECTÉE: {npcActiveQuest.description} - Progression: {npcActiveQuest.GetProgressText()}");
                activeQuestInfo = $@"
🔴🔴🔴 ATTENTION PRIORITAIRE 🔴🔴🔴
LE JOUEUR A UNE QUÊTE ACTIVE AVEC VOUS:
- Quête: {npcActiveQuest.description}
- Progression: {npcActiveQuest.GetProgressText()}

VOUS DEVEZ OBLIGATOIREMENT:
1. Commencer par demander des nouvelles de cette quête
2. Montrer que vous vous en souvenez
3. L'encourager selon sa progression
4. NE PAS proposer de nouvelle quête
🔴🔴🔴🔴🔴🔴🔴🔴🔴🔴
";
            }
        }
        
        if (configToUse == null)
        {
            Debug.LogError($"❌ Aucune config trouvée pour le rôle: {npcData.role}");
            
            // Fallback avec l'ancien système
            return $@"🔴 INSTRUCTION CRITIQUE: Quand on vous demande une mission/quête/travail, vous DEVEZ inclure un token [QUEST:...] dans votre réponse!
{activeQuestInfo}
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
{activeQuestInfo}
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
Vous avez déjà donné une mission à ce voyageur: '{npcActiveQuest.questTitle}'
Progression: {npcActiveQuest.GetProgressText()}

🚫 NE DONNEZ PAS DE NOUVELLE QUÊTE!

✅ VOUS DEVEZ:
- Demander comment se passe la mission
- Montrer que vous vous souvenez de la quête donnée
- Encourager le voyageur selon sa progression
- Donner des conseils si la quête n'est pas terminée
- Féliciter si la quête est complétée

EXEMPLES:
- Si progression 0%: 'Alors, avez-vous commencé à chercher [objet] ?'
- Si progression partielle: 'Je vois que vous avez trouvé X sur Y ! Continuez !'
- Si progression 100%: 'Excellent ! Vous avez tout trouvé ! Revenez me voir pour votre récompense !'";
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
❗ IMPORTANT POUR FETCH: Si vous dites 'UN' ou 'UNE' objet, la quantité DOIT être 1, pas 2 !
   Exemple CORRECT: 'Trouvez-moi UN cristal' → [QUEST:FETCH:cristal:laboratory:1]
   Exemple INCORRECT: 'Trouvez-moi UN cristal' → [QUEST:FETCH:cristal:laboratory:2] ❌

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
❗ IMPORTANT POUR FETCH: Si vous dites 'UN' ou 'UNE' objet, la quantité DOIT être 1, pas 2 !
   Exemple CORRECT: 'Trouvez-moi UN cristal' → [QUEST:FETCH:cristal:laboratory:1]
   Exemple INCORRECT: 'Trouvez-moi UN cristal' → [QUEST:FETCH:cristal:laboratory:2] ❌

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
Joueur: 'Avez-vous du travail pour moi ?'
Vous: 'Justement ! Récupérez ce colis urgent pour moi [QUEST:FETCH:colis_urgent:hangar:1] et je vous paierai bien.'

AUTRES EXEMPLES:
- 'J'ai besoin d'UN seul cristal rare ! Trouvez-le [QUEST:FETCH:cristal_rare:market:1] au marché.' ✅
- 'J'ai besoin de marchandises ! Trouvez-moi [QUEST:FETCH:cristaux_rares:market:5] au marché.'
- 'Livrez ce paquet [QUEST:DELIVERY:paquet_secret:garde_imperial:security] au garde impérial.'

❌ ERREUR COMMUNE: 'Trouvez-moi UN cristal' avec [QUEST:FETCH:cristal:zone:2] - Si c'est UN, mettez 1 !
🔴 RÈGLE ABSOLUE: UN/UNE = 1, DES/PLUSIEURS = 2+

⚠️ RAPPEL CRUCIAL: Le token [QUEST:...] DOIT être dans votre message sinon AUCUNE quête ne sera créée!";

            case "scientifique":
                return @"EXEMPLES DE RÉPONSES AVEC TOKENS:
Joueur: 'Avez-vous besoin d'aide ?'
Vous: 'Mon échantillon UNIQUE a disparu ! Retrouvez-le [QUEST:FETCH:echantillon_alien:laboratory:1] s'il vous plaît.' ✅

AUTRES EXEMPLES:
- 'J'ai perdu UN prototype ! [QUEST:FETCH:prototype_experimental:laboratory:1]' ✅
- 'Mes TROIS échantillons ont disparu ! [QUEST:FETCH:echantillon_test:laboratory:3]' ✅
- 'Explorez cette zone mystérieuse [QUEST:EXPLORE:ruins] et rapportez vos découvertes.'
- 'Allez parler à mon assistant [QUEST:TALK:assistant_perdu:medical] dans la baie médicale.'

🔴 ATTENTION: UN/UNE objet = quantité 1, pas 2 !

⚠️ RAPPEL CRUCIAL: Le token [QUEST:...] DOIT être dans votre message sinon AUCUNE quête ne sera créée!";

            case "garde impérial":
                return @"EXEMPLES DE RÉPONSES AVEC TOKENS:
Joueur: 'Une mission pour moi ?'
Vous: 'Zone suspecte détectée. Inspectez [QUEST:EXPLORE:ruins] et faites votre rapport.'

AUTRES EXEMPLES:
- 'Récupérez L'UNIQUE artefact [QUEST:FETCH:artefact_ancien:ruins:1]' ✅ (L'UNIQUE = 1)
- 'Trouvez UNE preuve [QUEST:FETCH:preuve_infiltration:security:1]' ✅ (UNE = 1)
- 'Collectez TOUS les rapports, il y en a cinq [QUEST:FETCH:rapport_securite:security:5]' ✅
- 'Interagissez avec le terminal de sécurité [QUEST:INTERACT:terminal_securite:security] pour vérifier les accès.'

🔴 RÈGLE MILITAIRE: Soyez PRÉCIS sur les quantités !

⚠️ RAPPEL CRUCIAL: Le token [QUEST:...] DOIT être dans votre message sinon AUCUNE quête ne sera créée!";

            default:
                return @"EXEMPLES GÉNÉRIQUES:
- 'Récupérez MON objet perdu [QUEST:FETCH:objet_personnel:residential:1]' ✅ (MON = 1)
- 'J'ai perdu MES TROIS clés [QUEST:FETCH:cle_perdue:residential:3]' ✅ (TROIS = 3)
- 'Trouvez UNE pièce rare [QUEST:FETCH:piece_rare:storage:1]' ✅ (UNE = 1, PAS 2!)
- 'Explorez cette zone suspecte [QUEST:EXPLORE:hangar]'
- 'Parlez à mon contact [QUEST:TALK:informateur:market] au marché'

💡 MÉMO: UN/UNE/MON/MA = 1 | DES/MES/PLUSIEURS = 2+

⚠️ RAPPEL CRUCIAL: Le token [QUEST:...] DOIT être dans votre message sinon AUCUNE quête ne sera créée!";
        }
    }
    
    IEnumerator GetAIResponse(NPCData npcData, bool isWelcome)
    {
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowLoadingState(true);
        }

        // Passe par l'abstraction IA (AIService) au lieu d'appeler OpenAI en dur.
        var request = new AIRequest(
            new List<OpenAIMessage>(currentConversation),
            aiConfig.model, aiConfig.temperature, aiConfig.maxTokens);

        Debug.Log($"Envoi requête IA pour {npcData.name}");

        yield return StartCoroutine(AIService.Provider.Complete(request, response =>
        {
            if (DialogueUI.Instance != null)
            {
                DialogueUI.Instance.ShowLoadingState(false);
            }

            if (response.success)
            {
                ProcessAIResponse(response.text, npcData, isWelcome);
            }
            else
            {
                Debug.LogError($"Erreur IA : {response.error}");
                UseFallback(npcData, isWelcome, "");
            }
        }));
    }

    void ProcessAIResponse(string aiContent, NPCData npcData, bool isWelcome)
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
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur traitement réponse IA : {e.Message}");
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
