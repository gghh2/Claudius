using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Console développeur. Touche ² (backquote) pour ouvrir/fermer.
/// Auto-construite (Canvas + InputField + Output) si pas câblée.
/// Tape "help" pour voir les commandes disponibles.
/// </summary>
public class DevConsole : MonoBehaviour
{
    public static DevConsole Instance { get; private set; }

    [Tooltip("Touche principale d'ouverture.")]
    [SerializeField] KeyCode toggleKey = KeyCode.BackQuote;
    [Tooltip("Touche alternative (souvent + fiable que BackQuote selon le clavier).")]
    [SerializeField] KeyCode altToggleKey = KeyCode.F12;
    [SerializeField] int historyLines = 20;

    /// <summary>
    /// Auto-bootstrap : à chaque chargement de scène, on s'assure qu'un
    /// DevConsole existe. Évite à l'utilisateur d'avoir à le placer en scène.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoBootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("DevConsole");
            go.AddComponent<DevConsole>();
            // L'instance se DontDestroyOnLoad elle-même dans Awake.
        }
    }

    GameObject root;
    TMP_InputField input;
    TextMeshProUGUI output;

    public bool IsOpen => root != null && root.activeSelf;
    readonly Queue<string> lines = new Queue<string>();
    readonly Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase);

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
            return;
        }
        RegisterCommands();
    }

    void Start()
    {
        BuildUI();
        SetVisible(false);
        Print("Dev console — tape 'help' pour voir les commandes.");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(altToggleKey))
        {
            // Si on est en train de taper dans un autre InputField (dialogue
            // IA, recherche...), on n'ouvre pas la console. Mais on accepte
            // de FERMER : le focus est alors sur l'input de la console
            // elle-meme, qui est aussi un InputField.
            if (IsOpen || !UIInputUtils.IsTypingInInputField())
                SetVisible(!root.activeSelf);
        }
    }

    float prevTimeScale = 1f;
    bool dialogueInputWasInteractable;

    void SetVisible(bool v)
    {
        root.SetActive(v);
        if (v)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Pause explicite de l'horloge (unscaledDeltaTime ignore timeScale).
            GameClock.Instance?.Pause();

            // Coupe l'input field du DialogueUI pour qu'il arrête de capter
            // les frappes et de re-prendre le focus chaque frame.
            if (DialogueUI.Instance != null && DialogueUI.Instance.playerInputField != null)
            {
                var f = DialogueUI.Instance.playerInputField;
                dialogueInputWasInteractable = f.interactable;
                f.interactable = false;
                f.DeactivateInputField();
            }

            input.text = "";
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
            input.Select();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Time.timeScale = prevTimeScale;
            GameClock.Instance?.Resume();

            if (DialogueUI.Instance != null && DialogueUI.Instance.playerInputField != null)
            {
                DialogueUI.Instance.playerInputField.interactable = dialogueInputWasInteractable;
            }

            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null);
        }
    }

    void BuildUI()
    {
        var cgo = new GameObject("DevConsoleCanvas");
        cgo.transform.SetParent(transform);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        cgo.AddComponent<CanvasScaler>();
        cgo.AddComponent<GraphicRaycaster>();

        root = new GameObject("DevConsoleRoot");
        root.transform.SetParent(cgo.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0.4f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // Output area (top)
        var outGo = new GameObject("Output");
        outGo.transform.SetParent(root.transform, false);
        var outRt = outGo.AddComponent<RectTransform>();
        outRt.anchorMin = new Vector2(0, 0.12f);
        outRt.anchorMax = new Vector2(1, 1f);
        outRt.offsetMin = new Vector2(10, 0);
        outRt.offsetMax = new Vector2(-10, -5);
        output = outGo.AddComponent<TextMeshProUGUI>();
        output.fontSize = 16;
        output.color = new Color(0.9f, 0.95f, 0.85f);
        output.alignment = TextAlignmentOptions.BottomLeft;
        output.text = "";

        // Input field (bottom)
        var inGo = new GameObject("Input");
        inGo.transform.SetParent(root.transform, false);
        var inRt = inGo.AddComponent<RectTransform>();
        inRt.anchorMin = new Vector2(0, 0);
        inRt.anchorMax = new Vector2(1, 0.12f);
        inRt.offsetMin = new Vector2(10, 5);
        inRt.offsetMax = new Vector2(-10, 0);
        var inBg = inGo.AddComponent<Image>();
        inBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        input = inGo.AddComponent<TMP_InputField>();
        input.lineType = TMP_InputField.LineType.SingleLine;

        // Text component for the input field
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(inGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8, 0);
        textRt.offsetMax = new Vector2(-8, 0);
        var textComp = textGo.AddComponent<TextMeshProUGUI>();
        textComp.fontSize = 16;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.MidlineLeft;
        input.textComponent = textComp;
        input.textViewport = textRt;

        input.onSubmit.AddListener(OnSubmit);
    }

    void OnSubmit(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        Print($"> {raw}");
        var parts = raw.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0];
        var args = parts.Skip(1).ToArray();
        if (commands.TryGetValue(cmd, out var fn))
        {
            try { fn(args); }
            catch (Exception e) { Print($"<color=#ff6060>Erreur : {e.Message}</color>"); }
        }
        else
        {
            Print($"<color=#ff6060>Commande inconnue : {cmd}. Tape 'help'.</color>");
        }
        input.text = "";
        input.ActivateInputField();
    }

    public void Print(string msg)
    {
        if (lines.Count >= historyLines) lines.Dequeue();
        lines.Enqueue(msg);
        if (output != null) output.text = string.Join("\n", lines);
    }

    void RegisterCommands()
    {
        commands["help"] = _ => Print(string.Join(", ", commands.Keys.OrderBy(k => k)));
        commands["clear"] = _ => { lines.Clear(); if (output != null) output.text = ""; };

        commands["give"] = a =>
        {
            if (a.Length < 2) { Print("Usage : give credits N | give item NAME [qty]"); return; }
            if (a[0].Equals("credits", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(a[1], out int n)) { Print("Quantité invalide"); return; }
                PlayerWallet.Instance?.AddCredits(n);
                Print($"+{n} crédits (total : {PlayerWallet.Instance?.Credits})");
            }
            else if (a[0].Equals("item", StringComparison.OrdinalIgnoreCase))
            {
                int qty = a.Length >= 3 && int.TryParse(a[2], out int q) ? q : 1;
                if (PlayerInventory.Instance == null) { Print("PlayerInventory absent"); return; }
                PlayerInventory.Instance.AddItem(a[1], qty, "");
                Print($"+{qty}x {a[1]}");
            }
            else Print("Sous-commande inconnue (credits | item)");
        };

        commands["settime"] = a =>
        {
            if (GameClock.Instance == null) { Print("GameClock absent"); return; }
            if (a.Length == 0) { Print($"Heure courante : {GameClock.Instance.FormatNow()}"); return; }
            var hm = a[0].Split(':');
            if (!int.TryParse(hm[0], out int h)) { Print("Heure invalide"); return; }
            int m = hm.Length > 1 && int.TryParse(hm[1], out int mm) ? mm : 0;
            GameClock.Instance.LoadSaveData(new GameClockSaveData { totalMinutes = h * 60 + m });
            Print($"Heure : {GameClock.Instance.FormatNow()}");
        };

        commands["setspeed"] = a =>
        {
            if (GameClock.Instance == null) { Print("GameClock absent"); return; }
            if (a.Length == 0 || !float.TryParse(a[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s))
            { Print("Usage : setspeed N (min/sec réel)"); return; }
            GameClock.Instance.minutesPerRealSecond = s;
            Print($"Vitesse horloge : {s} min/s");
        };

        commands["teleport"] = a =>
        {
            if (a.Length == 0) { Print("Usage : teleport ZONE_NAME"); return; }
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) { Print("Player introuvable"); return; }
            string query = string.Join(" ", a).ToLowerInvariant();
            var zones = UnityEngine.Object.FindObjectsByType<QuestZone>(FindObjectsSortMode.None);
            var match = zones.FirstOrDefault(z => z.zoneName.ToLowerInvariant().Contains(query));
            if (match == null) { Print($"Zone '{query}' non trouvée"); return; }
            var cc = go.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            go.transform.position = match.transform.position + Vector3.up * 2f;
            if (cc != null) cc.enabled = true;
            Print($"Téléporté à : {match.zoneName}");
        };

        commands["quests"] = _ =>
        {
            if (QuestJournal.Instance == null) { Print("QuestJournal absent"); return; }
            foreach (var q in QuestJournal.Instance.GetActiveQuests())
                Print($"  [active] {q.questId.Substring(0, Mathf.Min(6, q.questId.Length))} — {q.questTitle} ({q.GetProgressText()})");
        };

        commands["npcs"] = _ =>
        {
            var sb = new StringBuilder();
            foreach (var n in UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None))
                sb.Append($"{n.npcName} ({n.npcRole}); ");
            Print(sb.Length == 0 ? "(aucun)" : sb.ToString());
        };

        commands["god"] = _ =>
        {
            var p = UnityEngine.Object.FindFirstObjectByType<PlayerControllerCC>();
            if (p == null) { Print("Player absent"); return; }
            p.currentStamina = 9999f;
            Print("God mode (stamina ∞)");
        };

        commands["spawnnpc"] = a =>
        {
            var spawner = ProceduralNPCSpawner.Instance;
            if (spawner == null) { Print("ProceduralNPCSpawner absent (ajoute le composant en scène)"); return; }
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) { Print("Player absent"); return; }
            // Spawn 3m devant le joueur.
            Vector3 pos = player.transform.position + player.transform.forward * 3f;
            string role = a.Length > 0 ? string.Join(" ", a) : null;
            var go = spawner.SpawnAt(pos, role);
            if (go != null) Print($"PNJ procédural spawné : {go.GetComponent<NPC>()?.npcName}");
        };

        commands["addrumor"] = a =>
        {
            if (a.Length == 0) { Print("Usage : addrumor TEXTE"); return; }
            if (RumorPool.Instance == null) { Print("RumorPool absent"); return; }
            string text = string.Join(" ", a);
            RumorPool.Instance.AddRumor($"dev_{System.Guid.NewGuid().ToString().Substring(0, 6)}", text);
            Print("Rumeur ajoutée");
        };

        commands["addnote"] = a =>
        {
            if (a.Length < 2) { Print("Usage : addnote TITRE TEXTE..."); return; }
            if (LoreLibrary.Instance == null)
            {
                var go = new GameObject("LoreLibrary");
                go.AddComponent<LoreLibrary>();
            }
            string title = a[0];
            string content = string.Join(" ", a, 1, a.Length - 1);
            LoreLibrary.Instance.RegisterNote($"dev_{System.Guid.NewGuid().ToString().Substring(0, 6)}", title, content);
            Print($"Note ajoutée : {title}");
        };
    }
}
