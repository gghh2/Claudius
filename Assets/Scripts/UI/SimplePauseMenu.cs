using UnityEngine;

/// <summary>
/// Simple pause menu
/// </summary>
public class SimplePauseMenu : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 spawnPosition = Vector3.zero;
    public Vector3 spawnRotation = Vector3.zero;
    public bool autoSaveSpawnPosition = true;
    
    [Header("Visual")]
    [Tooltip("Background image for the pause menu (leave null for solid color)")]
    public Texture2D backgroundImage;
    
    [Tooltip("Background color (used if no image is set)")]
    public Color backgroundColor = new Color(0, 0, 0, 0.8f);
    
    [Tooltip("Tint color applied to the background image")]
    public Color backgroundImageTint = new Color(1, 1, 1, 0.9f);
    
    [Tooltip("How to scale the background image")]
    public BackgroundScaleMode backgroundScaleMode = BackgroundScaleMode.StretchToFill;
    
    private bool isPaused = false;
    private GameObject player;
    private PlayerController playerController;
    private bool cursorWasLocked = false;
    
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle controlsStyle;
    private bool stylesInitialized = false;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            
            if (autoSaveSpawnPosition)
            {
                spawnPosition = player.transform.position;
                spawnRotation = player.transform.eulerAngles;
            }
        }
    }
    
    void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        titleStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 40,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.white;
        
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        buttonStyle.normal.textColor = Color.white;
        
        controlsStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        controlsStyle.normal.textColor = Color.white;
        controlsStyle.padding = new RectOffset(10, 10, 10, 10);
        
        stylesInitialized = true;
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }
    
    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        if (playerController != null)
            playerController.enabled = false;
    }
    
    void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (cursorWasLocked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        if (playerController != null)
            playerController.enabled = true;
    }
    
    void OnGUI()
    {
        if (!isPaused) return;
        
        InitializeStyles();
        
        // Background
        DrawBackground();
        
        GUI.color = Color.white;
        
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        float buttonWidth = 250;
        float buttonHeight = 60;
        float spacing = 20;
        
        // Title
        GUI.Box(new Rect(centerX - 150, centerY - 200, 300, 80), "PAUSE", titleStyle);
        
        // Buttons
        float buttonsStartY = centerY - 80;
        
        if (GUI.Button(new Rect(centerX - buttonWidth/2, buttonsStartY, buttonWidth, buttonHeight), 
                       "REPRENDRE", buttonStyle))
        {
            Resume();
        }
        
        if (GUI.Button(new Rect(centerX - buttonWidth/2, buttonsStartY + buttonHeight + spacing, buttonWidth, buttonHeight), 
                       "RETOUR AU SPAWN", buttonStyle))
        {
            ResetPlayerPosition();
        }
        
        if (GUI.Button(new Rect(centerX - buttonWidth/2, buttonsStartY + (buttonHeight + spacing) * 2, buttonWidth, buttonHeight), 
                       "QUITTER LE JEU", buttonStyle))
        {
            QuitGame();
        }
        
        // Controls
        string controls = "CONTRÔLES DU JEU\n\n" +
                         "— DÉPLACEMENT —\n" +
                         "Avancer : Z / W\n" +
                         "Reculer : S\n" +
                         "Gauche : Q / A\n" +
                         "Droite : D\n" +
                         "Sauter : Espace\n" +
                         "Sprinter : Shift\n\n" +
                         "— INTERACTION —\n" +
                         "Parler/Interagir : E\n" +
                         "Inventaire : I / Tab\n" +
                         "Journal : J\n\n" +
                         "— SYSTÈME —\n" +
                         "Menu Pause : Échap";
        
        GUI.Box(new Rect(centerX + 200, centerY - 150, 350, 400), controls, controlsStyle);
    }
    
    void ResetPlayerPosition()
    {
        if (player != null)
        {
            player.transform.position = spawnPosition;
            player.transform.eulerAngles = spawnRotation;
            
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = spawnPosition;
                cc.enabled = true;
            }
            
            Resume();
        }
    }
    
    void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void DrawBackground()
    {
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
        
        if (backgroundImage != null)
        {
            // Use custom background image
            GUI.color = backgroundImageTint;
            
            switch (backgroundScaleMode)
            {
                case BackgroundScaleMode.StretchToFill:
                    GUI.DrawTexture(screenRect, backgroundImage);
                    break;
                    
                case BackgroundScaleMode.ScaleToFit:
                    DrawScaledTexture(screenRect, backgroundImage, true);
                    break;
                    
                case BackgroundScaleMode.ScaleToFill:
                    DrawScaledTexture(screenRect, backgroundImage, false);
                    break;
                    
                case BackgroundScaleMode.Center:
                    float x = (Screen.width - backgroundImage.width) / 2f;
                    float y = (Screen.height - backgroundImage.height) / 2f;
                    GUI.DrawTexture(new Rect(x, y, backgroundImage.width, backgroundImage.height), backgroundImage);
                    break;
            }
        }
        else
        {
            // Use solid color
            GUI.color = backgroundColor;
            GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
        }
    }
    
    void DrawScaledTexture(Rect screenRect, Texture2D texture, bool scaleToFit)
    {
        float screenAspect = screenRect.width / screenRect.height;
        float textureAspect = (float)texture.width / texture.height;
        
        Rect drawRect = new Rect();
        
        if (scaleToFit)
        {
            // Scale to fit (may have letterboxing)
            if (screenAspect > textureAspect)
            {
                // Screen is wider - fit height
                drawRect.height = screenRect.height;
                drawRect.width = drawRect.height * textureAspect;
                drawRect.x = (screenRect.width - drawRect.width) / 2f;
                drawRect.y = 0;
            }
            else
            {
                // Screen is taller - fit width
                drawRect.width = screenRect.width;
                drawRect.height = drawRect.width / textureAspect;
                drawRect.x = 0;
                drawRect.y = (screenRect.height - drawRect.height) / 2f;
            }
        }
        else
        {
            // Scale to fill (may crop)
            if (screenAspect > textureAspect)
            {
                // Screen is wider - fit width
                drawRect.width = screenRect.width;
                drawRect.height = drawRect.width / textureAspect;
                drawRect.x = 0;
                drawRect.y = (screenRect.height - drawRect.height) / 2f;
            }
            else
            {
                // Screen is taller - fit height
                drawRect.height = screenRect.height;
                drawRect.width = drawRect.height * textureAspect;
                drawRect.x = (screenRect.width - drawRect.width) / 2f;
                drawRect.y = 0;
            }
        }
        
        GUI.DrawTexture(drawRect, texture);
    }
    
    public enum BackgroundScaleMode
    {
        StretchToFill,  // Stretch to fill screen (may distort)
        ScaleToFit,     // Scale maintaining aspect ratio (may have letterboxing)
        ScaleToFill,    // Scale to fill screen maintaining aspect ratio (may crop)
        Center          // Center at original size
    }
    
    public bool IsPaused() => isPaused;
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}