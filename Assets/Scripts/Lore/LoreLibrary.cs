using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LoreEntry
{
    public string id;
    public string title;
    public string content;
}

/// <summary>
/// Registre des notes / indices écrits trouvés par le joueur. Singleton
/// persistant. Quand un dialogue commence, une note récente est injectée
/// dans le contexte du PNJ — il peut y faire référence.
/// </summary>
public class LoreLibrary : MonoBehaviour
{
    public static LoreLibrary Instance { get; private set; }

    [SerializeField] List<LoreEntry> entries = new List<LoreEntry>();
    public IReadOnlyList<LoreEntry> Entries => entries;

    [SerializeField] HashSet<string> injectedIds = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterNote(string id, string title, string content)
    {
        if (entries.Exists(e => e.id == id)) return;
        entries.Add(new LoreEntry { id = id, title = title, content = content });
    }

    public bool HasNote(string id) => entries.Exists(e => e.id == id);

    /// <summary>
    /// Renvoie une note non encore injectée dans la mémoire d'un PNJ
    /// (registry par-PNJ pour éviter le ressassement). null si aucune
    /// nouvelle note à injecter.
    /// </summary>
    public LoreEntry GetUninjectedNoteFor(string npcName)
    {
        if (entries.Count == 0) return null;
        foreach (var e in entries)
        {
            string key = npcName + "|" + e.id;
            if (!injectedIds.Contains(key))
            {
                injectedIds.Add(key);
                return e;
            }
        }
        return null;
    }

    public LoreSaveData GetSaveData() => new LoreSaveData { entries = new List<LoreEntry>(entries), injectedIds = new List<string>(injectedIds) };

    public void LoadSaveData(LoreSaveData data)
    {
        entries.Clear();
        injectedIds.Clear();
        if (data == null) return;
        if (data.entries != null) entries.AddRange(data.entries);
        if (data.injectedIds != null) foreach (var id in data.injectedIds) injectedIds.Add(id);
    }
}

[System.Serializable]
public class LoreSaveData
{
    public List<LoreEntry> entries = new List<LoreEntry>();
    public List<string> injectedIds = new List<string>();
}
