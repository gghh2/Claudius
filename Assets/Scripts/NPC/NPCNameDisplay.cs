using UnityEngine;
using TMPro;

public class NPCNameDisplay : MonoBehaviour
{
    [Header("Name Display Settings")]
    public GameObject nameDisplayPrefab; // Optionnel : préfab personnalisé
    public Vector3 nameOffset = new Vector3(0, 2.5f, 0); // Position au-dessus de la tête
    public float maxDisplayDistance = 15f; // Distance max pour afficher le nom
    public bool alwaysShow = false; // Toujours visible ou seulement à distance raisonnable
    
    [Header("Text Settings")]
    public float fontSize = 4f;
    public Color textColor = Color.white;
    public bool useNPCColor = true; // Utilise la couleur du NPC
    
    private GameObject nameDisplay;
    private TextMeshPro nameText;
    private Transform playerTransform;
    private Camera mainCamera;
    private NPC npcScript;
    
    void Start()
    {
        // Récupère les références
        npcScript = GetComponent<NPC>();
        mainCamera = Camera.main;
        
        // Trouve le joueur
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        CreateNameDisplay();
    }
    
    void CreateNameDisplay()
    {
        if (nameDisplayPrefab != null)
        {
            // Utilise le préfab personnalisé
            nameDisplay = Instantiate(nameDisplayPrefab, transform.position + nameOffset, Quaternion.identity);
            nameDisplay.transform.SetParent(transform);
            nameText = nameDisplay.GetComponent<TextMeshPro>();
        }
        else
        {
            // Crée automatiquement l'affichage du nom
            nameDisplay = new GameObject($"{gameObject.name}_NameDisplay");
            nameDisplay.transform.SetParent(transform);
            nameDisplay.transform.localPosition = nameOffset;
            
            // Ajoute le composant TextMeshPro
            nameText = nameDisplay.AddComponent<TextMeshPro>();
        }
        
        // Configure le texte
        if (nameText != null && npcScript != null)
        {
            nameText.text = npcScript.npcName;
            nameText.fontSize = fontSize;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.horizontalAlignment = HorizontalAlignmentOptions.Center;
			nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // Couleur du texte
            if (useNPCColor)
            {
                nameText.color = npcScript.npcColor;
            }
            else
            {
                nameText.color = textColor;
            }
            
            // Configure pour que le texte soit lisible
            nameText.fontStyle = FontStyles.Bold;
            nameText.enableAutoSizing = false;
            
            // Ajoute un outline pour la lisibilité
            nameText.outlineWidth = 0.2f;
            nameText.outlineColor = Color.black;
        }
    }
    
    void Update()
    {
        if (nameDisplay == null || mainCamera == null) return;
        
        // Fait toujours regarder la caméra (billboard effect)
        nameDisplay.transform.LookAt(mainCamera.transform);
        nameDisplay.transform.Rotate(0, 180, 0); // Retourne pour que le texte soit dans le bon sens
        
        // Gère la visibilité selon la distance
        if (!alwaysShow && playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool shouldShow = distanceToPlayer <= maxDisplayDistance;
            
            if (nameDisplay.activeSelf != shouldShow)
            {
                nameDisplay.SetActive(shouldShow);
            }
            
            // Optionnel : ajuste l'opacité selon la distance
            if (shouldShow && nameText != null)
            {
                float alpha = Mathf.Lerp(1f, 0.3f, distanceToPlayer / maxDisplayDistance);
                Color currentColor = nameText.color;
                currentColor.a = alpha;
                nameText.color = currentColor;
            }
        }
    }
    
    // Méthode pour changer la couleur du nom
    public void SetNameColor(Color newColor)
    {
        if (nameText != null)
        {
            nameText.color = newColor;
        }
    }
    
    // Méthode pour changer le texte affiché
    public void SetDisplayName(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
        }
    }
    
    // Cache temporairement le nom (utile pendant les dialogues)
    public void HideName()
    {
        if (nameDisplay != null)
        {
            nameDisplay.SetActive(false);
        }
    }
    
    public void ShowName()
    {
        if (nameDisplay != null)
        {
            nameDisplay.SetActive(true);
        }
    }
}