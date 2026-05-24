using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalogue de notes/indices écrits pré-rédigés. Quand une LoreNote a
/// useRandomContent = true, elle pioche dans ce catalogue au Start au lieu
/// d'utiliser le texte sérialisé. Les entrées sont non-répétantes par
/// session (chacune n'est utilisée qu'une fois).
/// </summary>
public static class LoreContentLibrary
{
    public struct Entry
    {
        public string title;
        public string content;
        public Entry(string t, string c) { title = t; content = c; }
    }

    static readonly List<Entry> Catalog = new List<Entry>
    {
        new Entry("Inscription érodée",
            "« Avant que les soleils se croisent, descends de trois mesures et écoute la pierre. Ce que tu trouveras n'a pas de nom. »"),
        new Entry("Page arrachée d'un carnet",
            "Jour 47. La voix est revenue. Elle ne parle plus comme avant. Elle compte. Toujours les mêmes chiffres : 3, 7, 12."),
        new Entry("Reçu froissé",
            "Reçu pour cristaux d'énergie (×4). Prix convenu. Le solde sera réglé en lune montante. — V."),
        new Entry("Ordre de mission",
            "Section 9 : vérifier l'intégrité du verrou ancien dans la zone interdite. Si présence, ne pas approcher. Rapporter par voie indirecte."),
        new Entry("Lettre inachevée",
            "Mère, je ne reviendrai pas. Ce que nous avons trouvé ici dépasse les mots. Si tu lis ceci, c'est qu'on m'a remplacé. Ne crois pas mon visage."),
        new Entry("Page d'herbier",
            "Spore-de-cendre. Floraison nocturne. Toxique au contact prolongé. Croît uniquement où la terre a brûlé. Ne pas confondre avec spore-de-suie (inoffensive)."),
        new Entry("Note médicale",
            "Patient n°12 — somnambulisme aigu. Marche en direction des ruines toutes les nuits, sans souvenir. Symptôme corrélé avec les nouvelles arrivées."),
        new Entry("Schéma au charbon",
            "Un cercle entouré de huit traits. Sous le cercle, une flèche pointe vers le bas. À côté, en petit : « ne pas ouvrir avant la troisième aube »."),
        new Entry("Liste de noms",
            "Korvyn — disparu, jour 12. Sevra — partie au labyrinthe, jamais revenue, jour 19. Ymir — refuse de parler depuis le retour, jour 28."),
        new Entry("Carnet de chasse",
            "La bête laisse trois empreintes en triangle, jamais quatre. Elle évite les sentiers ouverts. À l'aube elle s'approche des sources."),
        new Entry("Prière improvisée",
            "Esprits du sol, gardiens des os, accordez à ce voyageur le passage. Il ne prend que ce qu'il faut. Il ne réveille rien."),
        new Entry("Bordereau d'envoi",
            "Colis scellé, destinataire absent. Conservation indéfinie dans la chambre froide n°3. Ne pas inventorier. Ne pas commenter."),
        new Entry("Fragment de chant",
            "« ...et nous marchions sans nos noms / sur la pierre qui se souvient / les soleils baissaient leurs lances / pour nous laisser passer. »"),
        new Entry("Avertissement gravé",
            "ARRÊTE. Le terrain devant toi s'enfonce. Quatre l'ont déjà appris à leurs dépens. Reviens sur tes pas, vivant."),
        new Entry("Note d'un cartographe",
            "Toutes les cartes de cette région sont fausses. Le marais marqué au nord n'existe plus depuis trois saisons. Ne te fie qu'à tes yeux."),
    };

    // Tracking pour éviter de tirer deux fois le même par session.
    static HashSet<int> usedIndices = new HashSet<int>();

    /// <summary>
    /// Pioche une entrée non encore utilisée. Si toutes ont été tirées,
    /// réinitialise le pool.
    /// </summary>
    public static Entry PickRandom()
    {
        if (usedIndices.Count >= Catalog.Count) usedIndices.Clear();
        int idx;
        int safety = 64;
        do { idx = Random.Range(0, Catalog.Count); } while (usedIndices.Contains(idx) && --safety > 0);
        usedIndices.Add(idx);
        return Catalog[idx];
    }
}
