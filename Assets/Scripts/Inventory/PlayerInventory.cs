using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public int quantity;
    public string questId;
    [TextArea(2, 6)]
    [Tooltip("Si non vide, l'item est 'lisible' (note, lettre, livre) — bouton Lire " +
        "apparaît dans l'inventaire et ouvre un panneau de lecture.")]
    public string readableContent;

    public InventoryItem(string name, int qty, string quest = "")
    {
        itemName = name;
        quantity = qty;
        questId = quest;
    }
}

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    /// <summary>
    /// Émis dès que la liste des items change (Add / Remove / clear).
    /// Permet à l'UI de refresh live au lieu d'attendre une réouverture.
    /// </summary>
    public event System.Action OnItemsChanged;

    [Header("Inventory")]
    public List<InventoryItem> items = new List<InventoryItem>();

    /// <summary>
    /// Noms de tous les items que le joueur a possede a un moment donne dans
    /// cette session (incluant ceux deja consommes / livres). Lu par
    /// l'autocompletion de dialogue pour proposer les references familieres
    /// au joueur meme apres qu'il s'en soit defait.
    /// </summary>
    public HashSet<string> EverPossessedItemNames { get; } = new HashSet<string>();
    
    // Debug est maintenant géré par GlobalDebugManager
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
                Debug.Log("✅ PlayerInventory Instance créée");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void AddItem(string itemName, int quantity = 1, string questId = "")
    {
        AddItem(itemName, quantity, questId, null);
    }

    public void AddItem(string itemName, int quantity, string questId, string readableContent)
    {
        InventoryItem existingItem = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);

        if (existingItem != null)
        {
            existingItem.quantity += quantity;
            // Si l'objet existant n'avait pas de contenu lisible et que celui qu'on
            // ajoute en a un, on enrichit (utile pour les notes ramassées dont on
            // n'aurait pas fixé le content du premier coup).
            if (string.IsNullOrEmpty(existingItem.readableContent) && !string.IsNullOrEmpty(readableContent))
                existingItem.readableContent = readableContent;
        }
        else
        {
            var item = new InventoryItem(itemName, quantity, questId);
            item.readableContent = readableContent;
            items.Add(item);
        }

        // Historique de possession (utilise par l'autocompletion de dialogue).
        if (!string.IsNullOrEmpty(itemName)) EverPossessedItemNames.Add(itemName);

        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
        {
            Debug.Log($"📦 INVENTAIRE: Ajouté {quantity}x {itemName} (Quête: {questId})");
        }

        OnItemsChanged?.Invoke();
    }
    
    public bool RemoveItem(string itemName, int quantity = 1, string questId = "")
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);
        
        if (item != null && item.quantity >= quantity)
        {
            item.quantity -= quantity;
            
            if (item.quantity <= 0)
            {
                items.Remove(item);
            }

            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
                Debug.Log($"📤 INVENTAIRE: Retiré {quantity}x {itemName}");

            OnItemsChanged?.Invoke();
            return true;
        }
        
        return false;
    }
    
    public bool HasItemsForQuest(string itemName, int requiredQuantity, string questId)
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);
        bool hasEnough = item != null && item.quantity >= requiredQuantity;
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
            Debug.Log($"🔍 VÉRIFICATION: {itemName} x{requiredQuantity} pour quête {questId} = {(hasEnough ? "OUI" : "NON")}");
            
        return hasEnough;
    }
    
    public void RemoveQuestItem(string itemName, string questId)
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);
        if (item != null)
        {
            items.Remove(item);
            if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
                Debug.Log($"📤 INVENTAIRE: Retiré objet de quête {itemName} (Quête annulée: {questId})");
            OnItemsChanged?.Invoke();
        }
    }
    
    public int GetItemQuantity(string itemName, string questId = "")
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);
        return item?.quantity ?? 0;
    }
    
    // Get all items for a specific quest
    public List<InventoryItem> GetQuestItems(string questId)
    {
        return items.Where(i => i.questId == questId).ToList();
    }
    
    // Check if player has a specific item for a quest
    public bool HasItem(string itemName, string questId)
    {
        return items.Any(i => i.itemName == itemName && i.questId == questId);
    }
    
    [ContextMenu("Show Inventory")]
    public void ShowInventory()
    {
        Debug.Log("=== 📦 INVENTAIRE JOUEUR ===");
        if (items.Count == 0)
        {
            Debug.Log("Inventaire vide");
        }
        else
        {
            foreach (InventoryItem item in items)
            {
                Debug.Log($"• {item.quantity}x {item.itemName} (Quête: {item.questId})");
            }
        }
    }
    
    /// <summary>
    /// Get all items in the inventory
    /// </summary>
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items);
    }
    
    /// <summary>
    /// Clear the entire inventory
    /// </summary>
    public void ClearInventory()
    {
        items.Clear();
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
            Debug.Log("📦 INVENTAIRE: Inventaire vidé");
    }
}