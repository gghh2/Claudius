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
        
        // NOUVEAU: Invoque une mise à jour différée au cas où le nom change après Start()
        Invoke(nameof(RefreshDisplayName), 0.1f);
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
            // NOUVEAU: Formate le nom pour enlever les underscores
            string formattedName = TextFormatter.FormatName(npcScript.npcName);
            nameText.text = formattedName;
            
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
            nameText.fontStyle = FontStyles.Normal;
            nameText.enableAutoSizing = false;
            
            // Pas d'outline
            nameText.outlineWidth = 0f;
        }
    }
    
    void Update()
    {
        if (nameDisplay == null || mainCamera == null) return;
        
        // Billboard effect - always face camera direction
        nameDisplay.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
        
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
            // NOUVEAU: Formate le nom pour enlever les underscores
            nameText.text = TextFormatter.FormatName(newName);
        }
    }
    
    // NOUVELLE MÉTHODE: Rafraîchit le nom affiché depuis le composant NPC
    public void RefreshDisplayName()
    {
        if (nameText != null && npcScript != null)
        {
            // NOUVEAU: Formate le nom pour enlever les underscores
            nameText.text = TextFormatter.FormatName(npcScript.npcName);
            
            // Met à jour la couleur aussi
            if (useNPCColor)
            {
                nameText.color = npcScript.npcColor;
            }
            
            Debug.Log($"[NPCNameDisplay] Nom rafraîchi: {npcScript.npcName} -> {nameText.text}");
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