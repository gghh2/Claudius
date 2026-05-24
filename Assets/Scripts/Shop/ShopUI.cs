using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI auto-construite pour une boutique. Singleton. OpenFor(Shop) affiche
/// le catalogue ; chaque entrée a un bouton "Acheter" qui débite le wallet
/// et ajoute l'objet à l'inventaire.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    GameObject root;
    Transform itemsContainer;
    TextMeshProUGUI titleText;
    TextMeshProUGUI creditsText;
    Shop currentShop;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnCreditsChanged -= OnWalletChanged;
    }

    void OnEnable()
    {
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnCreditsChanged += OnWalletChanged;
    }

    void Update()
    {
        if (root != null && root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    void BuildUI()
    {
        var cgo = new GameObject("ShopCanvas");
        cgo.transform.SetParent(transform);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        cgo.AddComponent<CanvasScaler>();
        cgo.AddComponent<GraphicRaycaster>();

        root = new GameObject("ShopPanel");
        root.transform.SetParent(cgo.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600, 520);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.05f, 0.95f);

        titleText = CreateText(root.transform, "Title", new Vector2(0, 230), new Vector2(560, 50), 28, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
        creditsText = CreateText(root.transform, "Credits", new Vector2(0, 195), new Vector2(560, 30), 18, TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.9f));

        // Scroll view (manuel — sans ScrollRect pour rester simple)
        var listGo = new GameObject("Items");
        listGo.transform.SetParent(root.transform, false);
        var listRt = listGo.AddComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0, 0);
        listRt.anchorMax = new Vector2(1, 1);
        listRt.offsetMin = new Vector2(20, 60);
        listRt.offsetMax = new Vector2(-20, -90);
        var layout = listGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        itemsContainer = listGo.transform;

        var closeGo = new GameObject("Close");
        closeGo.transform.SetParent(root.transform, false);
        var closeRt = closeGo.AddComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0);
        closeRt.anchorMax = new Vector2(0.5f, 0);
        closeRt.pivot = new Vector2(0.5f, 0);
        closeRt.anchoredPosition = new Vector2(0, 10);
        closeRt.sizeDelta = new Vector2(150, 40);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.3f, 0.2f, 0.15f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(Close);
        var closeTxt = CreateText(closeGo.transform, "T", Vector2.zero, new Vector2(150, 40), 18, TextAlignmentOptions.Center, Color.white);
        closeTxt.text = "Fermer (ESC)";
    }

    static TextMeshProUGUI CreateText(Transform parent, string n, Vector2 anchored, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchored;
        rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        return t;
    }

    public void OpenFor(Shop shop)
    {
        currentShop = shop;
        titleText.text = shop.GetShopName();
        UpdateCredits();
        Rebuild();
        SetVisible(true);
        // Bloque les contrôles du joueur pendant la boutique.
        FindFirstObjectByType<PlayerControllerCC>()?.EnableControls(false);
    }

    public void Close()
    {
        SetVisible(false);
        FindFirstObjectByType<PlayerControllerCC>()?.EnableControls(true);
    }

    void SetVisible(bool v)
    {
        root.SetActive(v);
        if (v)
        {
            // Curseur visible géré par SmartCursorManager (UnifiedUIManager n'est
            // pas dans le flow ici, on doit forcer la visibilité manuellement).
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void Rebuild()
    {
        foreach (Transform c in itemsContainer) Destroy(c.gameObject);
        if (currentShop == null) return;
        foreach (var item in currentShop.catalog)
        {
            BuildRow(item);
        }
    }

    void BuildRow(ShopItem item)
    {
        var row = new GameObject($"Row_{item.itemName}");
        row.transform.SetParent(itemsContainer, false);
        var rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 50);
        var bg = row.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.13f, 0.10f, 0.9f);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 50;

        var nameTxt = CreateText(row.transform, "Name", new Vector2(-130, 0), new Vector2(280, 50), 18, TextAlignmentOptions.MidlineLeft, Color.white);
        nameTxt.text = $"{TextFormatter.FormatName(item.itemName)}" + (string.IsNullOrEmpty(item.description) ? "" : $"\n<size=12><color=#bbbbbb>{item.description}</color></size>");
        nameTxt.rectTransform.anchoredPosition = new Vector2(150, 0);
        nameTxt.alignment = TextAlignmentOptions.MidlineLeft;

        var priceTxt = CreateText(row.transform, "Price", Vector2.zero, new Vector2(120, 50), 18, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
        priceTxt.text = $"{item.price} ¢";
        priceTxt.rectTransform.anchoredPosition = new Vector2(170, 0);

        var btnGo = new GameObject("Buy");
        btnGo.transform.SetParent(row.transform, false);
        var brt = btnGo.AddComponent<RectTransform>();
        brt.sizeDelta = new Vector2(100, 36);
        brt.anchoredPosition = new Vector2(240, 0);
        var bimg = btnGo.AddComponent<Image>();
        bimg.color = new Color(0.25f, 0.4f, 0.2f);
        var bbtn = btnGo.AddComponent<Button>();
        var btxt = CreateText(btnGo.transform, "T", Vector2.zero, new Vector2(100, 36), 16, TextAlignmentOptions.Center, Color.white);
        btxt.text = "Acheter";
        bbtn.onClick.AddListener(() => Buy(item));
    }

    void Buy(ShopItem item)
    {
        if (PlayerWallet.Instance == null || PlayerInventory.Instance == null) return;
        if (!PlayerWallet.Instance.SpendCredits(item.price))
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowWarning("Crédits insuffisants");
            return;
        }
        PlayerInventory.Instance.AddItem(item.itemName, 1, "");
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowSuccess($"Achat : {TextFormatter.FormatName(item.itemName)}");
        UpdateCredits();
    }

    void UpdateCredits()
    {
        if (creditsText != null && PlayerWallet.Instance != null)
            creditsText.text = $"Vos crédits : {PlayerWallet.Instance.Credits}";
    }

    void OnWalletChanged(int credits) => UpdateCredits();
}
