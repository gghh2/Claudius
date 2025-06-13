using UnityEngine;

public static class TextFormatter
{
    /// <summary>
    /// Formate un nom en remplaçant les underscores par des espaces et en appliquant une capitalisation appropriée
    /// </summary>
    public static string FormatName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return rawName;
        
        // Remplace les underscores par des espaces
        string formatted = rawName.Replace('_', ' ');
        
        // Applique une capitalisation "Title Case" (première lettre de chaque mot en majuscule)
        formatted = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted.ToLower());
        
        // Gère les cas spéciaux (optionnel)
        formatted = HandleSpecialCases(formatted);
        
        return formatted;
    }
    
    /// <summary>
    /// Formate spécifiquement pour les descriptions (première lettre en majuscule seulement)
    /// </summary>
    public static string FormatDescription(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
            return rawText;
        
        // Remplace les underscores par des espaces
        string formatted = rawText.Replace('_', ' ');
        
        // Première lettre en majuscule, le reste en minuscule
        if (formatted.Length > 0)
        {
            formatted = char.ToUpper(formatted[0]) + formatted.Substring(1).ToLower();
        }
        
        return formatted;
    }
    
    /// <summary>
    /// Gère les cas spéciaux de formatage
    /// </summary>
    private static string HandleSpecialCases(string text)
    {
        // Exemples de cas spéciaux que vous pourriez vouloir gérer
        text = text.Replace(" Ia ", " IA "); // IA au lieu de Ia
        text = text.Replace(" Ai ", " AI "); // AI au lieu de Ai
        text = text.Replace(" Npc ", " NPC "); // NPC au lieu de Npc
        text = text.Replace(" Ui ", " UI "); // UI au lieu de Ui
        
        // Ajoutez d'autres cas selon vos besoins
        
        return text;
    }
    
    /// <summary>
    /// Vérifie si un texte contient des underscores
    /// </summary>
    public static bool HasUnderscores(string text)
    {
        return !string.IsNullOrEmpty(text) && text.Contains("_");
    }
}
