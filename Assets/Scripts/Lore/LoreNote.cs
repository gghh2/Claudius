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
    [Tooltip("Titre court affiché au joueur. Ignoré si useRandomContent.")]
    public string title = "Note";
    [Tooltip("Contenu : court paragraphe injecté dans la mémoire des PNJ et lisible dans le journal. Ignoré si useRandomContent.")]
    [TextArea(3, 6)]
    public string content;
    [Tooltip("Touche pour ramasser quand le joueur est dans le trigger.")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Aléatoire")]
    [Tooltip("Si vrai, le titre et le contenu sont tirés du LoreContentLibrary " +
        "au lieu d'utiliser ceux serialises. Chaque entrée n'est tirée qu'une " +
        "fois par session.")]
    public bool useRandomContent = true;
    [Tooltip("Si vrai, applique une rotation Y aléatoire à la note au démarrage " +
        "pour casser l'alignement régulier.")]
    public bool randomizeYRotation = true;

    bool playerInRange;
    GameObject promptObj;
    TextMeshPro promptText;

    void Start()
    {
        // Si la note est déjà connue (ramassée + persistée via save), on se
        // supprime à l'init pour éviter le respawn au scene-reload.
        if (LoreLibrary.Instance != null
            && !string.IsNullOrEmpty(noteId)
            && LoreLibrary.Instance.HasNote(noteId))
        {
            Destroy(gameObject);
            return;
        }

        // Contenu aléatoire : remplace titre + contenu par une entrée
        // piochée dans le catalogue commun (non répétante par session).
        if (useRandomContent)
        {
            var entry = LoreContentLibrary.PickRandom();
            title = entry.title;
            content = entry.content;
            // Génère un id stable basé sur le titre pour la persistance.
            if (string.IsNullOrEmpty(noteId))
                noteId = "note_" + title.GetHashCode().ToString("X");
        }

        // Rotation Y aléatoire — casse l'alignement régulier des notes posées.
        if (randomizeYRotation)
        {
            var e = transform.eulerAngles;
            transform.eulerAngles = new Vector3(e.x, Random.Range(0f, 360f), e.z);
        }
    }

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
        // Suivi en monde + billboard caméra (comme NPCNameDisplay). On ne
        // parente PAS le prompt à la note : la note est un cube scale non
        // uniforme, l'héritage écraserait le texte.
        if (promptObj != null && promptObj.activeSelf)
        {
            promptObj.transform.position = transform.position + Vector3.up * 1.2f;
            if (Camera.main != null)
                promptObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }

    void ShowPrompt(bool show)
    {
        if (show && promptObj == null)
        {
            promptObj = new GameObject($"LoreNotePrompt_{noteId}");
            // Pas de SetParent — évite l'héritage du scale déformé du cube.
            promptObj.transform.position = transform.position + Vector3.up * 1.2f;
            promptObj.transform.localScale = Vector3.one;

            promptText = promptObj.AddComponent<TextMeshPro>();
            promptText.fontSize = 3;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = new Color(1f, 0.92f, 0.55f);
            promptText.fontStyle = FontStyles.Normal;
            promptText.outlineWidth = 0f;
            promptText.text = $"{title}\n[E] Ramasser";
        }
        if (promptObj != null) promptObj.SetActive(show);
    }

    void OnDestroy()
    {
        // Le prompt n'étant pas parenté, on doit le nettoyer manuellement
        // quand la note est ramassée / détruite.
        if (promptObj != null) Destroy(promptObj);
    }

    void Pickup()
    {
        Debug.Log($"[LoreNote] Pickup '{title}' (id={noteId}) — content {content?.Length ?? 0} chars");

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
            Debug.Log($"[LoreNote] AddItem OK — inventaire contient {PlayerInventory.Instance.items.Count} items");
        }
        else
        {
            Debug.LogError("[LoreNote] PlayerInventory.Instance est NULL — la note n'a pas pu être ajoutée à l'inventaire !");
        }

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowSuccess($"Note trouvée : {title}");

        if (AdventureJournalUI.Instance != null)
            AdventureJournalUI.Instance.LogGameEvent($"J'ai trouvé une note : « {title} ». {content}");

        Destroy(gameObject);
    }
}
