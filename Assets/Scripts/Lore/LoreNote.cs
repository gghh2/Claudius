using UnityEngine;

/// <summary>
/// Indice écrit trouvable dans le monde (parchemin, gravure, holotape...).
/// À attacher à un GameObject avec un Collider trigger.
/// Touche E à proximité ramasse la note : son contenu va dans la
/// <see cref="LoreLibrary"/> (devient accessible aux PNJ via injection IA) et
/// une entrée est ajoutée au Journal d'Aventure.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LoreNote : MonoBehaviour
{
    [Tooltip("Identifiant unique de la note (snake_case). Utilisé pour la persistance.")]
    public string noteId;
    [Tooltip("Titre court affiché au joueur.")]
    public string title = "Note";
    [Tooltip("Contenu : court paragraphe injecté dans la mémoire des PNJ et lisible dans le journal.")]
    [TextArea(3, 6)]
    public string content;
    [Tooltip("Touche pour ramasser quand le joueur est dans le trigger.")]
    public KeyCode pickupKey = KeyCode.E;

    bool playerInRange;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag("Player")) playerInRange = false;
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(pickupKey))
        {
            Pickup();
        }
    }

    void Pickup()
    {
        if (LoreLibrary.Instance == null)
        {
            // Auto-instancie la library s'il manque (devient persistante seule).
            var go = new GameObject("LoreLibrary");
            go.AddComponent<LoreLibrary>();
        }
        LoreLibrary.Instance.RegisterNote(noteId, title, content);

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowSuccess($"Note trouvée : {title}");

        if (AdventureJournalUI.Instance != null)
            AdventureJournalUI.Instance.LogGameEvent($"J'ai trouvé une note : « {title} ». {content}");

        Destroy(gameObject);
    }
}
