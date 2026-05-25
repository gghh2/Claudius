using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mémoire commune des PNJ sur le monde : nom de la planète, lieux notables,
/// etc. Le premier PNJ qui parle au joueur invente un nom de planète ; tous
/// les PNJ suivants se servent du même. Persisté via save/load.
/// </summary>
public class WorldLore : MonoBehaviour
{
    public static WorldLore Instance { get; private set; }

    // NB : pas de [SerializeField] sur ces champs — sinon une valeur fixée à
    // un Play précédent pourrait fuiter dans le fichier de scène (et l'éditeur
    // recharger 'Khael Tor' à chaque nouvelle partie). On veut explicitement
    // que les données ne survivent QUE via le save/load.
    string planetName = "";
    public string PlanetName => planetName;
    public bool HasPlanetName => !string.IsNullOrWhiteSpace(planetName);

    List<string> namedLocations = new List<string>();
    public IReadOnlyList<string> NamedLocations => namedLocations;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Reset explicite. Au cas où 'Enter Play Mode > Reload Domain' soit
        // désactivé dans Project Settings : les statiques ne se reset pas
        // automatiquement, on force ici.
        planetName = "";
        namedLocations.Clear();
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Réinitialise explicitement (appelé lors d'un 'Nouvelle Partie' depuis
    /// le menu pour repartir d'un monde vierge même si la singleton avait
    /// survécu à une partie précédente).
    /// </summary>
    public void ClearForNewGame()
    {
        planetName = "";
        namedLocations.Clear();
        Debug.Log("[WorldLore] Reset (Nouvelle Partie).");
    }

    /// <summary>Fixe le nom de la planète. No-op si déjà défini.</summary>
    public void SetPlanetName(string name)
    {
        if (HasPlanetName) return;
        if (string.IsNullOrWhiteSpace(name)) return;
        planetName = name.Trim();
        Debug.Log($"[WorldLore] Planète nommée : {planetName}");
    }

    public void AddNamedLocation(string loc)
    {
        if (string.IsNullOrWhiteSpace(loc)) return;
        if (!namedLocations.Contains(loc)) namedLocations.Add(loc);
    }

    public WorldLoreSaveData GetSaveData() => new WorldLoreSaveData
    {
        planetName = planetName,
        namedLocations = new List<string>(namedLocations)
    };

    public void LoadSaveData(WorldLoreSaveData data)
    {
        if (data == null) return;
        planetName = data.planetName ?? "";
        namedLocations.Clear();
        if (data.namedLocations != null) namedLocations.AddRange(data.namedLocations);
    }
}

[System.Serializable]
public class WorldLoreSaveData
{
    public string planetName;
    public List<string> namedLocations = new List<string>();
}
