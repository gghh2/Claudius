using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Au lancement du jeu, genere en arriere-plan le catalogue de chaque NPC
/// qui en a un vide (Shop component sans articles). L'IA invente les
/// produits, leur prix et leur description en se basant sur le role et la
/// description du PNJ. Traitement sequentiel trie par distance au joueur
/// (les PNJ proches sont prets en premier, le joueur ne les croisera qu'apres
/// que leur catalogue soit pret).
///
/// Si l'IA estime que le PNJ ne devrait rien vendre (ex. un ermite, un
/// philosophe), elle peut repondre VIDE — le catalogue reste vide et
/// l'interaction [B] n'apparait pas au joueur.
///
/// Singleton auto-bootstrappe : aucun cablage en scene necessaire.
/// </summary>
public class ShopCatalogGenerator : MonoBehaviour
{
    public static ShopCatalogGenerator Instance { get; private set; }

    [Tooltip("Delai avant de demarrer la generation (le temps que la scene se stabilise).")]
    public float startDelay = 2f;

    [Tooltip("Pause entre deux generations pour ne pas saturer Ollama.")]
    public float interGenDelay = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (Instance != null) return;
        // Lance uniquement dans la scene Game.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game") return;
        var go = new GameObject("ShopCatalogGenerator");
        go.AddComponent<ShopCatalogGenerator>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Si WorldNPCPopulator est present, c'est LUI qui declenchera la
        // generation a la fin de son peuplement (sinon on tournerait avant
        // que les NPC ne soient spawnes). Sinon (pas de populator, NPC
        // manuels ou save deja chargee), on demarre solo apres startDelay.
        if (FindFirstObjectByType<WorldNPCPopulator>() == null)
            StartCoroutine(GenerateAllCatalogs(initialDelay: startDelay));
    }

    /// <summary>
    /// Lance la generation des catalogues. Public pour permettre a
    /// WorldNPCPopulator de l'enchainer apres son propre peuplement.
    /// </summary>
    public void TriggerNow()
    {
        StartCoroutine(GenerateAllCatalogs(initialDelay: 0f));
    }

    IEnumerator GenerateAllCatalogs(float initialDelay)
    {
        if (initialDelay > 0f) yield return new WaitForSecondsRealtime(initialDelay);

        var provider = AIService.Provider;
        if (provider == null || !provider.IsConfigured)
        {
            Debug.Log("[ShopCatalog] Provider IA non configure, generation skippee.");
            yield break;
        }

        var player = FindFirstObjectByType<PlayerControllerCC>();
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

        var npcs = FindObjectsByType<NPC>(FindObjectsSortMode.None)
            .OrderBy(n => (n.transform.position - playerPos).sqrMagnitude)
            .ToList();

        int generated = 0;
        foreach (var npc in npcs)
        {
            var shop = npc.GetComponent<Shop>();
            if (shop == null) continue;
            if (shop.catalog != null && shop.catalog.Count > 0) continue; // deja rempli (manuellement ou save)

            yield return StartCoroutine(GenerateFor(npc, shop));
            generated++;
            yield return new WaitForSecondsRealtime(interGenDelay);
        }

        Debug.Log($"[ShopCatalog] Generation terminee : {generated} catalogue(s) sur {npcs.Count} NPC.");
    }

    // Roles explicitement commerciaux : catalogue garanti, le modele doit
    // generer. Pour les autres, le modele decide (peut repondre VIDE).
    static readonly System.Collections.Generic.HashSet<string> MerchantRoleKeywords =
        new System.Collections.Generic.HashSet<string>
        {
            "marchand", "marchande", "negociant", "negociante", "négociant", "négociante",
            "commercant", "commerçant", "commercante", "commerçante",
            "vendeur", "vendeuse",
            "tavernier", "taverniere", "tavernière", "aubergiste",
            "armurier", "armuriere", "armurière",
            "trader", "merchant"
        };

    static bool RoleIsMerchant(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        string lo = role.ToLowerInvariant();
        foreach (var kw in MerchantRoleKeywords)
            if (lo.Contains(kw)) return true;
        return false;
    }

    IEnumerator GenerateFor(NPC npc, Shop shop)
    {
        // Distingue 2 modes :
        //   - Marchand explicite : on FORCE la generation (2-5 articles).
        //   - Autre role : on laisse l'IA decider, avec des examples de
        //     'qui vend / qui ne vend pas' pour calibrer son jugement.
        bool isExplicitMerchant = RoleIsMerchant(npc.npcRole);
        string modeInstructions = isExplicitMerchant
            ? "Ce PNJ est un MARCHAND explicite : tu DOIS generer entre 2 et 5 articles. PAS de VIDE."
            : "Ce PNJ N'EST PAS un marchand explicite. Decide si son metier lui permet de " +
              "vendre PLAUSIBLEMENT quelque chose en marge de son activite principale.\n" +
              "  PEUVENT vendre : druide (herbes/talismans), archeologue (trouvailles), " +
              "mecanicien (pieces de rechange), apothicaire/alchimiste/herboriste (potions), " +
              "ferrailleur (debris), explorateur retraite (souvenirs), sage (textes), " +
              "scientifique (echantillons), artisan (objets fabriques).\n" +
              "  NE VENDENT PAS : garde en service, pilote actif, soldat, capitaine en mission, " +
              "fonctionnaire, gardien austere, philosophe pur, ermite recluse.\n" +
              "Si OUI : genere 1 a 3 articles. Si NON : reponds STRICTEMENT par : VIDE";

        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system",
                "Tu generes le catalogue de boutique d'un PNJ d'un jeu d'aventure " +
                "spatiale. Format STRICT, une ligne par article, separateur '|' :\n" +
                "nom_item_en_snake_case | prix_credits | description_courte\n\n" +
                modeInstructions + "\n\n" +
                "REGLES (si tu generes) :\n" +
                "- Articles COHERENTS avec le metier, la personnalite, le lore space-opera. " +
                "Pas de doublons, pas de mots trop modernes.\n" +
                "- Noms en snake_case (herbe_de_lune, lasergun_leger, fragment_de_relique).\n" +
                "- Prix entre 10 et 500 credits, proportionnel a la rarete.\n" +
                "- Description courte (5-15 mots), en francais, sans guillemets.\n" +
                "- Aucun preambule ni postambule. Juste les lignes (ou VIDE)."),
            new OpenAIMessage("user",
                $"PNJ :\n- Nom : {npc.npcName}\n- Role : {npc.npcRole}\n- Description : {npc.npcDescription}\n\nGenere son catalogue :")
        };
        var request = new AIRequest(messages, temperature: 0.85f, maxTokens: 250);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        yield return StartCoroutine(AIService.Provider.Complete(request, response =>
        {
            sw.Stop();
            if (!response.success)
            {
                Debug.LogWarning($"[ShopCatalog] Echec pour {npc.npcName} : {response.error}");
                return;
            }
            string raw = (response.text ?? "").Trim();
            ParseAndApply(npc, shop, raw);
            Debug.Log($"[ShopCatalog] {npc.npcName} ({sw.Elapsed.TotalSeconds:N1}s) -> {shop.catalog.Count} item(s)");
        }));
    }

    void ParseAndApply(NPC npc, Shop shop, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        // Reponse 'VIDE' => pas d'articles, catalogue reste vide.
        if (Regex.IsMatch(raw, @"^\s*VIDE\s*$", RegexOptions.IgnoreCase)) return;

        var lines = raw.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            // Skip d'eventuels marqueurs markdown ou bullets que le modele ajouterait.
            line = Regex.Replace(line, @"^[-*•\d]+[\.\)]?\s*", "").Trim();
            if (line.Length == 0) continue;

            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            string name = parts[0].Trim().ToLowerInvariant().Replace(' ', '_');
            string priceStr = Regex.Match(parts[1], @"\d+").Value;
            string desc = parts[2].Trim().Trim('"');

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(priceStr)) continue;
            if (!int.TryParse(priceStr, out int price)) continue;
            price = Mathf.Clamp(price, 5, 2000);

            shop.catalog.Add(new ShopItem
            {
                itemName = name,
                price = price,
                description = desc
            });

            // Garde-fou : pas plus de 6 items meme si l'IA en propose 20.
            if (shop.catalog.Count >= 6) break;
        }
    }
}
