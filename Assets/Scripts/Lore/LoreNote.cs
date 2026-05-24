using UnityEngine;
using TMPro;

/// <summary>
/// Indice écrit trouvable dans le monde (parchemin, gravure, holotape...).
/// À attacher à un GameObject avec un Collider trigger.
/// Touche E à proximité ramasse la note : ajoutée à l'inventaire (objet
/// lisible avec contenu), enregistrée dans la <see cref="LoreLibrary"/>
/// pour l'injection IA, et journalisée.
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
    GameObject promptObj;
    TextMeshPro promptText;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInRange = true;
            ShowPrompt(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInRange = false;
            ShowPrompt(false);
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(pickupKey))
        {
            Pickup();
        }
        // Billboard du prompt vers la caméra.
        if (promptObj != null && promptObj.activeSelf && Camera.main != null)
        {
            promptObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }

    void ShowPrompt(bool show)
    {
        if (show && promptObj == null)
        {
            promptObj = new GameObject("Prompt");
            promptObj.transform.SetParent(transform);
            promptObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            promptText = promptObj.AddComponent<TextMeshPro>();
            promptText.fontSize = 3;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = new Color(1f, 0.92f, 0.55f);
            promptText.text = $"📜 {title}\n[E] Ramasser";
        }
        if (promptObj != null) promptObj.SetActive(show);
    }

    void Pickup()
    {
        if (LoreLibrary.Instance == null)
        {
            var go = new GameObject("LoreLibrary");
            go.AddComponent<LoreLibrary>();
        }
        LoreLibrary.Instance.RegisterNote(noteId, title, content);

        // L'objet va dans l'inventaire — il reste lisible via le panel Reader.
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddItem(title, 1, "", content);
        }

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowSuccess($"Note trouvée : {title}");

        if (AdventureJournalUI.Instance != null)
            AdventureJournalUI.Instance.LogGameEvent($"J'ai trouvé une note : « {title} ». {content}");

        Destroy(gameObject);
    }
}
