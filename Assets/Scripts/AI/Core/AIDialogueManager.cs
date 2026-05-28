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
            // Reset explicite des dictionnaires : si 'Enter Play Mode > Reload
            // Domain' est désactivé dans Project Settings, l'Instance et ses
            // dicts peuvent survivre à une Play stop -> conversations fuiteraient
            // d'une partie à la suivante (PNJ qui mentionne un ancien nom de
            // planète). On force la table rase ici.
            conversationHistories.Clear();
            conversationsByNpc.Clear();
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

            // Économie de tokens : si le joueur a quitté SANS répondre (dernier
            // message = assistant), on réutilise tel quel ce message au lieu
            // de re-générer. Exceptions :
            //   1. Le message faisait référence au moment de la journée et
            //      ce moment a changé (« bonsoir » à 14h serait bizarre).
            //   2. Un fait a ete injecte dans le contexte depuis ce dernier
            //      assistant (typiquement InjectGiverCompletionMemory au
            //      turn-in d'une quete) — la cache est STALE, le PNJ redirait
            //      sa demande alors qu'elle est accomplie.
            var last = existing[existing.Count - 1];
            bool canReuse = last.role == "assistant"
                && !TimeOfDayChangedSinceCachedWelcome(npcData.name, last.content)
                && !HasInjectionsSinceLastAssistant(existing);
            if (canReuse)
            {
                if (DialogueUI.Instance != null)
                    DialogueUI.Instance.StartAIDialogue(npcData, $"{npcData.name}: {last.content}");
                return; // Pas d'appel IA, pas de nouvelles injections.
            }

            InjectTimeContext();
            InjectLoreContext(npcData.name);
            InjectRumorContext(npcData.name);
            InjectWorldLoreContext();
            InjectInventoryContext();
            InjectShopCatalogContext(npcData);
            currentConversation.Add(new OpenAIMessage("user",
                "Le joueur revient vous parler. Accueillez-le comme une connaissance, en vous souvenant de vos échanges précédents."));
        }
        else
        {
            currentConversation = new List<OpenAIMessage>();
            conversationsByNpc[npcData.name] = currentConversation;

            currentConversation.Add(new OpenAIMessage("system", BuildSystemPrompt(npcData)));
            InjectTimeContext();
            InjectLoreContext(npcData.name);
            InjectRumorContext(npcData.name);
            InjectWorldLoreContext();
            InjectInventoryContext();
            InjectShopCatalogContext(npcData);

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
            return $@"REGLE LANGUE — ABSOLUE : tu reponds EXCLUSIVEMENT en francais.
JAMAIS un mot, une phrase, un caractere ou un commentaire dans une autre
langue (pas de chinois, anglais, espagnol). JAMAIS de meta-commentaire sur
ce que tu fais. Tu ECRIS DIRECTEMENT la replique du personnage, point.

Vous incarnez un personnage d'un jeu d'aventure spatiale. Restez dans votre rôle, répondez en français, en 1 à 3 phrases.
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
- Vous n'attribuez JAMAIS de mission formelle et vous n'écrivez JAMAIS rien entre crochets (ni code, ni didascalie comme « [je souris] »). Contentez-vous de jouer votre personnage et de discuter.

RÈGLE ABSOLUE — ANTI-HALLUCINATION : si le joueur prétend posséder ou montrer
un objet, vous n'avez le DROIT de réagir QUE si cet objet figure explicitement
dans le message système « Inventaire du joueur » fourni plus loin. Sinon, en
restant DANS VOTRE PERSONNAGE et avec VOS MOTS À VOUS, exprimez que vous ne
voyez rien, doutez, demandez une preuve, ou détournez le sujet — selon votre
tempérament. N'inventez JAMAIS l'apparence, la lumière, les runes, l'énergie
ou les propriétés d'un objet absent de l'inventaire. N'utilisez aucune
formule toute faite : variez vos refus, formulez-les comme votre personnage
les dirait naturellement. Cette règle l'emporte sur toute consigne d'être
engageant ou de rebondir.
{BuildShopInstructions(npcData)}";
        }

        // Utilise la config appropriée — prompt de roleplay PUR (aucune quête).
        return $@"REGLE LANGUE — ABSOLUE : tu reponds EXCLUSIVEMENT en francais.
JAMAIS un mot, une phrase, un caractere ou un commentaire dans une autre
langue (pas de chinois, anglais, espagnol). JAMAIS de meta-commentaire sur
ce que tu fais (pas de 'I'll switch back to character', pas de 'OK, je
reponds maintenant'). Tu ECRIS DIRECTEMENT la replique du personnage, point.

Vous incarnez un personnage d'un jeu d'aventure spatiale. Restez dans votre rôle, répondez en français, en 1 à 3 phrases.
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
jouer votre personnage et de discuter.

RÈGLE ABSOLUE — ANTI-HALLUCINATION : si le joueur prétend posséder ou montrer
un objet, vous n'avez le DROIT de réagir QUE si cet objet figure explicitement
dans le message système « Inventaire du joueur » fourni plus loin. Sinon, en
restant DANS VOTRE PERSONNAGE et avec VOS MOTS À VOUS, exprimez que vous ne
voyez rien, doutez, demandez une preuve, ou détournez le sujet — selon votre
tempérament. N'inventez JAMAIS l'apparence, la lumière, les runes, l'énergie
ou les propriétés d'un objet absent de l'inventaire. N'utilisez aucune
formule toute faite : variez vos refus, formulez-les comme votre personnage
les dirait naturellement. Cette règle l'emporte sur toute consigne d'être
engageant ou de rebondir.
{BuildShopInstructions(npcData)}";
    }

    /// <summary>
    /// Cherche le Shop du PNJ par nom dans la scene et l'ouvre. Differe
    /// pour laisser le temps au joueur de lire la phrase d'invite avant que
    /// le panneau de boutique se superpose au dialogue.
    /// </summary>
    IEnumerator OpenShopForDelayed(string npcName, float delay)
    {
        Debug.Log($"[ShopOpen] Scheduled in {delay}s for '{npcName}'");
        // Realtime obligatoire : pendant un dialogue, Time.timeScale=0
        // (UnifiedUIManager pause le jeu) — un WaitForSeconds normal
        // ne s'ecoule jamais et la coroutine reste bloquee.
        yield return new WaitForSecondsRealtime(delay);
        foreach (var npc in FindObjectsByType<NPC>(FindObjectsSortMode.None))
        {
            if (npc.npcName != npcName) continue;
            var shop = npc.GetComponent<Shop>();
            if (shop == null) { Debug.LogWarning($"[ShopOpen] NPC '{npcName}' trouve mais pas de Shop component"); yield break; }
            if (ShopUI.Instance == null)
            {
                var go = new GameObject("ShopUI");
                go.AddComponent<ShopUI>();
            }
            Debug.Log($"[ShopOpen] Opening shop for '{npcName}'");
            ShopUI.Instance.OpenFor(shop);
            yield break;
        }
        Debug.LogWarning($"[ShopOpen] NPC '{npcName}' introuvable en scene");
    }

    /// <summary>
    /// Instructions specifiques pour les marchands. L'OUVERTURE de la
    /// boutique est decidee par un appel d'analyse separe
    /// (AnalyzeForShopIntent), pas par un token dans le chat. Le chat
    /// doit juste preparer l'invite si le joueur demande a voir.
    /// </summary>
    string BuildShopInstructions(NPCData npcData)
    {
        if (!npcData.hasShop) return string.Empty;
        return @"

ROLE MARCHAND — distingue 3 cas d'usage selon ce que le joueur demande :

1. Le joueur veut VOIR / ACHETER tes articles (« qu'as-tu », « montre-moi ta
   boutique », « je veux acheter »...) : invite-le verbalement (« Approche,
   regarde mon etalage »). Le panneau de boutique s'ouvre automatiquement
   apres ta reponse, tu n'as RIEN a faire de plus. N'INVENTE JAMAIS un
   article absent de ton catalogue 'Catalogue de TA boutique' fourni plus
   loin.

2. Le joueur cherche DU TRAVAIL ou DES CREDITS (« tu as du boulot »,
   « je n'ai pas de credit »...) : ne le sales-pitch PAS. Propose-lui un
   service ou un travail naturel pour un marchand (ramener une marchandise
   rare, livrer un colis, retrouver un fournisseur disparu) — c'est plus
   coherent : il pourra revenir acheter ensuite avec les credits gagnes.
   Ne liste PAS tes articles dans ce cas.

3. Bavardage / autre : roleplay normal, sans pousser ni la vente ni la
   quete.

JAMAIS de cumul des cas 1 et 2 dans la meme reponse — c'est confus pour le
joueur. Si tu hesites entre les deux, prends le cas 2 (proposer du travail
plutot que de vendre a un joueur sans credit).";
    }

    /// <summary>
    /// Injecte le CATALOGUE REEL du marchand dans le contexte IA. Sans
    /// cela le PNJ marchand hallucine ses propres produits (ex. invente
    /// 'reliques antiques, gemmes du Createur, armes legendaires' alors
    /// qu'il a juste 1 fusil et 1 gourde). Vide si pas de Shop.
    /// </summary>
    void InjectShopCatalogContext(NPCData npcData)
    {
        if (!npcData.hasShop) return;

        // Cherche le Shop component dans la scene par nom.
        Shop shop = null;
        foreach (var npc in FindObjectsByType<NPC>(FindObjectsSortMode.None))
        {
            if (npc.npcName != npcData.name) continue;
            shop = npc.GetComponent<Shop>();
            break;
        }
        if (shop == null || shop.catalog == null) return;

        if (shop.catalog.Count == 0)
        {
            currentConversation.Add(new OpenAIMessage("system",
                "Catalogue de TA boutique (LISTE EXHAUSTIVE) : VIDE. Tu n'as " +
                "RIEN a vendre actuellement. Si le joueur demande, dis-le " +
                "honnetement (rupture de stock, en attente de cargaison, etc) " +
                "et n'invente AUCUN produit."));
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append("Catalogue de TA boutique (LISTE EXHAUSTIVE — tu n'as RIEN d'AUTRE) : ");
        for (int i = 0; i < shop.catalog.Count; i++)
        {
            var it = shop.catalog[i];
            if (i > 0) sb.Append(", ");
            sb.Append(it.itemName).Append(" (").Append(it.price).Append(" credits");
            if (!string.IsNullOrWhiteSpace(it.description))
                sb.Append(" — ").Append(it.description);
            sb.Append(")");
        }
        sb.Append(". REGLE STRICTE : si on te demande ce que tu vends, ");
        sb.Append("enumere UNIQUEMENT ces produits. N'invente JAMAIS d'autres ");
        sb.Append("articles (pas de 'reliques antiques', 'armes legendaires', ");
        sb.Append("'cristaux mystiques' improvises). Tu peux les decrire avec ");
        sb.Append("ton ton de marchand, mais le contenu doit etre exactement ");
        sb.Append("cette liste.");

        currentConversation.Add(new OpenAIMessage("system", sb.ToString()));
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

            // Strip de tout token [SHOP] eventuel : le chat ne devrait pas
            // l'emettre, l'analyse separee (AnalyzeForShopIntent) decide.
            // Si Ollama le laisse fuir on ne veut pas l'afficher au joueur.
            if (npcData.hasShop)
            {
                aiResponse = System.Text.RegularExpressions.Regex.Replace(
                    aiResponse, @"\[SHOP\]", "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            }

            // Sécurité : le chat ne doit pas produire de token QUEST. Si le
            // modèle en glisse un malgré tout, on le retire de l'affichage —
            // il ne crée aucune quête (la détection est faite par l'appel
            // d'analyse séparé).
            if (QuestTokenDetector.Instance != null)
                aiResponse = QuestTokenDetector.Instance.CleanMessageFromTokens(aiResponse);

            // Token [PLANET:Nom] : si le PNJ a invente un nom de planete,
            // il l'inclut via ce token. On le capture et on le retire avant
            // affichage. La regex tolere tout token mal forme.
            aiResponse = ExtractAndStripPlanetToken(aiResponse);


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

                // Appel 3 — analyse d'intention SHOP, parallele a l'analyse
                // de quete. Plus fiable que de compter sur Ollama pour
                // emettre [SHOP] dans le chat.
                Debug.Log($"[ShopIntent] dispatch check — npc={npcData.name} hasShop={npcData.hasShop}");
                if (npcData.hasShop)
                    StartCoroutine(AnalyzeForShopIntent(npcData, playerMessage));
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
[QUEST:FETCH:objet:zone:quantité]        — rapporter des objets ordinaires
[QUEST:DELIVERY:objet:destinataire:zone] — livrer quelque chose à quelqu'un
[QUEST:EXPLORE:zone]                     — explorer une zone
[QUEST:TALK:personnage:zone]             — aller parler à quelqu'un
[QUEST:INTERACT:objet:zone]              — interagir avec un objet
[QUEST:TREASURE:nom_du_tresor]           — DÉTERRER un trésor à un endroit aléatoire de la carte (PAS DE ZONE — le jeu place le trésor)

ZONES VALIDES pour les types AVEC zone (utilise UNIQUEMENT celles-ci) : laboratory, hangar, market, security, residential, engineering, medical, storage, ruins
(TREASURE n'a pas de zone — son emplacement est tiré au hasard sur la carte.)

CHOIX DU TYPE :
- TRIGGER TREASURE : si la conversation contient les mots « trésor », « enfoui »,
  « caché », « oublié », « ancien », « déterrer », « creuser », « relique »,
  « fragment », « fouille », « jadis » → tu DOIS répondre avec
  [QUEST:TREASURE:nom_invente]. Même si le PNJ n'a pas d'objet précis en
  tête : invente un nom de trésor évocateur (« relique_oubliee »,
  « fragment_stellaire », « medaille_des_anciens »...). Le joueur EXPRIME
  son envie de chercher un trésor → c'est suffisant.
- Si le sujet est un objet quelconque qu'on récupère dans une zone connue
  (outils, échantillons, marchandises, paquets) → FETCH.
- En cas d'hésitation entre FETCH 'trésor' et TREASURE : choisis TOUJOURS TREASURE.

RÈGLES :
- La quête doit découler d'un sujet CONCRET de la conversation : un objet, un lieu, un problème ou un besoin réellement évoqué. Si le joueur exprime de l'intérêt mais qu'aucun sujet concret n'a été abordé, réponds NONE.
- FETCH : si le joueur parle d'UN seul objet, la quantité est 1.
- Le destinataire d'une DELIVERY et la cible d'un TALK sont des personnages avec un nom propre inventé. RÈGLE STRICTE : invente un prénom (et éventuellement un nom/épithète). EXEMPLES VALIDES : « Maître Orin », « Dame Sevra », « Korvyn », « Yliss l'Errante », « Capitaine Brann ». INTERDIT : « le garde », « l'apothicaire », « un marchand », « le frère », « le destinataire », « contact », « informateur » — tout terme générique est REFUSÉ. INTERDIT : un nom de zone (hangar, market, etc.) en position destinataire/cible.
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

    // Appel 3 — analyse SHOP : decide si le joueur demande explicitement a
    // voir la boutique du marchand. Plus fiable que d'attendre que le chat
    // emette [SHOP] (qwen2.5 / Ollama suivent mal cette instruction).
    IEnumerator AnalyzeForShopIntent(NPCData npcData, string playerMessage)
    {
        Debug.Log($"[ShopIntent] START — npc={npcData.name} message='{playerMessage}'");
        if (string.IsNullOrWhiteSpace(playerMessage)) { Debug.Log("[ShopIntent] message vide, skip"); yield break; }
        if (ShopUI.Instance != null && ShopUI.Instance.IsOpen) { Debug.Log("[ShopIntent] deja ouverte, skip"); yield break; }

        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system",
                "Tu es un analyseur d'intention pour un jeu d'aventure. Le PNJ a une " +
                "boutique. Le joueur vient d'envoyer un message. Determine si le joueur " +
                "demande EXPLICITEMENT a voir / consulter / ouvrir la boutique, le " +
                "catalogue, le stock, les marchandises, ou s'il veut acheter. " +
                "Reponds STRICTEMENT par OUI ou NON. Aucun autre texte. " +
                "Bavardage = NON. Mention en passant = NON. Question vague = NON. " +
                "Demande directe = OUI."),
            new OpenAIMessage("user", $"Message du joueur : « {playerMessage} »\nReponse : OUI ou NON ?")
        };
        var request = new AIRequest(messages, 0.1f, 4);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        yield return StartCoroutine(AIService.Provider.Complete(request, response =>
        {
            stopwatch.Stop();
            if (!response.success)
            {
                Debug.LogWarning($"[ShopIntent] Echec : {response.error}");
                return;
            }
            string raw = (response.text ?? string.Empty).Trim().ToUpperInvariant();
            bool yes = raw.StartsWith("OUI") || raw.StartsWith("YES");
            // Log inconditionnel : critique pour diagnostiquer pourquoi
            // la boutique ne s'ouvre pas malgre une demande claire.
            Debug.Log($"[ShopIntent] ({stopwatch.Elapsed.TotalSeconds:N1}s) raw='{raw}' -> open={yes}");
            if (yes)
                StartCoroutine(OpenShopForDelayed(npcData.name, 0.8f));
        }));
    }

    // Traite la sortie de l'appel 2 : extrait un éventuel token (validé par
    // QuestTokenDetector), le journalise, et le transmet à l'UI si une quête en sort.
    void ProcessQuestAnalysis(string analysisOutput, NPCData npcData, string playerMessage, string chatReply, double seconds)
    {
        string raw = (analysisOutput ?? string.Empty).Trim();
        // Log inconditionnel : sortie d'analyse visible en build pour le diagnostic
        // (sans ça, impossible de savoir pourquoi un type de quête n'est jamais
        // émis — NONE silencieux versus token réel mais rejeté par validation).
        Debug.Log($"[QuestAnalysis] Sortie ({seconds:N1} s) : {raw}");

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
    /// Noms de tous les PNJ avec qui le joueur a deja parle. Lu par
    /// l'autocompletion de dialogue.
    /// </summary>
    public IEnumerable<string> GetSpokenNpcNames()
    {
        foreach (var kvp in conversationHistories)
        {
            if (kvp.Value != null && kvp.Value.hasSpokenBefore)
                yield return kvp.Key;
        }
    }

    /// <summary>
    /// Ajoute un message système rappelant l'heure in-game et le moment de la
    /// journée à la conversation en cours. Appelé à chaque démarrage de
    /// dialogue pour que le PNJ puisse réagir au temps qui passe.
    /// </summary>
    void InjectTimeContext()
    {
        if (GameClock.Instance == null) return;
        string moment = GameClock.Instance.TimeOfDayLabel();
        string now = GameClock.Instance.FormatNow();
        currentConversation.Add(new OpenAIMessage("system",
            $"Contexte temporel : nous sommes au {now} ({moment}). " +
            "Vous êtes conscient de l'heure et du moment de la journée. Adaptez " +
            "votre ton et vos références si pertinent (sans le rabâcher). " +
            "Ne JAMAIS donner une heure différente de celle-ci si on vous demande l'heure."));
    }

    /// <summary>
    /// Cohérence du monde : si un nom de planète a déjà été établi par un PNJ
    /// précédent, on l'injecte. Sinon on demande au PNJ d'en inventer un et
    /// de le mentionner naturellement — le post-traitement de la réponse
    /// extraira le nom (premier mot capitalisé proche du mot "planète").
    /// </summary>
    /// <summary>
    /// Detecte le token [PLANET:Nom] dans la reponse IA et fixe le nom dans
    /// WorldLore. Retourne la reponse SANS le token (a afficher au joueur).
    /// No-op si le nom est deja fixe (le token est tout de meme strippe).
    /// </summary>
    string ExtractAndStripPlanetToken(string aiResponse)
    {
        if (string.IsNullOrEmpty(aiResponse)) return aiResponse;

        var rx = new System.Text.RegularExpressions.Regex(
            @"\[PLANET:([^\]]+)\]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var m = rx.Match(aiResponse);
        if (m.Success && WorldLore.Instance != null && !WorldLore.Instance.HasPlanetName)
        {
            string candidate = m.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(candidate) && candidate.Length >= 3 && candidate.Length <= 30)
            {
                WorldLore.Instance.SetPlanetName(candidate);
            }
        }
        // Strip toujours, meme si on n'a rien capture (cache un eventuel
        // token mal forme et evite que le joueur le voie).
        return rx.Replace(aiResponse, "").Trim();
    }

    /// <summary>
    /// Détermine si le message en cache est devenu obsolète à cause du temps.
    /// Logique : on regarde si le message mentionne un libellé temporel
    /// DIFFÉRENT du libellé actuel. Si la cache dit "matin" et on est toujours
    /// le matin → reuse OK. Si la cache dit "soir" et on est le matin → re-gen.
    /// Si la cache ne mentionne aucun libellé temporel → reuse OK.
    /// </summary>
    bool TimeOfDayChangedSinceCachedWelcome(string npcName, string cachedMessage)
    {
        if (GameClock.Instance == null) return false;
        string current = GameClock.Instance.TimeOfDayLabel().ToLowerInvariant();
        if (string.IsNullOrEmpty(cachedMessage)) return false;
        string m = cachedMessage.ToLowerInvariant();

        // Tous les libellés temporels possibles (alignés sur GameClock.TimeOfDayLabel).
        string[] labels = { "matin", "midi", "après-midi", "apres-midi", "soir", "nuit", "aube", "aurore", "crépuscule", "crepuscule" };

        bool mentionsCurrent = m.Contains(current);
        bool mentionsAnyOther = false;
        foreach (var l in labels)
        {
            if (l == current) continue;
            // 'midi' est inclus dans 'après-midi' : on s'assure de matcher le mot entier.
            if (System.Text.RegularExpressions.Regex.IsMatch(m, $@"\b{System.Text.RegularExpressions.Regex.Escape(l)}\b"))
            {
                mentionsAnyOther = true;
                break;
            }
        }

        // Obsolète si le message évoque un autre moment de la journée que celui en cours.
        return mentionsAnyOther && !mentionsCurrent;
    }

    /// <summary>
    /// True si une injection (system message) a ete ajoutee dans la cache
    /// APRES le dernier message assistant. Sert a invalider la reprise sans
    /// IA quand un fait nouveau a ete injecte (typiquement
    /// InjectGiverCompletionMemory au turn-in de quete) — sinon le PNJ
    /// redirait verbatim sa demande de quete alors qu'elle est accomplie.
    /// </summary>
    bool HasInjectionsSinceLastAssistant(List<OpenAIMessage> conversation)
    {
        // On scanne depuis la fin et on note s'il y a au moins un system
        // AVANT de rencontrer le dernier assistant.
        bool sawSystem = false;
        for (int i = conversation.Count - 1; i >= 0; i--)
        {
            var msg = conversation[i];
            if (msg.role == "assistant") return sawSystem;
            if (msg.role == "system") sawSystem = true;
        }
        return sawSystem;
    }

    void InjectWorldLoreContext()
    {
        if (WorldLore.Instance == null) return;

        if (WorldLore.Instance.HasPlanetName)
        {
            currentConversation.Add(new OpenAIMessage("system",
                $"Lore monde : nous sommes sur la planète {WorldLore.Instance.PlanetName}. " +
                "Si vous évoquez ce monde, utilisez ce nom — ne réinventez surtout pas. " +
                "N'utilisez PAS le token [PLANET:...] : le nom est deja fixe."));
        }
        else
        {
            currentConversation.Add(new OpenAIMessage("system",
                "Lore monde : la planète sur laquelle vous vivez n'a pas encore été " +
                "nommée. Quand vous l'evoquez pour la PREMIERE fois (entree en matiere, " +
                "remarque naturelle), inventez un nom court (1-3 syllabes, consonance " +
                "space-opera) et collez UN SEUL token [PLANET:NomChoisi] a la fin de votre " +
                "phrase — le token est SILENCIEUX pour le joueur (retire avant affichage) " +
                "mais verrouille le nom pour TOUS les futurs dialogues. Sans ce token, le " +
                "nom inventé sera perdu et un autre PNJ en proposera un different. " +
                "EVITE les exemples de jeux video connus (Krynn, Tatooine, Arrakis...) — " +
                "invente VRAIMENT."));
        }
    }

    /// <summary>
    /// Si le joueur a récemment trouvé une note que ce PNJ ne connaît pas
    /// encore, on injecte le contenu dans son contexte — il peut s'en
    /// servir comme entrée en matière ("Vous portez là un parchemin
    /// curieux...") ou réagir si le joueur en parle.
    /// </summary>
    void InjectLoreContext(string npcName)
    {
        if (LoreLibrary.Instance == null) return;
        var note = LoreLibrary.Instance.GetUninjectedNoteFor(npcName);
        if (note == null) return;
        currentConversation.Add(new OpenAIMessage("system",
            $"Information complémentaire : le voyageur a trouvé une note intitulée " +
            $"« {note.title} » qui dit : « {note.content} ». Vous pouvez y faire référence " +
            "si la conversation s'y prête (par curiosité, par lecture mentale...) mais " +
            "ne pas l'imposer."));
    }

    /// <summary>
    /// Glisse une rumeur fraîche dans le contexte. Sert à propager les
    /// exploits du joueur entre PNJ — chacun en apprend une au max par
    /// dialogue, marquée comme déjà entendue (RumorPool gère le suivi).
    /// </summary>
    void InjectRumorContext(string npcName)
    {
        if (RumorPool.Instance == null) return;
        var rumor = RumorPool.Instance.GetFreshRumorFor(npcName);
        if (rumor == null) return;
        currentConversation.Add(new OpenAIMessage("system",
            $"Rumeur entendue récemment : « {rumor.text} » Vous pouvez l'évoquer " +
            "naturellement si la conversation s'y prête, comme un on-dit, sans en " +
            "faire toute une affaire."));
    }

    /// <summary>
    /// Liste ce que le joueur porte pour que le PNJ puisse rebondir s'il y
    /// fait reference. Les items "remarquables" (notes, items de quete,
    /// tresors) peuvent etre evoques spontanement par le PNJ ; les autres,
    /// uniquement si le joueur les mentionne. Si le joueur evoque un objet
    /// ABSENT de cette liste, le PNJ doit douter au lieu de jouer le jeu.
    /// </summary>
    void InjectInventoryContext()
    {
        if (PlayerInventory.Instance == null) return;
        var items = PlayerInventory.Instance.GetAllItems();
        if (items == null || items.Count == 0) return;

        // Format compact, separe les remarquables des banals pour guider le
        // niveau de reactivite du PNJ.
        var remarkable = new System.Text.StringBuilder();
        var ordinary = new System.Text.StringBuilder();
        foreach (var it in items)
        {
            bool isReadable = !string.IsNullOrEmpty(it.readableContent);
            bool isQuest = !string.IsNullOrEmpty(it.questId);
            string label = $"{it.itemName}{(it.quantity > 1 ? $" x{it.quantity}" : "")}";
            if (isReadable) label += " (note)";
            else if (isQuest) label += " (objet de quete)";

            var sb = (isReadable || isQuest) ? remarkable : ordinary;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(label);
        }

        var msg = new System.Text.StringBuilder("Inventaire du joueur (LISTE EXHAUSTIVE — il ne possede RIEN d'autre) : ");
        if (remarkable.Length > 0) msg.Append("[remarquables] ").Append(remarkable);
        if (remarkable.Length > 0 && ordinary.Length > 0) msg.Append(" ; ");
        if (ordinary.Length > 0) msg.Append("[divers] ").Append(ordinary);
        msg.Append(". REGLE STRICTE : tout objet ABSENT de cette liste n'existe PAS dans les mains du joueur. ");
        msg.Append("Cela couvre AUSSI les references vagues : si le joueur dit \"regardez\", \"tenez\", \"voici\", \"je vous montre\", \"il est la\", sans nommer un objet PRESENT dans la liste, c'est du vide — tu ne vois RIEN. ");
        msg.Append("Tu n'inventes JAMAIS la description, la lumiere, les runes, l'energie, les proprietes ou meme la forme d'un objet hors-liste. ");
        msg.Append("Tu refuses avec TES PROPRES MOTS, dans ton personnage, en variant la formulation (jamais deux fois la meme phrase). Pas de yes-and, jamais. ");
        msg.Append("Pour les objets DE la liste : tu peux rebondir naturellement, avec mesure. Les [remarquables] peuvent meme amorcer le sujet si la conversation s'y prete ; ");
        msg.Append("les [divers], seulement si le joueur les mentionne. Sois nuance, pas omniscient — interesse-toi a l'objet sans pretendre tout en savoir.");

        currentConversation.Add(new OpenAIMessage("system", msg.ToString()));
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
