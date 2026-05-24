using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boussole HUD : affiche une flèche directionnelle vers la quête trackée
/// + la distance restante. Auto-construite si rien n'est câblé.
/// </summary>
public class QuestCompass : MonoBehaviour
{
    public static QuestCompass Instance { get; private set; }

    [Tooltip("Optionnel — laisser null pour génération auto.")]
    public TextMeshProUGUI compassText;

    [Tooltip("Camera de référence pour calculer 'vers où regarde le joueur'.")]
    public Camera referenceCamera;

    Transform player;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (compassText == null) BuildAutoHud();
        if (referenceCamera == null) referenceCamera = Camera.main;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (compassText == null) return;
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }
        if (referenceCamera == null) referenceCamera = Camera.main;
        if (referenceCamera == null) return;

        Vector3? target = GetTrackedQuestPosition();
        if (target == null)
        {
            compassText.text = "";
            return;
        }

        Vector3 to = target.Value - player.position;
        to.y = 0;
        float distance = to.magnitude;

        // Direction relative à l'orientation horizontale de la caméra.
        Vector3 camFwd = referenceCamera.transform.forward;
        camFwd.y = 0;
        camFwd.Normalize();
        float signed = Vector3.SignedAngle(camFwd, to.normalized, Vector3.up);

        string arrow = ArrowFor(signed);
        compassText.text = $"{arrow}  Quête  —  {distance:0}m";
    }

    static string ArrowFor(float signedAngleDeg)
    {
        // Mappe l'angle [-180, 180] sur 8 directions.
        // 0 = devant, 90 = droite, -90 = gauche, ±180 = derrière.
        float a = signedAngleDeg;
        if (a > -22.5f && a <= 22.5f)   return "↑";
        if (a > 22.5f && a <= 67.5f)    return "↗";
        if (a > 67.5f && a <= 112.5f)   return "→";
        if (a > 112.5f && a <= 157.5f)  return "↘";
        if (a > 157.5f || a <= -157.5f) return "↓";
        if (a > -157.5f && a <= -112.5f) return "↙";
        if (a > -112.5f && a <= -67.5f)  return "←";
        return "↖";
    }

    Vector3? GetTrackedQuestPosition()
    {
        if (QuestJournal.Instance == null) return null;
        var tracked = QuestJournal.Instance.GetTrackedQuest();
        if (tracked == null) return null;
        if (QuestManager.Instance == null) return null;
        var active = QuestManager.Instance.GetActiveQuestPublic(tracked.questId);
        if (active == null) return null;

        // Priorité 1 : un objet spawné (le plus précis).
        if (active.spawnedObjects != null)
        {
            foreach (var obj in active.spawnedObjects)
                if (obj != null) return obj.transform.position;
        }
        if (active.reusedObjects != null)
        {
            foreach (var obj in active.reusedObjects)
                if (obj != null) return obj.transform.position;
        }

        // Priorité 2 : la zone cible (plus large).
        var zone = active.GetTargetZone();
        if (zone != null) return zone.transform.position;
        return null;
    }

    void BuildAutoHud()
    {
        Canvas canvas = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode != RenderMode.WorldSpace && c.isActiveAndEnabled) { canvas = c; break; }
        }
        if (canvas == null)
        {
            var cgo = new GameObject("QuestCompassCanvas");
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
        rt.anchoredPosition = new Vector2(0, -20f);
        rt.sizeDelta = new Vector2(320f, 44f);

        compassText = go.AddComponent<TextMeshProUGUI>();
        compassText.fontSize = 22;
        compassText.alignment = TextAlignmentOptions.Center;
        compassText.color = new Color(1f, 0.95f, 0.85f);
    }
}
