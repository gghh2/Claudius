using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("UI Reference")]
    public InventoryUI inventoryUI;
    
    [Header("Settings")]
    public KeyCode inventoryKey = KeyCode.I;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // Ne pas permettre d'ouvrir l'inventaire si un dialogue est ouvert
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueOpen())
        {
            return;
        }
        
        // Toggle inventory with I key
        if (Input.GetKeyDown(inventoryKey))
        {
            if (inventoryUI != null)
            {
                inventoryUI.ToggleInventory();
            }
        }
    }
}
