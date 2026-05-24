using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau de lecture générique pour n'importe quel objet "readable"
/// (note, lettre, livre, parchemin...). Singleton auto-construit.
/// Appelle <see cref="Open(string, string)"/> pour afficher.
/// </summary>
public class ReaderPanel : MonoBehaviour
{
    public static ReaderPanel Instance { get; private set; }

    GameObject root;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;
    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        SetVisible(false);
    }

    void Update()
    {
        if (root != null && root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public static void Show(string title, string body)
    {
        if (Instance == null)
        {
            var go = new GameObject("ReaderPanel");
            go.AddComponent<ReaderPanel>();
        }
        Instance.Open(title, body);
    }

    public void Open(string title, string body)
    {
        titleText.text = title ?? "";
        bodyText.text = body ?? "";
        SetVisible(true);
    }

    public void Close() => SetVisible(false);

    void SetVisible(bool v)
    {
        root.SetActive(v);
        if (v)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void BuildUI()
    {
        var cgo = new GameObject("ReaderCanvas");
        cgo.transform.SetParent(transform);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        cgo.AddComponent<CanvasScaler>();
        cgo.AddComponent<GraphicRaycaster>();

        // Root plein écran : capture les clics derrière le panneau de
        // lecture pour empêcher d'interagir avec l'inventaire en dessous.
        root = new GameObject("ReaderRoot");
        root.transform.SetParent(cgo.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var overlay = root.AddComponent<Image>();
        // Voile semi-transparent : assombrit l'arrière et bloque les clics.
        overlay.color = new Color(0f, 0f, 0f, 0.55f);

        // Panneau central enfant : c'est lui qui porte le contenu du reader.
        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(root.transform, false);
        var prt = panelGo.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(600, 460);
        var bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.05f, 0.96f);
        // À partir d'ici tout est enfant du panneau central.
        var panelT = panelGo.transform;

        // Bordure dorée (simple imagecadre via Outline-like via second Image plus petit)
        // Simplification : on garde juste le fond sombre.

        titleText = MakeText(panelT, "Title", new Vector2(0, 195), new Vector2(560, 50), 26, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
        titleText.fontStyle = FontStyles.Bold;

        bodyText = MakeText(panelT, "Body", new Vector2(0, -20), new Vector2(540, 320), 18, TextAlignmentOptions.TopLeft, new Color(0.95f, 0.92f, 0.85f));
        bodyText.enableWordWrapping = true;

        var closeGo = new GameObject("Close");
        closeGo.transform.SetParent(panelT, false);
        var crt = closeGo.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0, 12);
        crt.sizeDelta = new Vector2(140, 36);
        var cimg = closeGo.AddComponent<Image>();
        cimg.color = new Color(0.25f, 0.2f, 0.12f);
        var btn = closeGo.AddComponent<Button>();
        btn.onClick.AddListener(Close);
        var ct = MakeText(closeGo.transform, "T", Vector2.zero, new Vector2(140, 36), 16, TextAlignmentOptions.Center, Color.white);
        ct.text = "Fermer (ESC)";
    }

    static TextMeshProUGUI MakeText(Transform parent, string n, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        return t;
    }
}
