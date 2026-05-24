using UnityEngine;

/// <summary>
/// Barème de récompenses des quêtes. Le jeu fixe les montants — l'IA reste
/// vague et ne cite jamais de chiffre. À ajuster ici uniquement.
/// </summary>
public static class QuestRewardScale
{
    public const int FETCH_BASE = 30;
    public const int FETCH_PER_ITEM = 15;
    public const int DELIVERY = 75;
    public const int EXPLORE = 60;
    public const int TALK = 40;
    public const int INTERACT = 50;
    public const int ESCORT = 100;

    /// <summary>
    /// Calcule la récompense d'une quête terminée. Retourne un montant
    /// positif ; 0 si le type n'est pas reconnu.
    /// </summary>
    public static int GetReward(QuestType type, int quantity)
    {
        int q = Mathf.Max(1, quantity);
        switch (type)
        {
            case QuestType.FETCH:    return FETCH_BASE + FETCH_PER_ITEM * q;
            case QuestType.DELIVERY: return DELIVERY;
            case QuestType.EXPLORE:  return EXPLORE;
            case QuestType.TALK:     return TALK;
            case QuestType.INTERACT: return INTERACT;
            case QuestType.ESCORT:   return ESCORT;
            default:                 return 0;
        }
    }
}
