using UnityEngine;

/// <summary>
/// Configuration centralisée pour le système de quêtes
/// </summary>
public static class QuestSystemConfig
{
    // === COULEURS UI ===
    public static readonly Color TrackedQuestBackgroundColor = new Color(1f, 1f, 0f, 0.3f); // Jaune semi-transparent
    public static readonly Color NormalQuestBackgroundColor = new Color(0f, 0f, 0f, 0.1f); // Presque transparent
    public static readonly Color TrackedButtonColor = Color.yellow;
    public static readonly Color UntrackedButtonColor = Color.gray;
    
    // === MARQUEURS UI ===
    public const float DefaultMarkerSize = 50f;
    public const float MarkerPulseSpeed = 2f;
    public const float MarkerPulseAmount = 0.1f;
    public const float MarkerHideDistance = 10f;
    public const float MarkerEdgeOffset = 50f;
    
    // === TAGS ===
    public const string PlayerTag = "Player";
    public const string NPCTag = "NPC";
    
    // === QUÊTES ===
    public const int DefaultMaxActiveQuests = 5;
    public const float DefaultExplorationTime = 2f;
    public const float DefaultTriggerRadius = 3f;
    
    // === SONS - VOLUMES PAR DÉFAUT ===
    public const float DefaultQuestStartVolume = 0.5f;
    public const float DefaultQuestCompleteVolume = 0.5f;
    public const float DefaultQuestItemCollectVolume = 0.3f;
    public const float DefaultQuestCancelVolume = 0.4f;
    
    // === ANIMATIONS ===
    public const float QuestObjectPulseSpeed = 2f;
    public const float QuestObjectPulseIntensity = 0.5f;
    public const float MarkerDestroyDelay = 2f;
    public const float MarkerFadeTime = 1f;
    
    // === TEXTE UI ===
    public const float DefaultFontSize = 3f;
    public const float InteractionFontSizeMultiplier = 1.2f;
    public const float ExplorationFontSizeMultiplier = 1.3f;
    public const float CompletionFontSizeMultiplier = 1.5f;
    
    // === MESSAGES DE DEBUG ===
    public const string QuestCreatedMessage = "[QUEST] Quête créée avec succès: {0}";
    public const string QuestProgressMessage = "[QUEST] Progression quête {0}: {1}/{2}";
    public const string QuestCompletedMessage = "[QUEST] OBJECTIFS ACCOMPLIS ! Retournez voir {0} pour rendre la quête.";
    public const string QuestCleanedMessage = "[QUEST] Quête nettoyée: {0}";
    
    // === RÔLES NPC PAR DÉFAUT ===
    public const string DeliveryNPCRole = "Destinataire";
    public const string TalkNPCRole = "Informateur";
    public const string InteractNPCRole = "Terminal";
}
