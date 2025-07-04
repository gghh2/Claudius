using UnityEngine;

/// <summary>
/// Simple helper to trigger quest list refresh when panel becomes active
/// </summary>
public class QuestJournalPanelHelper : MonoBehaviour
{
    void OnEnable()
    {
        // When this panel becomes active, refresh the quest list
        if (QuestJournalUI.Instance != null)
        {
            // Ensure we're on the InProgress tab by default
            QuestJournalUI.Instance.SwitchTab(QuestStatus.InProgress);
        }
    }
}
