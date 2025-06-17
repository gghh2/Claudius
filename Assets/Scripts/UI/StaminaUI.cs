using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StaminaUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject staminaBarContainer; // Le GameObject parent de la barre
    public Image staminaFillImage; // L'image de remplissage de la barre
    public Image staminaBackgroundImage; // L'image de fond de la barre
    public TextMeshProUGUI staminaText; // Texte optionnel (100/100)
    
    [Header("UI Settings")]
    public bool showStaminaText = true;
    public bool hideWhenFull = true;
    public float fadeSpeed = 2f;
    public float hideDelay = 2f; // Délai avant de cacher quand pleine
    
    [Header("Colors")]
    public Gradient staminaGradient; // Gradient de couleur selon le niveau
    public Color backgroundColor = new Color(0, 0, 0, 0.5f);
    
    [Header("Position & Size")]
    public Vector2 barSize = new Vector2(200, 20);
    public Vector2 screenPosition = new Vector2(10, 10); // Depuis bas-gauche
    
    // Références
    private PlayerControllerCC playerController;
    private CanvasGroup canvasGroup;
    private RectTransform containerRect;
    private float hideTimer = 0f;
    
    void Start()
    {
        SetupUI();
        FindPlayerController();
    }
    
    void SetupUI()
    {
        // Si les références UI ne sont pas assignées, crée l'UI
        if (staminaBarContainer == null)
        {
            CreateStaminaBar();
        }
        
        // Récupère ou ajoute le CanvasGroup pour le fade
        canvasGroup = staminaBarContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = staminaBarContainer.AddComponent<CanvasGroup>();
        }
        
        // Configure la position et taille
        containerRect = staminaBarContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerRect.sizeDelta = barSize;
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 0);
            containerRect.anchoredPosition = screenPosition;
        }
        
        // Configure les couleurs
        if (staminaBackgroundImage != null)
        {
            staminaBackgroundImage.color = backgroundColor;
        }
        
        // Cache au départ si hideWhenFull est activé
        if (hideWhenFull)
        {
            canvasGroup.alpha = 0f;
        }
    }
    
    void CreateStaminaBar()
    {
        // Trouve ou crée le Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Crée le container de la barre
        staminaBarContainer = new GameObject("StaminaBar");
        staminaBarContainer.transform.SetParent(canvas.transform, false);
        
        // Ajoute le RectTransform
        RectTransform containerRT = staminaBarContainer.AddComponent<RectTransform>();
        
        // Crée le fond de la barre
        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(staminaBarContainer.transform, false);
        staminaBackgroundImage = backgroundGO.AddComponent<Image>();
        staminaBackgroundImage.color = backgroundColor;
        
        RectTransform bgRT = backgroundGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        bgRT.anchoredPosition = Vector2.zero;
        
        // Crée la barre de remplissage
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(staminaBarContainer.transform, false);
        staminaFillImage = fillGO.AddComponent<Image>();
        staminaFillImage.color = Color.green;
        staminaFillImage.type = Image.Type.Filled;
        staminaFillImage.fillMethod = Image.FillMethod.Horizontal;
        staminaFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;
        fillRT.anchoredPosition = Vector2.zero;
        
        // Crée le texte (optionnel)
        if (showStaminaText)
        {
            GameObject textGO = new GameObject("StaminaText");
            textGO.transform.SetParent(staminaBarContainer.transform, false);
            staminaText = textGO.AddComponent<TextMeshProUGUI>();
            staminaText.text = "Stamina 100/100";
            staminaText.fontSize = 14;
            staminaText.color = Color.black;
            staminaText.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.anchoredPosition = Vector2.zero;
        }
        
        // Configure le gradient par défaut
        if (staminaGradient == null || staminaGradient.colorKeys.Length == 0)
        {
            staminaGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.red, 0.0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 1.0f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            staminaGradient.SetKeys(colorKeys, alphaKeys);
        }
    }
    
    void FindPlayerController()
    {
        playerController = FindObjectOfType<PlayerControllerCC>();
        
        if (playerController == null)
        {
            Debug.LogError("❌ PlayerControllerCC non trouvé !");
        }
        else
        {
            Debug.Log("✅ StaminaUI connectée au PlayerControllerCC");
        }
    }
    
    void Update()
    {
        if (playerController == null) return;
        
        // Récupère le pourcentage de stamina
        float staminaPercentage = playerController.GetStaminaPercentage();
        
        // Met à jour la barre
        UpdateStaminaBar(staminaPercentage);
        
        // Gère la visibilité
        UpdateVisibility(staminaPercentage);
    }
    
    void UpdateStaminaBar(float percentage)
    {
        // Met à jour le remplissage
        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = percentage;
            
            // Applique la couleur du gradient
            Color barColor = staminaGradient.Evaluate(percentage);
            staminaFillImage.color = barColor;
        }
        
        // Met à jour le texte
        if (showStaminaText && staminaText != null && playerController.useStamina)
        {
            float current = playerController.currentStamina;
            float max = playerController.maxStamina;
            staminaText.text = $"Stamina : {Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
        }
    }
    
    void UpdateVisibility(float percentage)
    {
        if (!hideWhenFull || !playerController.useStamina)
        {
            canvasGroup.alpha = 1f;
            return;
        }
        
        bool shouldShow = percentage < 0.99f || playerController.IsSprinting();
        
        if (shouldShow)
        {
            // Montre la barre (utilise unscaledDeltaTime pour continuer pendant la pause)
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.unscaledDeltaTime * fadeSpeed);
            hideTimer = 0f;
        }
        else
        {
            // Commence le timer pour cacher (utilise unscaledDeltaTime)
            hideTimer += Time.unscaledDeltaTime;
            
            if (hideTimer >= hideDelay)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.unscaledDeltaTime * fadeSpeed);
            }
        }
    }
    
    // Méthodes publiques pour personnalisation
    public void SetBarSize(Vector2 newSize)
    {
        barSize = newSize;
        if (containerRect != null)
            containerRect.sizeDelta = barSize;
    }
    
    public void SetBarPosition(Vector2 newPosition)
    {
        screenPosition = newPosition;
        if (containerRect != null)
            containerRect.anchoredPosition = screenPosition;
    }
    
    public void ShowStaminaBar()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        hideTimer = 0f;
    }
    
    public void HideStaminaBar()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
}