using UnityEngine;

/// <summary>
/// Script de debug pour tracer le problème de double item dans l'inventaire
/// </summary>
public class InventoryDebugger : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool enableDetailedLogging = true;
    [SerializeField] private float checkInterval = 0.5f;
    
    private float nextCheckTime;
    private int lastItemCount;
    
    void Start()
    {
        if (PlayerInventory.Instance != null)
        {
            Debug.Log("[InventoryDebugger] Starting inventory monitoring...");
            LogCurrentInventory("START");
        }
    }
    
    void Update()
    {
        if (!enableDetailedLogging) return;
        
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            CheckInventoryChanges();
        }
    }
    
    void CheckInventoryChanges()
    {
        if (PlayerInventory.Instance == null) return;
        
        int currentCount = PlayerInventory.Instance.items.Count;
        
        if (currentCount != lastItemCount)
        {
            Debug.LogWarning($"[InventoryDebugger] Item count changed from {lastItemCount} to {currentCount}");
            LogCurrentInventory("CHANGE DETECTED");
            lastItemCount = currentCount;
        }
    }
    
    void LogCurrentInventory(string context)
    {
        if (PlayerInventory.Instance == null) return;
        
        Debug.Log($"[InventoryDebugger] === INVENTORY STATE ({context}) ===");
        Debug.Log($"[InventoryDebugger] Total items: {PlayerInventory.Instance.items.Count}");
        
        foreach (var item in PlayerInventory.Instance.items)
        {
            Debug.Log($"[InventoryDebugger] - {item.quantity}x {item.itemName} (Quest: {item.questId})");
        }
        
        Debug.Log("[InventoryDebugger] ================================");
    }
    
    // Hook into the AddItem method via event
    void OnEnable()
    {
        // Subscribe to inventory changes if we add events later
    }
    
    void OnDisable()
    {
        // Unsubscribe
    }
    
    [ContextMenu("Force Log Inventory")]
    public void ForceLogInventory()
    {
        LogCurrentInventory("MANUAL CHECK");
    }
}