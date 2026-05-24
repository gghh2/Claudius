using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mini-carte top-down. Crée une caméra orthographique secondaire qui suit
/// le joueur par le dessus, rendue dans une RenderTexture affichée en
/// haut-droite de l'écran. Le Nord est en haut de la map (caméra non tournée).
/// </summary>
public class MiniMap : MonoBehaviour
{
    public static MiniMap Instance { get; private set; }

    [Tooltip("Optionnel — Transform suivi. Si null, le joueur (tag Player) est trouvé.")]
    public Transform target;

    [Tooltip("Taille orthographique de la caméra mini-map (rayon visible en m).")]
    public float orthoSize = 25f;

    [Tooltip("Hauteur au-dessus du target.")]
    public float height = 50f;

    [Tooltip("Layers visibles dans la mini-map (par défaut tout).")]
    public LayerMask renderMask = ~0;

    [Header("HUD")]
    [Tooltip("Taille du widget mini-map en pixels.")]
    public Vector2 hudSize = new Vector2(180f, 180f);
    [Tooltip("Marge depuis le coin haut-droit.")]
    public Vector2 hudOffset = new Vector2(-15f, -15f);

    Camera mapCam;
    RenderTexture rt;
    RawImage hudImage;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        BuildMapCamera();
        BuildHud();
    }

    void Update()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
            else return;
        }

        if (mapCam != null)
        {
            // Suit le joueur en X/Z, fixe en Y (vue dessus).
            mapCam.transform.position = new Vector3(target.position.x, target.position.y + height, target.position.z);
            mapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Nord = +Z = haut de l'image.
        }
    }

    void BuildMapCamera()
    {
        rt = new RenderTexture(256, 256, 16);
        rt.name = "MiniMapRT";

        var camGo = new GameObject("MiniMapCamera");
        camGo.transform.SetParent(transform);
        mapCam = camGo.AddComponent<Camera>();
        mapCam.orthographic = true;
        mapCam.orthographicSize = orthoSize;
        mapCam.cullingMask = renderMask;
        mapCam.clearFlags = CameraClearFlags.SolidColor;
        mapCam.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
        mapCam.targetTexture = rt;
        mapCam.depth = -2; // Rendu avant la main camera.
        mapCam.allowHDR = false;
        mapCam.allowMSAA = false;
    }

    void BuildHud()
    {
        Canvas canvas = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode != RenderMode.WorldSpace && c.isActiveAndEnabled)
            { canvas = c; break; }
        }
        if (canvas == null)
        {
            var cgo = new GameObject("MiniMapCanvas");
            cgo.transform.SetParent(transform);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 91;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        var root = new GameObject("MiniMap");
        root.transform.SetParent(canvas.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = hudOffset;
        rt.sizeDelta = hudSize;

        // Cadre.
        var frame = root.AddComponent<Image>();
        frame.color = new Color(0f, 0f, 0f, 0.6f);

        // Image qui montre la RT.
        var imgGo = new GameObject("Image");
        imgGo.transform.SetParent(root.transform, false);
        var imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.offsetMin = new Vector2(4, 4);
        imgRt.offsetMax = new Vector2(-4, -4);
        hudImage = imgGo.AddComponent<RawImage>();
        hudImage.texture = this.rt;

        // Petit point central qui représente le joueur.
        var dot = new GameObject("PlayerDot");
        dot.transform.SetParent(root.transform, false);
        var dotRt = dot.AddComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0.5f, 0.5f);
        dotRt.anchorMax = new Vector2(0.5f, 0.5f);
        dotRt.pivot = new Vector2(0.5f, 0.5f);
        dotRt.sizeDelta = new Vector2(8, 8);
        var dotImg = dot.AddComponent<Image>();
        dotImg.color = new Color(1f, 0.9f, 0.3f);

        // Indicateur "N" en haut.
        var nGo = new GameObject("N");
        nGo.transform.SetParent(root.transform, false);
        var nRt = nGo.AddComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0.5f, 1f);
        nRt.anchorMax = new Vector2(0.5f, 1f);
        nRt.pivot = new Vector2(0.5f, 1f);
        nRt.anchoredPosition = new Vector2(0, -2);
        nRt.sizeDelta = new Vector2(22, 18);
        var nTxt = nGo.AddComponent<TMPro.TextMeshProUGUI>();
        nTxt.text = "N";
        nTxt.fontSize = 14;
        nTxt.alignment = TMPro.TextAlignmentOptions.Center;
        nTxt.color = new Color(1f, 0.85f, 0.3f);
    }
}
