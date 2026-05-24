using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Génère procéduralement des PNJ à partir d'un prefab template. Tire un nom,
/// un rôle, une couleur, et configure le composant NPC à la volée.
/// Utilisation : appeler <see cref="SpawnAt(Vector3)"/> ou via la console
/// dev (commande 'spawnnpc').
/// </summary>
public class ProceduralNPCSpawner : MonoBehaviour
{
    public static ProceduralNPCSpawner Instance { get; private set; }

    [Tooltip("Prefab utilisé comme base. Doit avoir un composant NPC et NPCNameDisplay.")]
    public GameObject npcPrefab;

    [Header("Pools de génération")]
    public string[] firstNames =
    {
        "Velka", "Orin", "Sevra", "Talan", "Kira", "Mox", "Brunn", "Astrid",
        "Renn", "Calix", "Ymir", "Selene", "Drex", "Liora", "Onyx", "Phaeris",
        "Sylas", "Mira", "Eros", "Nyx", "Cassian", "Vex", "Kael", "Soren"
    };
    public string[] lastNames =
    {
        "le Vagabond", "des Étoiles", "le Patient", "aux Mille Voyages", "la Discrète",
        "le Cartographe", "des Brumes", "le Silencieux", "des Ruines", "l'Oublié",
        "le Cendreux", "des Cieux Bas", "le Cordier", "des Sables"
    };
    public string[] roles =
    {
        "Marchand", "Scientifique", "Garde Impérial", "Chasseur", "Cartographe",
        "Vagabond", "Erudit", "Mécanicien", "Diplomate", "Pilote retraité"
    };

    [Header("Apparence")]
    public Color[] possibleColors =
    {
        new Color(1f, 0.85f, 0.4f),  // doré
        new Color(0.5f, 0.8f, 1f),   // bleu glacier
        new Color(0.9f, 0.5f, 0.5f), // rouille
        new Color(0.6f, 1f, 0.6f),   // vert pâle
        new Color(0.9f, 0.7f, 1f),   // lavande
        new Color(1f, 1f, 1f),       // blanc
        new Color(0.7f, 0.6f, 0.4f), // beige
    };
    [Tooltip("Variation aléatoire d'échelle (±). 0.1 = ±10%.")]
    [Range(0f, 0.3f)] public float scaleVariance = 0.08f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public GameObject SpawnAt(Vector3 position) => SpawnAt(position, null);

    public GameObject SpawnAt(Vector3 position, string forcedRole)
    {
        if (npcPrefab == null)
        {
            Debug.LogError("[ProceduralNPC] npcPrefab non assigné — assigne un prefab NPC dans l'Inspector.");
            return null;
        }

        var go = Instantiate(npcPrefab, position, Quaternion.identity);
        var npc = go.GetComponent<NPC>();
        if (npc == null)
        {
            Debug.LogWarning("[ProceduralNPC] Le prefab n'a pas de composant NPC.");
            return go;
        }

        npc.npcName = $"{Pick(firstNames)} {Pick(lastNames)}";
        npc.npcRole = forcedRole ?? Pick(roles);
        npc.npcColor = Pick(possibleColors);

        // Léger jitter d'échelle pour donner de la variété visuelle.
        float s = 1f + Random.Range(-scaleVariance, scaleVariance);
        go.transform.localScale = new Vector3(s, s, s);

        var disp = go.GetComponent<NPCNameDisplay>();
        if (disp != null) disp.SetDisplayName(npc.npcName);

        return go;
    }

    static T Pick<T>(IList<T> arr) => arr[Random.Range(0, arr.Count)];
}
