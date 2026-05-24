using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Rumor
{
    public string id;
    public string text;
    public int day;     // jour in-game où la rumeur est née
}

/// <summary>
/// Pool de rumeurs auto-générées par les événements de jeu (quêtes
/// terminées, zones découvertes, achats notables...). À chaque dialogue,
/// une rumeur fraîche non encore connue du PNJ est injectée dans son
/// contexte — il peut la commenter et la propager.
/// </summary>
public class RumorPool : MonoBehaviour
{
    public static RumorPool Instance { get; private set; }

    [SerializeField] List<Rumor> rumors = new List<Rumor>();
    [SerializeField] HashSet<string> heardBy = new HashSet<string>(); // clé = "npcName|rumorId"
    public IReadOnlyList<Rumor> Rumors => rumors;

    [Tooltip("Une rumeur n'est plus considérée 'fraîche' au-delà de ce nombre de jours.")]
    public int rumorMaxAgeDays = 3;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void AddRumor(string id, string text)
    {
        if (rumors.Exists(r => r.id == id)) return;
        int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;
        rumors.Add(new Rumor { id = id, text = text, day = day });
    }

    /// <summary>
    /// Renvoie une rumeur fraîche (≤ rumorMaxAgeDays) que ce PNJ ne connaît
    /// pas encore. null si aucune candidate.
    /// </summary>
    public Rumor GetFreshRumorFor(string npcName)
    {
        if (rumors.Count == 0) return null;
        int today = GameClock.Instance != null ? GameClock.Instance.Day : 1;
        for (int i = rumors.Count - 1; i >= 0; i--)
        {
            var r = rumors[i];
            if (today - r.day > rumorMaxAgeDays) continue;
            string key = npcName + "|" + r.id;
            if (heardBy.Contains(key)) continue;
            heardBy.Add(key);
            return r;
        }
        return null;
    }

    public RumorSaveData GetSaveData() => new RumorSaveData
    {
        rumors = new List<Rumor>(rumors),
        heardBy = new List<string>(heardBy)
    };

    public void LoadSaveData(RumorSaveData data)
    {
        rumors.Clear();
        heardBy.Clear();
        if (data == null) return;
        if (data.rumors != null) rumors.AddRange(data.rumors);
        if (data.heardBy != null) foreach (var k in data.heardBy) heardBy.Add(k);
    }
}

[System.Serializable]
public class RumorSaveData
{
    public List<Rumor> rumors = new List<Rumor>();
    public List<string> heardBy = new List<string>();
}
