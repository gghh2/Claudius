// Assets/Scripts/AI/Core/AIConfig.cs
using UnityEngine;

[System.Serializable]
public class AIConfig
{
    [Header("API Configuration")]
    [HideInInspector] // Cache la clé API dans l'Inspector
    public string apiKey = "";
    
    [Header("Model Settings")]
    [Tooltip("GPT model to use")]
    public string model = "gpt-3.5-turbo";
    
    [Tooltip("Generation temperature (0=deterministic, 1=creative)")]
    [Range(0f, 1f)]
    public float temperature = 0.8f;
    
    [Tooltip("Maximum tokens per response")]
    public int maxTokens = 150;
    
    [Header("Debug")]
    public bool showApiStatus = true;
}