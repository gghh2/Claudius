using UnityEngine;

/// <summary>
/// Configuration centralisée pour le système de quêtes
/// </summary>
public static class QuestSystemConfig
{
    // Couleurs UI
    public static readonly Color TrackedQuestBackgroundColor = new Color(1f, 1f, 0f, 0.3f); // Jaune semi-transparent
    public static readonly Color NormalQuestBackgroundColor = new Color(0f, 0f, 0f, 0.1f); // Presque transparent
    public static readonly Color TrackedButtonColor = Color.yellow;
    public static readonly Color UntrackedButtonColor = Color.gray;
    
    // Tailles par défaut
    public const float DefaultMarkerSize = 50f;
    public const float MarkerPulseSpeed = 2f;
    public const float MarkerPulseAmount = 0.1f;
    
    // Distances
    public const float MarkerHideDistance = 10f;
    public const float MarkerEdgeOffset = 50f;
    
    // Tags
    public const string PlayerTag = "Player";
    public const string NPCTag = "NPC";
}
