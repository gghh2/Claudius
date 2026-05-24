using UnityEngine;

/// <summary>
/// Génère un compte-rendu d'exploration court à la complétion d'une quête
/// EXPLORE. Sert à donner au joueur quelque chose à rapporter au PNJ
/// donneur de la quête : injecté dans la mémoire de conversation IA pour
/// que le PNJ puisse en parler quand le joueur revient.
/// </summary>
public static class ExplorationReport
{
    static readonly string[] Observations =
    {
        "des traces fraîches d'un passage récent",
        "une structure abandonnée à demi enfouie",
        "des inscriptions étrangères sur les parois",
        "des fragments métalliques épars",
        "un silence inhabituel et persistant",
        "une lueur résiduelle dans l'air",
        "des spores aux teintes inconnues",
        "un courant d'air venant d'une cavité fermée",
        "des marques au sol évoquant un campement temporaire",
        "une vibration sourde sous les pieds",
        "des restes organiques mal identifiables",
        "un alignement de pierres qui n'a rien d'accidentel"
    };

    /// <summary>
    /// Construit un rapport court de 1-2 observations pour la zone donnée.
    /// </summary>
    public static string Generate(string zoneName)
    {
        string formattedZone = TextFormatter.FormatName(zoneName);
        int a = Random.Range(0, Observations.Length);
        int b;
        do { b = Random.Range(0, Observations.Length); } while (b == a);

        return $"Vous avez exploré « {formattedZone} ». Vous y avez noté : " +
            $"{Observations[a]} ; {Observations[b]}.";
    }
}
