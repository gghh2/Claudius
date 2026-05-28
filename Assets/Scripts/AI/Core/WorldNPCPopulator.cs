using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Au lancement de la scene Game, peuple le monde de PNJ generes par l'IA
/// en fonction des QuestZones existantes. Pour chaque zone, un appel IA
/// dedie invente entre 1 et 3 PNJ coherents avec le type de zone
/// (marche -> marchands, laboratoire -> scientifiques, etc.) et leur
/// donne un nom, un role et une description. Le prefab NPC_Template est
/// instancie a un spawnPoint random de la zone.
///
/// Apres ce populator, le ShopCatalogGenerator passe sur tous les NPC
/// avec un Shop vide pour generer leur catalogue de boutique.
///
/// Skip si des NPC sont deja en scene (loading de sauvegarde, ou
/// generation deja effectuee en session precedente).
///
/// Singleton auto-bootstrappe — aucun cablage scene necessaire.
/// </summary>
public class WorldNPCPopulator : MonoBehaviour
{
    static WorldNPCPopulator Instance;

    [Tooltip("Delai avant de demarrer la generation.")]
    public float startDelay = 1f;

    [Tooltip("Pause entre deux generations pour ne pas saturer Ollama.")]
    public float interGenDelay = 0.3f;

    [Tooltip("Nom du prefab template dans Assets/Resources/ (sans extension).")]
    public string prefabResourceName = "NPC_Template";

    [Tooltip("Nom du GameObject parent en scene qui contiendra les NPC generes (Hierarchy).")]
    public string parentContainerName = "NPC";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (Instance != null) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game") return;
        var go = new GameObject("WorldNPCPopulator");
        go.AddComponent<WorldNPCPopulator>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(PopulateAllZones());
    }

    IEnumerator PopulateAllZones()
    {
        yield return new WaitForSecondsRealtime(startDelay);

        // Si des NPC existent deja (loading de save, ou generation precedente
        // preservee, ou Play apres avoir ajoute des NPC manuellement), on
        // ne re-genere pas — mais on declenche tout de meme le generateur de
        // catalogues pour combler les Shop vides.
        if (FindObjectsByType<NPC>(FindObjectsSortMode.None).Length > 0)
        {
            Debug.Log("[WorldNPCPopulator] NPC deja presents, generation skippee. Declenchement ShopCatalogGenerator.");
            if (ShopCatalogGenerator.Instance != null)
                ShopCatalogGenerator.Instance.TriggerNow();
            yield break;
        }

        var provider = AIService.Provider;
        if (provider == null || !provider.IsConfigured)
        {
            Debug.Log("[WorldNPCPopulator] Provider IA non configure, skip.");
            yield break;
        }

        // Charge le prefab depuis Resources/ (compatible build).
        GameObject prefab = Resources.Load<GameObject>(prefabResourceName);
        if (prefab == null)
        {
            Debug.LogError($"[WorldNPCPopulator] Prefab introuvable a 'Assets/Resources/{prefabResourceName}.prefab'.");
            yield break;
        }

        Transform parent = GameObject.Find(parentContainerName)?.transform;
        if (parent == null)
        {
            Debug.LogWarning($"[WorldNPCPopulator] Parent '{parentContainerName}' introuvable — les NPC seront a la racine.");
        }

        var zones = FindObjectsByType<QuestZone>(FindObjectsSortMode.None);
        if (zones.Length == 0)
        {
            Debug.LogWarning("[WorldNPCPopulator] Aucune QuestZone en scene, rien a peupler.");
            yield break;
        }

        // Trie par distance au joueur — les zones proches en premier (le joueur
        // les croisera avant que toutes les generations soient finies).
        var player = FindFirstObjectByType<PlayerControllerCC>();
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
        var ordered = zones.OrderBy(z => (z.transform.position - playerPos).sqrMagnitude).ToList();

        int totalSpawned = 0;
        foreach (var zone in ordered)
        {
            int spawned = 0;
            yield return StartCoroutine(PopulateZone(zone, prefab, parent, n => spawned = n));
            totalSpawned += spawned;
            yield return new WaitForSecondsRealtime(interGenDelay);
        }

        Debug.Log($"[WorldNPCPopulator] Generation terminee : {totalSpawned} NPC sur {zones.Length} zone(s).");

        // Maintenant que les NPCs sont en scene, on declenche le generateur
        // de catalogues qui les attendait. Il enchaine sequentiellement, tri
        // par distance au joueur.
        if (totalSpawned > 0 && ShopCatalogGenerator.Instance != null)
        {
            Debug.Log("[WorldNPCPopulator] Declenchement de ShopCatalogGenerator.");
            ShopCatalogGenerator.Instance.TriggerNow();
        }
    }

    IEnumerator PopulateZone(QuestZone zone, GameObject prefab, Transform parent, System.Action<int> onDone)
    {
        // Tirage du nombre EN AMONT : sinon l'IA tire toujours 3 (effet
        // ancrage du upper bound). On lui demande N exact.
        int targetCount = Random.Range(1, 4); // 1..3 inclus
        string targetWord = targetCount == 1 ? "UN seul PNJ"
                          : targetCount == 2 ? "DEUX PNJ"
                          : "TROIS PNJ";

        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system",
                "Tu peuples une zone d'un jeu d'aventure spatiale avec des PNJ. " +
                "Format STRICT : une ligne par PNJ, trois champs separes par ' | '\n" +
                "  Champ 1 : nom complet (INVENTE)\n" +
                "  Champ 2 : role concret (1-3 mots)\n" +
                "  Champ 3 : description courte (10-25 mots)\n\n" +
                "REGLES IMPERATIVES :\n" +
                $"- TU DOIS PRODUIRE EXACTEMENT {targetCount} ligne(s), soit {targetWord}. " +
                "Pas plus, pas moins. Ce nombre est decide par le jeu.\n" +
                "- N'ECRIS JAMAIS la ligne de format / d'entete (ex. 'nom | role | description'). " +
                "Commence DIRECTEMENT par la premiere fiche du premier PNJ.\n" +
                "- PNJ COHERENT avec le TYPE de zone " +
                "(market -> marchand/negociant, laboratory -> scientifique, " +
                "security -> garde, hangar -> pilote/mecanicien, medical -> medecin, " +
                "etc.).\n" +
                "- Noms en francais avec consonance space-opera. INVENTE — n'utilise " +
                "AUCUN exemple de jeu existant ni de personnage celebre. Varie les " +
                "phonemes, les initiales, les structures (prenom seul, prenom+epithete, " +
                "nom propre seul).\n" +
                "- Role concret en 1-3 mots.\n" +
                "- Description francais sans guillemets, personnalite + petit detail unique.\n" +
                "- Aucun preambule, aucun postambule, aucune numerotation, aucun bullet."),
            new OpenAIMessage("user",
                $"Zone :\n- Nom : {zone.zoneName}\n- Type : {zone.zoneType}\n\n" +
                $"Genere EXACTEMENT {targetCount} fiche(s) de PNJ ({targetWord}). Commence directement :")
        };
        // maxTokens scale avec targetCount pour ne pas tronquer si 3, ni
        // gaspiller si 1.
        var request = new AIRequest(messages, temperature: 0.9f, maxTokens: 100 + 100 * targetCount);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        int count = 0;
        yield return StartCoroutine(AIService.Provider.Complete(request, response =>
        {
            sw.Stop();
            if (!response.success)
            {
                Debug.LogWarning($"[WorldNPCPopulator] Echec zone {zone.zoneName} : {response.error}");
                return;
            }
            string raw = (response.text ?? "").Trim();
            count = ParseAndSpawn(zone, raw, prefab, parent, targetCount);
            Debug.Log($"[WorldNPCPopulator] {zone.zoneName} ({sw.Elapsed.TotalSeconds:N1}s, target={targetCount}) -> {count} NPC");
        }));

        onDone?.Invoke(count);
    }

    int ParseAndSpawn(QuestZone zone, string raw, GameObject prefab, Transform parent, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        var lines = raw.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        int spawned = 0;

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            // Strip eventuels bullets / numerotations.
            line = System.Text.RegularExpressions.Regex.Replace(line, @"^[-*•\d]+[\.\)]?\s*", "").Trim();
            if (line.Length == 0) continue;

            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            string name = parts[0].Trim().Trim('"');
            string role = parts[1].Trim().Trim('"');
            string desc = parts[2].Trim().Trim('"');
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(role)) continue;

            // Filtre : la ligne de format/header s'est glissee dans la
            // reponse (ex. 'nom_complet | role | description_courte').
            if (IsTemplatePlaceholder(name, role, desc)) continue;

            Vector3 pos = PickSpawnPosition(zone);
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            if (parent != null) go.transform.SetParent(parent, true);
            go.name = name;

            var npc = go.GetComponent<NPC>();
            if (npc != null)
            {
                npc.npcName = name;
                npc.npcRole = role;
                npc.npcDescription = desc;
                // Couleur aleatoire vive : appliquee au material par
                // NPC.Start (via npcRenderer.material.color = npcColor).
                npc.npcColor = RandomNpcColor();
            }

            spawned++;
            if (spawned >= maxCount) break; // borne par le N decide en amont
        }
        return spawned;
    }

    /// <summary>
    /// True si la ligne ressemble a un re-echo de la ligne de format que
    /// j'ai donnee dans le prompt (le modele la recopie parfois en tete).
    /// </summary>
    bool IsTemplatePlaceholder(string name, string role, string desc)
    {
        string nLo = name.ToLowerInvariant();
        string rLo = role.ToLowerInvariant();
        string dLo = desc.ToLowerInvariant();
        if (nLo.Contains("nom_complet") || nLo.Contains("nom complet")) return true;
        if (rLo == "role" || rLo == "rôle") return true;
        if (dLo.Contains("description_courte") || dLo.Contains("description courte")) return true;
        // Garde-fou supplementaire : nom ou role qui contient le mot "champ".
        if (nLo.StartsWith("champ ") || rLo.StartsWith("champ ")) return true;
        return false;
    }

    /// <summary>
    /// Couleur HSL aleatoire vive (saturation et luminosite controlees)
    /// pour distinguer les NPC visuellement.
    /// </summary>
    Color RandomNpcColor()
    {
        float h = Random.value; // teinte uniformement repartie
        float s = Random.Range(0.55f, 0.85f);
        float v = Random.Range(0.75f, 1.0f);
        return Color.HSVToRGB(h, s, v);
    }

    Vector3 PickSpawnPosition(QuestZone zone)
    {
        // Si la zone a des spawnPoints, on en prend un au hasard ; sinon
        // position du centre de zone.
        var fld = typeof(QuestZone).GetField("spawnPoints",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = fld?.GetValue(zone) as List<Vector3>;
        if (list != null && list.Count > 0)
            return list[Random.Range(0, list.Count)];
        return zone.transform.position;
    }
}
