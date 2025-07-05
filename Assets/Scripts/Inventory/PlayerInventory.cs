using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public int quantity;
    public string questId;
    
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
    
    [Header("Inventory")]
    public List<InventoryItem> items = new List<InventoryItem>();
    
    // Debug est maintenant géré par GlobalDebugManager
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        // LOG DÉTAILLÉ POUR DEBUG
        Debug.LogWarning($"[PlayerInventory.AddItem] CALLED - Item: {itemName}, Quantity: {quantity}, QuestId: {questId}");
        Debug.LogWarning($"[PlayerInventory.AddItem] Stack Trace:\n{System.Environment.StackTrace}");
        
        InventoryItem existingItem = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);
        
        if (existingItem != null)
        {
            Debug.LogWarning($"[PlayerInventory.AddItem] EXISTING ITEM FOUND - Current quantity: {existingItem.quantity}, Adding: {quantity}");
            existingItem.quantity += quantity;
            Debug.LogWarning($"[PlayerInventory.AddItem] NEW QUANTITY: {existingItem.quantity}");
        }
        else
        {
            Debug.LogWarning($"[PlayerInventory.AddItem] NEW ITEM - Creating with quantity: {quantity}");
            items.Add(new InventoryItem(itemName, quantity, questId));
        }
        
        if (GlobalDebugManager.IsDebugEnabled(DebugSystem.Player))
        {
            Debug.Log($"📦 INVENTAIRE: Ajouté {quantity}x {itemName} (Quête: {questId})");
            ShowInventory(); // Debug automatique
        }
        
        // Force un log de l'inventaire complet
        Debug.LogWarning("[PlayerInventory.AddItem] INVENTORY AFTER ADD:");
        foreach (var item in items)
        {
            Debug.LogWarning($"  - {item.quantity}x {item.itemName} (Quest: {item.questId})");
        }
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