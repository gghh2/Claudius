using UnityEngine;

/// <summary>
/// Simple marker component to help identify the NotificationPanel
/// Attach this to your NotificationPanel GameObject
/// </summary>
public class NotificationPanel : MonoBehaviour
{
    void Awake()
    {
        // Ensure the panel starts hidden
        gameObject.SetActive(false);
    }
    
    void OnValidate()
    {
        // Help identify this panel in the editor
        if (string.IsNullOrEmpty(gameObject.name) || !gameObject.name.Contains("Notification"))
        {
            Debug.LogWarning($"[NotificationPanel] This GameObject should be named 'NotificationPanel' for auto-detection to work. Current name: {gameObject.name}");
        }
    }
}