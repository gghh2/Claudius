using UnityEngine;
using TMPro;

/// <summary>
/// Displays interaction prompts above interactable objects
/// </summary>
public class InteractionPrompt : MonoBehaviour
{
    private static InteractionPrompt instance;
    
    [Header("UI Settings")]
    public GameObject promptPrefab;
    public Vector3 offset = new Vector3(0, 2f, 0);
    public float fadeSpeed = 5f;
    
    private GameObject currentPrompt;
    private TextMeshProUGUI promptText;
    private CanvasGroup canvasGroup;
    private Transform target;
    private Transform lastCaller; // Track who called Show last
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            CreatePromptUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Static constructor to ensure instance exists
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (instance == null)
        {
            GameObject promptManager = new GameObject("InteractionPromptManager");
            promptManager.AddComponent<InteractionPrompt>();
            DontDestroyOnLoad(promptManager);
            Debug.Log("[InteractionPrompt] Auto-created instance");
        }
    }
    
    void CreatePromptUI()
    {
        // Create UI Canvas if needed
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("InteractionCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create prompt GameObject
        currentPrompt = new GameObject("InteractionPrompt");
        currentPrompt.transform.SetParent(canvas.transform, false);
        
        // Add CanvasGroup for fading
        canvasGroup = currentPrompt.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        // Add background
        UnityEngine.UI.Image bg = currentPrompt.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0, 0, 0, 0.8f);
        
        // Add text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(currentPrompt.transform, false);
        promptText = textGO.AddComponent<TextMeshProUGUI>();
        promptText.text = "Appuyez sur E pour interagir";
        promptText.fontSize = 18;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;
        
        // Setup RectTransform
        RectTransform rt = currentPrompt.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250, 50);
        
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = Vector2.zero;
        
        currentPrompt.SetActive(false);
    }
    
    public static void Show(string text, Transform targetTransform, Vector3 customOffset = default)
    {
        // Ensure instance exists
        if (instance == null)
        {
            Initialize();
        }
        
        if (instance != null)
        {
            instance.ShowPrompt(text, targetTransform, customOffset);
        }
    }
    
    public static void Hide()
    {
        if (instance != null)
        {
            instance.HidePrompt();
        }
    }
    
    void ShowPrompt(string text, Transform targetTransform, Vector3 customOffset)
    {
        target = targetTransform;
        lastCaller = targetTransform;
        promptText.text = text;
        currentPrompt.SetActive(true);
        
        if (customOffset != default)
            offset = customOffset;
    }
    
    void HidePrompt()
    {
        target = null;
        lastCaller = null;
        currentPrompt.SetActive(false);
    }
    
    public static void HideIfCaller(Transform caller)
    {
        // Only hide if the caller was the one who showed the prompt
        if (instance != null && instance.lastCaller == caller)
        {
            instance.HidePrompt();
        }
    }
    
    void Update()
    {
        if (currentPrompt.activeSelf)
        {
            // Update position
            if (target != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position + offset);
                currentPrompt.transform.position = screenPos;
            }
            
            // Fade in
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
        }
        else
        {
            // Fade out
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }
}