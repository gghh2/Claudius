using UnityEngine;

/// <summary>
/// Simple script to test the notification system
/// Attach this to any GameObject in the scene
/// </summary>
public class NotificationTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool enableKeyboardTesting = true;
    
    void Update()
    {
        if (!enableKeyboardTesting) return;
        
        // Test different notification types with keyboard
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestSuccessNotification();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestErrorNotification();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestInfoNotification();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestWarningNotification();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TestLongNotification();
        }
    }
    
    public void TestSuccessNotification()
    {
        Debug.Log("[NotificationTester] Testing Success notification");
        
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowSuccess("Succès ! La quête a été ajoutée.");
        }
        else
        {
            Debug.LogError("[NotificationTester] NotificationManager.Instance is NULL!");
        }
    }
    
    public void TestErrorNotification()
    {
        Debug.Log("[NotificationTester] Testing Error notification");
        
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowError("Erreur : Impossible de terminer la quête.");
        }
        else
        {
            Debug.LogError("[NotificationTester] NotificationManager.Instance is NULL!");
        }
    }
    
    public void TestInfoNotification()
    {
        Debug.Log("[NotificationTester] Testing Info notification");
        
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowInfo("Information : Nouvelle zone découverte !");
        }
        else
        {
            Debug.LogError("[NotificationTester] NotificationManager.Instance is NULL!");
        }
    }
    
    public void TestWarningNotification()
    {
        Debug.Log("[NotificationTester] Testing Warning notification");
        
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowWarning("Attention : Inventaire presque plein !");
        }
        else
        {
            Debug.LogError("[NotificationTester] NotificationManager.Instance is NULL!");
        }
    }
    
    public void TestLongNotification()
    {
        Debug.Log("[NotificationTester] Testing Long notification (5 seconds)");
        
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowInfo("Cette notification reste affichée plus longtemps (5 secondes)", 5f);
        }
        else
        {
            Debug.LogError("[NotificationTester] NotificationManager.Instance is NULL!");
        }
    }
    
    void OnGUI()
    {
        if (!enableKeyboardTesting) return;
        
        // Show instructions
        GUI.Box(new Rect(10, 10, 300, 140), "Test Notifications");
        GUI.Label(new Rect(20, 35, 280, 20), "Appuyez sur les touches:");
        GUI.Label(new Rect(20, 55, 280, 20), "1 - Notification Succès");
        GUI.Label(new Rect(20, 75, 280, 20), "2 - Notification Erreur");
        GUI.Label(new Rect(20, 95, 280, 20), "3 - Notification Info");
        GUI.Label(new Rect(20, 115, 280, 20), "4 - Notification Avertissement");
        GUI.Label(new Rect(20, 135, 280, 20), "5 - Notification Longue (5s)");
    }
}