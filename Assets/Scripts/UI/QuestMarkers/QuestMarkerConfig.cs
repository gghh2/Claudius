using UnityEngine;

[CreateAssetMenu(fileName = "QuestMarkerConfig", menuName = "Quest System/Quest Marker Configuration")]
public class QuestMarkerConfig : ScriptableObject
{
    [Header("Marker Settings")]
    [Tooltip("Size of the marker arrows")]
    [Range(40f, 120f)]
    public float markerSize = 60f;
    
    [Tooltip("Distance from screen edge")]
    [Range(20f, 100f)]
    public float edgeOffset = 50f;
    
    [Tooltip("Color for quest markers")]
    [ColorUsage(true, false)]
    public Color markerColor = Color.yellow;
    
    [Tooltip("Show distance text below marker")]
    public bool showDistance = true;
    
    [Tooltip("Distance to hide markers when close to zone")]
    [Range(5f, 50f)]
    public float hideDistance = 10f;
    
    [Header("Animation")]
    [Tooltip("Enable pulsing animation")]
    public bool enablePulse = true;
    
    [Tooltip("Pulse speed")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Pulse scale amount")]
    [Range(0.1f, 0.5f)]
    public float pulseAmount = 0.2f;
    
    [Header("Debug")]
    [Tooltip("Enable debug logs")]
    public bool debugMode = false;
}
