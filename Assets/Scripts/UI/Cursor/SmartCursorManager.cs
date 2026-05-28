using UnityEngine;

/// <summary>
/// Contrôleur de curseur — responsabilité unique : maintenir l'état du curseur
/// cohérent avec l'état de l'UI, à travers TOUTES les scènes.
///
/// Règle : un panel UI est ouvert → curseur visible et libre ;
/// sinon (en jeu) → curseur caché et verrouillé.
/// Scène sans UnifiedUIManager (ex. MainMenu) → on n'est pas en jeu :
/// curseur visible et libre.
///
/// Singleton DontDestroyOnLoad auto-bootstrapped (RuntimeInitializeOnLoadMethod)
/// pour eviter le bug "plus de curseur en MainMenu" qui survenait quand
/// l'instance scene-local etait detruite et qu'aucune autre prenait la
/// releve dans la scene cible.
///
/// Source de vérité : <see cref="UnifiedUIManager.IsShowingPanel"/>.
/// AUCUNE autre classe ne doit toucher Cursor.visible / Cursor.lockState.
/// </summary>
public class SmartCursorManager : MonoBehaviour
{
    private static SmartCursorManager Instance;

    private UnifiedUIManager uiManager;
    private float refindUiManagerTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoBootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("SmartCursorManager");
            go.AddComponent<SmartCursorManager>();
            // L'instance se DontDestroyOnLoad elle-meme dans Awake.
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        uiManager = FindFirstObjectByType<UnifiedUIManager>();
        Apply();
    }

    void Update()
    {
        // L'UnifiedUIManager peut changer entre les scenes : on rafraichit
        // la reference periodiquement (pas chaque frame, FindFirst...
        // coute un balayage).
        refindUiManagerTimer -= Time.unscaledDeltaTime;
        if (uiManager == null || refindUiManagerTimer <= 0f)
        {
            uiManager = FindFirstObjectByType<UnifiedUIManager>();
            refindUiManagerTimer = 0.5f;
        }

        Apply();
    }

    void Apply()
    {
        // Hors de la scene "Game", on n'est pas en jeu : curseur libre. Test
        // par nom de scene actif car UnifiedUIManager est DontDestroyOnLoad
        // et persiste depuis Game vers MainMenu — son existence seule ne
        // suffit pas a determiner si on est en jeu.
        bool inGameScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game";

        bool cursorFree = !inGameScene
            || uiManager == null
            || uiManager.IsShowingPanel;

        // Cas spécial : UIs hors UnifiedUIManager (boutique, panneau de lecture,
        // console dev) doivent aussi libérer le curseur. On consulte leurs
        // singletons directement.
        if (!cursorFree && IsExternalUiActive()) cursorFree = true;

        Cursor.visible = cursorFree;
        Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
    }

    static bool IsExternalUiActive()
    {
        if (ShopUI.Instance != null && ShopUI.Instance.IsOpen) return true;
        if (ReaderPanel.Instance != null && ReaderPanel.Instance.IsOpen) return true;
        if (DevConsole.Instance != null && DevConsole.Instance.IsOpen) return true;
        return false;
    }

    void OnDisable()
    {
        // Composant désactivé ou sortie du Play mode : on libère le curseur
        // pour qu'il ne reste pas caché/verrouillé dans l'éditeur.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
