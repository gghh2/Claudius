using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Horloge du temps in-game. Avance à <see cref="minutesPerRealSecond"/>
/// minutes virtuelles par seconde réelle (défaut 1 = 1s réelle → 1min jeu).
/// HUD auto-généré si aucune cible TMP n'est câblée. Save/load via
/// <see cref="GameClockSaveData"/>.
/// </summary>
public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Combien de minutes in-game s'écoulent par seconde réelle.")]
    public float minutesPerRealSecond = 1f;

    [Tooltip("Heure de démarrage (h, min) si pas de save.")]
    public int startHour = 8;
    public int startMinute = 0;

    [Header("HUD")]
    [Tooltip("Optionnel : si null, un HUD est généré automatiquement en haut-droite.")]
    public TextMeshProUGUI hudText;

    // Format d'affichage de l'heure — choix du joueur, persistant entre sessions.
    public enum TimeFormat { Hours24, Hours12 }
    const string PREF_KEY = "GameClock.TimeFormat";

    public static TimeFormat CurrentFormat
    {
        get => (TimeFormat)PlayerPrefs.GetInt(PREF_KEY, (int)TimeFormat.Hours24);
        set
        {
            PlayerPrefs.SetInt(PREF_KEY, (int)value);
            PlayerPrefs.Save();
            if (Instance != null) Instance.UpdateHud();
        }
    }

    public static bool Use12HourFormat => CurrentFormat == TimeFormat.Hours12;

    // Minutes totales écoulées depuis le démarrage (Jour 1 00:00).
    [SerializeField] private float totalMinutes;
    public float TotalMinutes => totalMinutes;
    public int Day => 1 + Mathf.FloorToInt(totalMinutes / (24 * 60));
    public int Hour => Mathf.FloorToInt((totalMinutes % (24 * 60)) / 60);
    public int Minute => Mathf.FloorToInt(totalMinutes % 60);

    public event Action<int /*Day*/, int /*Hour*/, int /*Minute*/> OnMinuteChanged;

    int lastMinute = -1;
    GameObject autoHudRoot;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            totalMinutes = startHour * 60 + startMinute;

            // Si on est dans une hiérarchie UI (Canvas ancêtre), on N'OSE PAS
            // détacher : les enfants UI (comme un TMP HUD qu'on porte) perdraient
            // leur Canvas et cesseraient de s'afficher. On reste donc en place
            // et on saute DontDestroyOnLoad — la clock vit le temps de la scène
            // Game (comme l'AdventureJournalUI).
            if (HasCanvasAncestor())
            {
                return;
            }

            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    bool HasCanvasAncestor()
    {
        Transform t = transform.parent;
        while (t != null)
        {
            if (t.GetComponent<Canvas>() != null) return true;
            t = t.parent;
        }
        return false;
    }

    void Start()
    {
        if (hudText == null) BuildAutoHud();
        UpdateHud();
    }

    void Update()
    {
        if (minutesPerRealSecond <= 0f) return;
        totalMinutes += Time.deltaTime * minutesPerRealSecond;

        if (Minute != lastMinute)
        {
            lastMinute = Minute;
            UpdateHud();
            OnMinuteChanged?.Invoke(Day, Hour, Minute);
        }
    }

    void UpdateHud()
    {
        if (hudText != null)
            hudText.text = $"Jour {Day} — {FormatHourMinute()}";
    }

    /// <summary>
    /// Formate l'heure courante selon le format choisi par le joueur (12h/24h).
    /// </summary>
    public string FormatHourMinute()
    {
        if (Use12HourFormat)
        {
            int h12 = Hour % 12;
            if (h12 == 0) h12 = 12;
            string suffix = Hour < 12 ? "AM" : "PM";
            return $"{h12}:{Minute:00} {suffix}";
        }
        return $"{Hour:00}:{Minute:00}";
    }

    void BuildAutoHud()
    {
        // Trouve un Canvas existant ; sinon en crée un dédié.
        Canvas canvas = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode != RenderMode.WorldSpace && c.isActiveAndEnabled)
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null)
        {
            var cgo = new GameObject("GameClockCanvas");
            cgo.transform.SetParent(transform);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        autoHudRoot = new GameObject("GameClockHUD");
        autoHudRoot.transform.SetParent(canvas.transform, false);
        var rt = autoHudRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(220f, 40f);

        hudText = autoHudRoot.AddComponent<TextMeshProUGUI>();
        hudText.fontSize = 22;
        hudText.alignment = TextAlignmentOptions.MidlineRight;
        hudText.color = new Color(1f, 0.95f, 0.85f);
    }

    public string FormatNow()
    {
        return $"Jour {Day}, {FormatHourMinute()}";
    }

    /// <summary>Étiquette grossière du moment de la journée pour l'IA.</summary>
    public string TimeOfDayLabel()
    {
        if (Hour < 6)  return "nuit";
        if (Hour < 9)  return "aube";
        if (Hour < 12) return "matin";
        if (Hour < 14) return "midi";
        if (Hour < 18) return "après-midi";
        if (Hour < 21) return "soir";
        return "nuit";
    }

    public GameClockSaveData GetSaveData() => new GameClockSaveData { totalMinutes = totalMinutes };

    public void LoadSaveData(GameClockSaveData data)
    {
        if (data == null) return;
        totalMinutes = Mathf.Max(0f, data.totalMinutes);
        lastMinute = -1;
        UpdateHud();
    }
}

[System.Serializable]
public class GameClockSaveData
{
    public float totalMinutes;
}
