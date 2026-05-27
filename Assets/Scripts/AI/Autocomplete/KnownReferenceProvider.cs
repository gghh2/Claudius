using System.Collections.Generic;

/// <summary>
/// Agrege toutes les "references familieres au joueur" — items possedes (ou
/// l'ayant ete), zones decouvertes, PNJ rencontres, notes lues — pour
/// alimenter l'autocompletion du champ de dialogue. Les noms sont retournes
/// au format snake_case interne ; l'autocompletion les formate via
/// TextFormatter.FormatName avant insertion.
/// </summary>
public static class KnownReferenceProvider
{
    public static IEnumerable<string> GetAllReferenceNames()
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        // 1) Items actuellement portes.
        if (PlayerInventory.Instance != null)
        {
            foreach (var it in PlayerInventory.Instance.items)
                if (it != null && !string.IsNullOrEmpty(it.itemName) && seen.Add(it.itemName))
                    yield return it.itemName;

            // 2) Items deja possedes a un moment (livres / utilises).
            foreach (var n in PlayerInventory.Instance.EverPossessedItemNames)
                if (!string.IsNullOrEmpty(n) && seen.Add(n))
                    yield return n;
        }

        // 3) Zones decouvertes.
        if (QuestZoneManager.Instance != null)
        {
            foreach (var z in QuestZoneManager.Instance.GetDiscoveredZoneNames())
                if (!string.IsNullOrEmpty(z) && seen.Add(z))
                    yield return z;
        }

        // 4) PNJ deja rencontres.
        if (AIDialogueManager.Instance != null)
        {
            foreach (var n in AIDialogueManager.Instance.GetSpokenNpcNames())
                if (!string.IsNullOrEmpty(n) && seen.Add(n))
                    yield return n;
        }

        // 5) Notes lues (LoreLibrary garde titre + contenu).
        if (LoreLibrary.Instance != null)
        {
            foreach (var note in LoreLibrary.Instance.Entries)
                if (note != null && !string.IsNullOrEmpty(note.title) && seen.Add(note.title))
                    yield return note.title;
        }
    }

    /// <summary>
    /// Trouve la meilleure reference qui commence par le prefix donne
    /// (case-insensitive). Retourne null si aucune. Le matching se fait sur
    /// le nom formate (TextFormatter.FormatName) ET sur le nom interne, pour
    /// que "lett" matche aussi bien "Lettre inachevee" que "lettre_inachevee".
    /// </summary>
    public static string FindMatch(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return null;
        string lo = prefix.ToLowerInvariant();

        string best = null;
        int bestScore = int.MaxValue;

        foreach (var raw in GetAllReferenceNames())
        {
            string formatted = TextFormatter.FormatName(raw);
            string rawLo = raw.ToLowerInvariant();
            string fmtLo = formatted.ToLowerInvariant();

            bool match = rawLo.StartsWith(lo) || fmtLo.StartsWith(lo);
            if (!match) continue;

            // Score = longueur du nom (on prefere les noms courts a prefix
            // egal, plus probables d'etre ce que le joueur tape).
            int score = formatted.Length;
            if (score < bestScore)
            {
                bestScore = score;
                best = formatted;
            }
        }

        return best;
    }
}
