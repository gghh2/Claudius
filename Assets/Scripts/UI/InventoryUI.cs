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
        // Cache le panel au démarrage
        gameObject.SetActive(false);
        
        // Configure le bouton de fermeture
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }
    }
    
    void Update()
    {
        // NOUVEAU : Gère la touche Escape pour fermer l'inventaire
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
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
        gameObject.SetActive(true);
        isOpen = true;
        
        // Notify UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPanelState(UIPanelNames.Inventory, true);
        }
        
        // Désactive le mouvement du joueur
        PlayerControllerCC player = FindObjectOfType<PlayerControllerCC>();
        if (player != null)
        {
            player.enabled = false;
        }
        
        // Rafraîchit l'affichage
        RefreshInventoryDisplay();
        
        Debug.Log("📦 Inventaire ouvert");
    }
    
    public void CloseInventory()
    {
        gameObject.SetActive(false);
        isOpen = false;
        
        // Notify UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPanelState(UIPanelNames.Inventory, false);
        }
        
        // Réactive le mouvement du joueur
        PlayerControllerCC player = FindObjectOfType<PlayerControllerCC>();
        if (player != null)
        {
            player.enabled = true;
        }
        
        Debug.Log("📦 Inventaire fermé");
    }
    
    void RefreshInventoryDisplay()
    {
        // Nettoie l'affichage actuel
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
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
    }
    
    // Méthode publique pour vérifier si l'inventaire est ouvert
    public bool IsInventoryOpen()
    {
        return isOpen;
    }
}
