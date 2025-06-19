using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Initializes save slots at runtime and manages their display
/// This component must stay on the SaveGameUI object
/// </summary>
public class SaveSlotsInitializer : MonoBehaviour
{
    private SaveGameUI saveUI;
    private List<SaveSlotData> slots = new List<SaveSlotData>();
    
    // Internal class to track slot components
    private class SaveSlotData
    {
        public int index;
        public GameObject gameObject;
        public TextMeshProUGUI displayText;
        public Button saveButton;
        public Button loadButton;
        public Button deleteButton;
    }
    
    void Start()
    {
        saveUI = GetComponent<SaveGameUI>();
        if (saveUI == null)
        {
            Debug.LogError("[SaveSlotsInitializer] SaveGameUI not found!");
            enabled = false;
            return;
        }
        
        // Initialize after a short delay to ensure everything is loaded
        Invoke(nameof(InitializeSlots), 0.1f);
        
        // Subscribe to save/load events
        SaveGameManager.OnGameSaved += OnSaveLoadEvent;
        SaveGameManager.OnGameLoaded += OnSaveLoadEvent;
    }
    
    void OnDestroy()
    {
        SaveGameManager.OnGameSaved -= OnSaveLoadEvent;
        SaveGameManager.OnGameLoaded -= OnSaveLoadEvent;
    }
    
    void InitializeSlots()
    {
        Transform slotContainer = transform.Find("SaveMenuPanel/SaveSlotContainer");
        if (slotContainer == null)
        {
            Debug.LogError("[SaveSlotsInitializer] SaveSlotContainer not found!");
            return;
        }
        
        // Clear existing data
        slots.Clear();
        
        // Process each slot
        for (int i = 0; i < slotContainer.childCount; i++)
        {
            Transform slotTransform = slotContainer.GetChild(i);
            SaveSlotData slotData = new SaveSlotData
            {
                index = i,
                gameObject = slotTransform.gameObject
            };
            
            // Find the display text (first text not in a button)
            TextMeshProUGUI[] texts = slotTransform.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.transform.parent.GetComponent<Button>() == null)
                {
                    slotData.displayText = text;
                    break;
                }
            }
            
            // Find buttons
            Button[] buttons = slotTransform.GetComponentsInChildren<Button>();
            foreach (var button in buttons)
            {
                string btnName = button.name.ToLower();
                
                // Clear existing listeners
                button.onClick.RemoveAllListeners();
                
                if (btnName.Contains("save"))
                {
                    slotData.saveButton = button;
                    int slotIndex = i; // Capture for lambda
                    button.onClick.AddListener(() => saveUI.SaveToSlot(slotIndex));
                }
                else if (btnName.Contains("load"))
                {
                    slotData.loadButton = button;
                    int slotIndex = i; // Capture for lambda
                    button.onClick.AddListener(() => saveUI.LoadFromSlot(slotIndex));
                }
                else if (btnName.Contains("delete"))
                {
                    slotData.deleteButton = button;
                    int slotIndex = i; // Capture for lambda
                    button.onClick.AddListener(() => saveUI.DeleteSlot(slotIndex));
                }
            }
            
            slots.Add(slotData);
        }
        
        // Initial display update
        UpdateAllSlots();
    }
    
    void UpdateAllSlots()
    {
        if (SaveGameManager.Instance == null) return;
        
        foreach (var slot in slots)
        {
            UpdateSlot(slot);
        }
    }
    
    void UpdateSlot(SaveSlotData slot)
    {
        string saveName = $"save_{slot.index}";
        bool hasSave = SaveGameManager.Instance.SaveExists(saveName);
        
        // Update display text
        if (slot.displayText != null)
        {
            slot.displayText.text = hasSave ? $"Claudius-{slot.index + 1}" : "Empty";
        }
        
        // Show/hide buttons
        if (slot.loadButton != null)
        {
            slot.loadButton.gameObject.SetActive(hasSave);
        }
        
        if (slot.deleteButton != null)
        {
            slot.deleteButton.gameObject.SetActive(hasSave);
        }
    }
    
    void OnSaveLoadEvent()
    {
        // Update display after save/load with a small delay
        Invoke(nameof(UpdateAllSlots), 0.1f);
    }
    
    // Public method to force refresh if needed
    public void ForceRefresh()
    {
        UpdateAllSlots();
    }
}
