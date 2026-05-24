using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawne au démarrage un ensemble de notes/indices écrits prédéfinis,
/// ancrés à des QuestZones existantes (le placement final est calé sur le
/// sol par raycast vertical pour gérer les terrains accidentés).
///
/// Pratique pour peupler le monde sans avoir à placer chaque note à la main
/// dans la scène : si un nouveau monde est généré, les notes restent
/// cohérentes avec les zones.
/// </summary>
public class LoreNoteSpawner : MonoBehaviour
{
    [System.Serializable]
    public class NoteSeed
    {
        public string id;
        public string title;
        [TextArea(2, 5)] public string content;
        [Tooltip("Nom partiel d'une QuestZone qui sert d'ancre. La note sera " +
            "placée à un offset aléatoire dans le rayon de spawn de la zone.")]
        public string zoneNameMatch;
    }

    [Tooltip("Visuel optionnel : un prefab utilisé pour matérialiser la note " +
        "dans le monde. Si null, on génère un petit cube doré.")]
    public GameObject visualPrefab;

    [Tooltip("Rayon de jitter autour du centre de zone (mètres).")]
    public float spawnRadius = 4f;

    public NoteSeed[] seeds =
    {
        new NoteSeed
        {
            id = "inscription_temple",
            title = "Inscription gravée",
            content = "« Ce que tu cherches n'est pas dans la pierre, mais sous elle. Trois pas vers le soleil levant, puis creuse. »",
            zoneNameMatch = "Temple"
        },
        new NoteSeed
        {
            id = "priere_oratoire",
            title = "Prière oubliée",
            content = "« Quand les deux soleils se taisent, l'Oratoire entend encore. Frappe trois fois sur la dalle centrale et écoute. »",
            zoneNameMatch = "Oratoire"
        },
        new NoteSeed
        {
            id = "recu_marchand",
            title = "Reçu d'un marchand",
            content = "Cristaux d'énergie (×3), payés rubis. Acheteur : « celui qui marche masqué le mardi ». Signé : K.",
            zoneNameMatch = "Marché"
        },
        new NoteSeed
        {
            id = "note_medicale",
            title = "Note médicale",
            content = "Patient n°7 : crises nocturnes, parle d'une voix qui vient du sol. À surveiller. Transfert si récidive.",
            zoneNameMatch = "Medical"
        },
        new NoteSeed
        {
            id = "carte_griboullie",
            title = "Carte griboullée",
            content = "Un schéma maladroit pointe une intersection dans le labyrinthe — un X surmonté du mot « ICI ». L'encre est récente.",
            zoneNameMatch = "labyrinthe"
        },
    };

    void Start()
    {
        var zones = FindObjectsByType<QuestZone>(FindObjectsSortMode.None);
        foreach (var seed in seeds)
        {
            // Évite de re-spawner si la note est déjà trouvée (save/load).
            if (LoreLibrary.Instance != null && LoreLibrary.Instance.HasNote(seed.id))
                continue;

            QuestZone anchor = null;
            string q = (seed.zoneNameMatch ?? "").ToLowerInvariant();
            foreach (var z in zones)
            {
                if (z.zoneName.ToLowerInvariant().Contains(q))
                { anchor = z; break; }
            }
            if (anchor == null) continue;

            // Jitter horizontal + raycast vertical pour caler au sol.
            Vector2 rnd = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = anchor.transform.position + new Vector3(rnd.x, 50f, rnd.y);
            Vector3 finalPos = anchor.transform.position;
            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 200f,
                ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.isTrigger) finalPos = hit.point + Vector3.up * 0.4f;
            }

            GameObject visual;
            if (visualPrefab != null)
            {
                visual = Instantiate(visualPrefab, finalPos, Quaternion.identity);
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.position = finalPos;
                visual.transform.localScale = new Vector3(0.3f, 0.05f, 0.4f);
                var r = visual.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.9f, 0.78f, 0.35f);
                // Le primitive Cube a déjà un BoxCollider non-trigger ; on ajoute
                // un second collider trigger plus large pour la détection joueur.
                var trig = visual.AddComponent<SphereCollider>();
                trig.radius = 1.5f;
                trig.isTrigger = true;
            }
            visual.name = $"LoreNote_{seed.id}";

            var note = visual.AddComponent<LoreNote>();
            note.noteId = seed.id;
            note.title = seed.title;
            note.content = seed.content;
        }
    }
}
