using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Content area for inventory items")]
    public Transform inventoryContent;

    [Tooltip("Prefab for inventory item display")]
    public GameObject inventoryItemPrefab;

    [Tooltip("Close button")]
    public Button closeButton;

    [Tooltip("Optionnel : texte TMP qui affiche le solde de crédits du joueur. " +
        "Si null, une ligne sera générée en tête de l'inventaire à chaque refresh.")]
    public TextMeshProUGUI creditsText;
    
    [Header("Settings")]
    [Tooltip("Key to open/close inventory")]
    public KeyCode inventoryKey = KeyCode.I;
    
    private bool isOpen = false;
    
    public static InventoryUI Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // NE PAS désactiver gameObject car cela désactive tout l'UI !
        // UnifiedUIManager gère la visibilité des panels

        // Configure le bouton de fermeture
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }

        // S'abonne au wallet pour rafraîchir le solde quand il change même
        // pendant que l'inventaire est ouvert.
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnCreditsChanged += OnCreditsChanged;
            UpdateCreditsText(PlayerWallet.Instance.Credits);
        }

        // Refresh live quand l'inventaire change (ex. note ramassée pendant
        // que le panel est ouvert) — évite le "temps de retard" d'attendre
        // une réouverture pour voir les nouveaux objets.
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnItemsChanged += OnInventoryChanged;
        }
    }

    void OnInventoryChanged()
    {
        if (isOpen) RefreshInventoryDisplay();
    }

    void OnDestroy()
    {
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnCreditsChanged -= OnCreditsChanged;
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnItemsChanged -= OnInventoryChanged;
    }

    // Note : on ne peut pas se fier à OnEnable du script. Sur ce projet le
    // composant InventoryUI vit sur un GO toujours actif (l'UI racine),
    // tandis que le panel inventaire (un enfant) s'active/désactive. OnEnable
    // ne firerait qu'une fois au scene-load. À la place, on poll dans Update
    // si le panel parent vient de devenir actif.
    bool wasContentActive;

    void Update()
    {
        if (inventoryContent == null) return;
        bool isActive = inventoryContent.gameObject.activeInHierarchy;
        if (isActive && !wasContentActive)
        {
            isOpen = true;
            RefreshInventoryDisplay();
        }
        else if (!isActive && wasContentActive)
        {
            isOpen = false;
        }
        wasContentActive = isActive;
    }

    void OnCreditsChanged(int newCredits)
    {
        UpdateCreditsText(newCredits);
    }

    void UpdateCreditsText(int amount)
    {
        if (creditsText != null)
            creditsText.text = $"Crédits : {amount}";

        // Si l'inventaire est ouvert et qu'on génère la ligne dynamiquement
        // (creditsText null), on régénère pour refléter le solde courant.
        if (creditsText == null && isOpen)
            RefreshInventoryDisplay();
    }

    void CreateCreditsHeader(int amount)
    {
        GameObject header = new GameObject("CreditsHeader");
        header.transform.SetParent(inventoryContent);
        header.transform.SetSiblingIndex(0);

        Image bg = header.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.12f, 0.05f, 0.85f);

        RectTransform rect = header.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 50);
        rect.localScale = Vector3.one;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(header.transform);

        TextMeshProUGUI t = textObj.AddComponent<TextMeshProUGUI>();
        t.text = $"Crédits : {amount}";
        t.fontSize = 24;
        t.color = new Color(1f, 0.85f, 0.3f);
        t.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;
        tr.anchoredPosition = new Vector2(10, 0);
    }
    
    public void ToggleInventory()
    {
        isOpen = !isOpen;
        
        if (isOpen)
        {
            OpenInventory();
        }
        else
        {
            CloseInventory();
        }
    }
    
    void OpenInventory()
    {
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateTo(UnifiedUIPanelNames.Inventory);
            isOpen = true;
        }
        else
        {
            gameObject.SetActive(true);
            isOpen = true;
        }
        
        // Player control is handled by UnifiedUIManager
        
        // Rafraîchit l'affichage
        RefreshInventoryDisplay();
        
        Debug.Log("📦 Inventaire ouvert");
    }
    
    public void CloseInventory()
    {
        if (UnifiedUIManager.Instance != null)
        {
            UnifiedUIManager.Instance.NavigateBack();
            isOpen = false;
        }
        else
        {
            gameObject.SetActive(false);
            isOpen = false;
        }
        
        // Player control is handled by UnifiedUIManager
        
        Debug.Log("📦 Inventaire fermé");
    }
    
    void RefreshInventoryDisplay()
    {
        // Souscription tardive au wallet (cas où PlayerWallet.Awake a tourné
        // APRÈS InventoryUI.Start). Sans ça, l'event OnCreditsChanged n'est
        // pas écouté et le texte reste figé sur 0 même après AddCredits.
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnCreditsChanged -= OnCreditsChanged;
            PlayerWallet.Instance.OnCreditsChanged += OnCreditsChanged;
            UpdateCreditsText(PlayerWallet.Instance.Credits);
        }

        // Nettoie l'affichage actuel
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }

        // Ligne crédits en tête (auto-générée si pas de creditsText câblé dans
        // l'Inspector). On garde un fallback pour ne rien demander à l'utilisateur.
        if (creditsText == null && PlayerWallet.Instance != null)
        {
            CreateCreditsHeader(PlayerWallet.Instance.Credits);
        }

        // Récupère l'inventaire du joueur
        if (PlayerInventory.Instance != null)
        {
            var items = PlayerInventory.Instance.items;
            
            if (items.Count == 0)
            {
                // Affiche un message si l'inventaire est vide
                GameObject emptyMessage = new GameObject("EmptyMessage");
                emptyMessage.transform.SetParent(inventoryContent);
                
                TextMeshProUGUI emptyText = emptyMessage.AddComponent<TextMeshProUGUI>();
                emptyText.text = "Inventaire vide";
                emptyText.fontSize = 24;
                emptyText.color = Color.gray;
                emptyText.alignment = TextAlignmentOptions.Center;
                
                RectTransform rect = emptyMessage.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(300, 50);
            }
            else
            {
                // Affiche chaque item
                foreach (var item in items)
                {
                    CreateItemDisplay(item);
                }
            }
        }
    }
    
    void CreateItemDisplay(InventoryItem item)
    {
        GameObject itemDisplay;
        
        // Utilise le prefab si disponible, sinon crée un affichage simple
        if (inventoryItemPrefab != null)
        {
            itemDisplay = Instantiate(inventoryItemPrefab, inventoryContent);
        }
        else
        {
            // Création manuelle d'un affichage simple
            itemDisplay = new GameObject($"Item_{item.itemName}");
            itemDisplay.transform.SetParent(inventoryContent);
            
            // Ajoute un background
            Image bg = itemDisplay.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Configure le RectTransform
            RectTransform rect = itemDisplay.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);
            
            // Ajoute le texte
            GameObject textObj = new GameObject("ItemText");
            textObj.transform.SetParent(itemDisplay.transform);
            
            TextMeshProUGUI itemText = textObj.AddComponent<TextMeshProUGUI>();
            // NOUVEAU: Formate le nom de l'item
            string formattedName = TextFormatter.FormatName(item.itemName);
            itemText.text = $"{item.quantity}x {formattedName}";
            
            // Si c'est un item de quête, ajoute une indication
            if (!string.IsNullOrEmpty(item.questId))
            {
                itemText.text += " (Quête)";
                itemText.color = Color.yellow;
            }
            else
            {
                itemText.color = Color.white;
            }
            
            itemText.fontSize = 20;
            itemText.alignment = TextAlignmentOptions.MidlineLeft;

            // Configure le RectTransform du texte
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = new Vector2(10, 0); // Padding gauche
        }

        // Bouton 'Lire' pour les items lisibles (notes, lettres, livres...).
        // Hors du else : marche aussi si l'utilisateur a un inventoryItemPrefab.
        if (!string.IsNullOrEmpty(item.readableContent))
        {
            AddReadButton(itemDisplay, item);
        }
    }

    void AddReadButton(GameObject parent, InventoryItem item)
    {
        var btnGo = new GameObject("ReadButton");
        btnGo.transform.SetParent(parent.transform, false);
        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-10, 0);
        rt.sizeDelta = new Vector2(90, 36);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.25f, 0.2f, 0.35f, 0.95f);

        var btn = btnGo.AddComponent<Button>();
        string title = TextFormatter.FormatName(item.itemName);
        string body = item.readableContent;
        btn.onClick.AddListener(() => ReaderPanel.Show(title, body));

        var txtGo = new GameObject("T");
        txtGo.transform.SetParent(btnGo.transform, false);
        var trt = txtGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        var t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = "Lire";
        t.fontSize = 16;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
    }
    
    // Méthode publique pour vérifier si l'inventaire est ouvert
    public bool IsInventoryOpen()
    {
        return isOpen;
    }
}
