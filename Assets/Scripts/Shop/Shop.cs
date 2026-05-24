using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopItem
{
    public string itemName;
    public int price = 50;
    [Tooltip("Optionnel — description courte affichée dans la boutique.")]
    public string description;
}

/// <summary>
/// Composant à attacher à un PNJ marchand. Expose un catalogue d'objets
/// achetables. La touche B (configurable) près du PNJ ouvre la boutique.
/// </summary>
public class Shop : MonoBehaviour
{
    [Tooltip("Nom affiché en titre de la boutique. Si vide, le nom du NPC est utilisé.")]
    public string shopName;
    public List<ShopItem> catalog = new List<ShopItem>();

    [Tooltip("Touche pour ouvrir la boutique quand le joueur est à portée.")]
    public KeyCode openKey = KeyCode.B;

    [Tooltip("Rayon du trigger de détection (mètres).")]
    public float triggerRadius = 3.5f;

    [Tooltip("Hauteur du prompt 'B Boutique' au-dessus du PNJ.")]
    public float promptHeight = 2.2f;

    bool playerInRange;
    SphereCollider triggerCol;
    GameObject promptObj;
    TMPro.TextMeshPro promptText;

    void Awake()
    {
        // Le NPC peut déjà avoir un Collider non-trigger (corps physique).
        // On ajoute notre propre trigger pour détecter le joueur sans
        // interférer avec la collision physique du NPC.
        triggerCol = gameObject.AddComponent<SphereCollider>();
        triggerCol.isTrigger = true;
        triggerCol.radius = triggerRadius;
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(openKey))
        {
            if (ShopUI.Instance == null)
            {
                var go = new GameObject("ShopUI");
                go.AddComponent<ShopUI>();
            }
            ShopUI.Instance.OpenFor(this);
        }

        // Billboard du prompt vers la caméra (non-parenté pour éviter
        // l'héritage de scale d'un éventuel NPC mal échelonné).
        if (promptObj != null && promptObj.activeSelf)
        {
            promptObj.transform.position = transform.position + Vector3.up * promptHeight;
            if (Camera.main != null)
                promptObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInRange = true;
            ShowPrompt(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInRange = false;
            ShowPrompt(false);
        }
    }

    void ShowPrompt(bool show)
    {
        if (show && promptObj == null)
        {
            promptObj = new GameObject($"ShopPrompt_{gameObject.name}");
            promptObj.transform.position = transform.position + Vector3.up * promptHeight;
            promptObj.transform.localScale = Vector3.one;
            promptText = promptObj.AddComponent<TMPro.TextMeshPro>();
            promptText.fontSize = 3;
            promptText.alignment = TMPro.TextAlignmentOptions.Center;
            promptText.color = new Color(0.95f, 0.75f, 0.3f);
            promptText.text = "[B] Boutique";
        }
        if (promptObj != null) promptObj.SetActive(show);
    }

    void OnDestroy()
    {
        if (promptObj != null) Destroy(promptObj);
    }

    public string GetShopName()
    {
        if (!string.IsNullOrWhiteSpace(shopName)) return shopName;
        var npc = GetComponent<NPC>();
        return npc != null ? $"Boutique de {npc.npcName}" : "Boutique";
    }
}
