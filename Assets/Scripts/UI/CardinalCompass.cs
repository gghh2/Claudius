using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Compas cardinal : bandeau en haut d'écran montrant N/E/S/W qui défile
/// selon l'orientation du joueur (ou de la caméra par défaut). Le centre du
/// bandeau indique la direction dans laquelle on regarde.
/// </summary>
public class CardinalCompass : MonoBehaviour
{
    public static CardinalCompass Instance { get; private set; }

    [Tooltip("Source d'orientation. Si null, la Camera.main est utilisée " +
        "(adapté pour les caméras qui suivent le joueur).")]
    public Transform headingSource;

    [Tooltip("Optionnel — laisser null pour HUD auto-construit.")]
    public TextMeshProUGUI compassText;

    [Tooltip("Largeur angulaire affichée (degrés). 120 = on voit -60° à +60°.")]
    public float visibleAngle = 120f;
    [Tooltip("Pas entre deux graduations en degrés (45 = N, NE, E, SE, S, SW, W, NW).")]
    public int tickStep = 45;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (compassText == null) BuildAutoHud();
        if (headingSource == null && Camera.main != null)
            headingSource = Camera.main.transform;
    }

    void Update()
    {
        if (compassText == null) return;
        if (headingSource == null && Camera.main != null) headingSource = Camera.main.transform;
        if (headingSource == null) return;

        Vector3 fwd = headingSource.forward;
        fwd.y = 0;
        if (fwd.sqrMagnitude < 0.001f) return;
        // Angle de la direction regardée par rapport au Nord (+Z) [-180, 180].
        float heading = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        compassText.text = BuildStrip(heading);
    }

    string BuildStrip(float heading)
    {
        var sb = new System.Text.StringBuilder();
        float halfVisible = visibleAngle * 0.5f;

        for (int deg = 0; deg < 360; deg += tickStep)
        {
            float rel = Mathf.DeltaAngle(heading, deg); // [-180, 180]
            if (Mathf.Abs(rel) > halfVisible) continue;

            string label = CardinalLabel(deg);

            // Highlight central (à la position que le joueur regarde).
            bool central = Mathf.Abs(rel) < tickStep * 0.5f;
            if (central)
                sb.Append($"  <color=#ffe06a><size=120%>[{label}]</size></color>  ");
            else
                sb.Append($"  <color=#cccccc>{label}</color>  ");
        }
        return sb.ToString();
    }

    static string CardinalLabel(int deg)
    {
        switch (deg)
        {
            case 0:   return "N";
            case 45:  return "NE";
            case 90:  return "E";
            case 135: return "SE";
            case 180: return "S";
            case 225: return "SO";
            case 270: return "O";
            case 315: return "NO";
            default:  return $"{deg}°";
        }
    }

    void BuildAutoHud()
    {
        Canvas canvas = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode != RenderMode.WorldSpace && c.isActiveAndEnabled)
            { canvas = c; break; }
        }
        if (canvas == null)
        {
            var cgo = new GameObject("CardinalCompassCanvas");
            cgo.transform.SetParent(transform);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        var go = new GameObject("CompassHUD");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -10f);
        rt.sizeDelta = new Vector2(500f, 36f);

        compassText = go.AddComponent<TextMeshProUGUI>();
        compassText.fontSize = 20;
        compassText.alignment = TextAlignmentOptions.Center;
        compassText.color = Color.white;
        compassText.richText = true;
    }
}
