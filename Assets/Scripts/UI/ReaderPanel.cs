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

        root = new GameObject("ReaderRoot");
        root.transform.SetParent(cgo.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600, 460);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.05f, 0.96f);

        // Bordure dorée (simple imagecadre via Outline-like via second Image plus petit)
        // Simplification : on garde juste le fond sombre.

        titleText = MakeText(root.transform, "Title", new Vector2(0, 195), new Vector2(560, 50), 26, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
        titleText.fontStyle = FontStyles.Bold;

        bodyText = MakeText(root.transform, "Body", new Vector2(0, -20), new Vector2(540, 320), 18, TextAlignmentOptions.TopLeft, new Color(0.95f, 0.92f, 0.85f));
        bodyText.enableWordWrapping = true;

        var closeGo = new GameObject("Close");
        closeGo.transform.SetParent(root.transform, false);
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
