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

    [Header("Beam lumineux")]
    [Tooltip("Pilier de lumière au-dessus de la note pour la repérer de loin.")]
    public bool showBeam = true;
    [Tooltip("Couleur du beam.")]
    public Color beamColor = new Color(1f, 0.85f, 0.4f, 0.85f);
    [Tooltip("Hauteur du beam (m).")]
    public float beamHeight = 6f;
    [Tooltip("Rayon du beam (m).")]
    public float beamRadius = 0.15f;

    bool playerInRange;
    GameObject promptObj;
    TextMeshPro promptText;
    GameObject beamObj;

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

        // Beam lumineux pour identifier la note de loin.
        if (showBeam) CreateBeam();
    }

    void CreateBeam()
    {
        // Cylindre fin, non parente (pareil que le prompt — evite l'heritage
        // du scale deforme du cube de la note). Materiau émissif dore.
        beamObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beamObj.name = $"LoreNoteBeam_{noteId}";
        // Supprime le collider du primitive (pas besoin d'interaction physique).
        var col = beamObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        beamObj.transform.position = transform.position + Vector3.up * (beamHeight * 0.5f + 0.1f);
        beamObj.transform.localScale = new Vector3(beamRadius * 2f, beamHeight * 0.5f, beamRadius * 2f);

        // Matériau émissif (essaie URP Lit, fallback Standard).
        var renderer = beamObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                // URP : _BaseColor + _EmissionColor ; Standard : _Color + _EmissionColor.
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", beamColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", beamColor);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", new Color(beamColor.r, beamColor.g, beamColor.b) * 3f);
                    mat.EnableKeyword("_EMISSION");
                }
                // URP : transparence via _Surface (1 = transparent).
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 0f); // Alpha
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // additif léger
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
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
        // Prompt + beam non parentés, à détruire manuellement.
        if (promptObj != null) Destroy(promptObj);
        if (beamObj != null) Destroy(beamObj);
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
