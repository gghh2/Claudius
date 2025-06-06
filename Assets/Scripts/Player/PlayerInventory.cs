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
    
    [Header("Debug")]
    public bool debugMode = true;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ PlayerInventory Instance créée");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void AddItem(string itemName, int quantity = 1, string questId = "")
    {
        InventoryItem existingItem = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);
        
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            items.Add(new InventoryItem(itemName, quantity, questId));
        }
        
        if (debugMode)
            Debug.Log($"📦 INVENTAIRE: Ajouté {quantity}x {itemName} (Quête: {questId})");
            
        ShowInventory(); // Debug automatique
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
            
            if (debugMode)
                Debug.Log($"📤 INVENTAIRE: Retiré {quantity}x {itemName}");
            
            return true;
        }
        
        return false;
    }
    
    public bool HasItemsForQuest(string itemName, int requiredQuantity, string questId)
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);
        bool hasEnough = item != null && item.quantity >= requiredQuantity;
        
        if (debugMode)
            Debug.Log($"🔍 VÉRIFICATION: {itemName} x{requiredQuantity} pour quête {questId} = {(hasEnough ? "OUI" : "NON")}");
            
        return hasEnough;
    }
    
    public int GetItemQuantity(string itemName, string questId = "")
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemName == itemName && i.questId == questId);
        return item?.quantity ?? 0;
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
}