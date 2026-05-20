using UnityEngine;

/// <summary>
/// Petit relais posé sur le panneau AdventureJournal.
/// Le contrôleur <see cref="AdventureJournalUI"/> vit sur le GameObject "UI"
/// (toujours actif) : il ne reçoit donc pas OnEnable quand le panneau s'affiche.
/// Ce helper le prévient pour qu'il rafraîchisse l'affichage.
/// </summary>
public class AdventureJournalPanelHelper : MonoBehaviour
{
    void OnEnable()
    {
        if (AdventureJournalUI.Instance != null)
            AdventureJournalUI.Instance.OnPanelShown();
    }
}
