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
        // preservee), on ne touche a rien.
        if (FindObjectsByType<NPC>(FindObjectsSortMode.None).Length > 0)
        {
            Debug.Log("[WorldNPCPopulator] NPC deja presents, generation skippee.");
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
    }

    IEnumerator PopulateZone(QuestZone zone, GameObject prefab, Transform parent, System.Action<int> onDone)
    {
        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system",
                "Tu peuples une zone d'un jeu d'aventure spatiale avec des PNJ. " +
                "Format STRICT, une ligne par PNJ, separateur '|' :\n" +
                "nom_complet | role | description_courte\n\n" +
                "REGLES :\n" +
                "- Genere entre 1 et 3 PNJ COHERENTS avec le TYPE de zone " +
                "(marche -> marchand/negociant, laboratoire -> scientifique, " +
                "security -> garde, hangar -> pilote/mecanicien, etc.).\n" +
                "- Nom complet francais avec consonance space-opera (ex. 'Velka des Etoiles', 'Korvyn le Cendreux').\n" +
                "- Role concret en 1-3 mots (ex. 'Marchand', 'Scientifique', 'Garde Imperial').\n" +
                "- Description courte (10-25 mots) en francais, personnalite + petit detail.\n" +
                "- Aucun preambule ni postambule. Juste les lignes."),
            new OpenAIMessage("user",
                $"Zone :\n- Nom : {zone.zoneName}\n- Type : {zone.zoneType}\n\nGenere ses habitants :")
        };
        var request = new AIRequest(messages, temperature: 0.9f, maxTokens: 300);
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
            count = ParseAndSpawn(zone, raw, prefab, parent);
            Debug.Log($"[WorldNPCPopulator] {zone.zoneName} ({sw.Elapsed.TotalSeconds:N1}s) -> {count} NPC");
        }));

        onDone?.Invoke(count);
    }

    int ParseAndSpawn(QuestZone zone, string raw, GameObject prefab, Transform parent)
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
            }

            spawned++;
            if (spawned >= 3) break; // garde-fou
        }
        return spawned;
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
