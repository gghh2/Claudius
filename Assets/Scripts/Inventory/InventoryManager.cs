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
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Note : la touche I est gérée par UnifiedUIManager (ouverture + toggle).
    // InventoryManager ne traite plus l'input ici pour éviter le double-fire
    // (deux NavigateTo/NavigateBack sur la même frame -> il fallait 2 pressions
    // pour voir l'inventaire). Le refresh à l'ouverture est géré par
    // InventoryUI.OnEnable.
}
