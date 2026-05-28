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

    bool playerInRange;
    SphereCollider triggerCol;

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
        // Defenses cumulees pour eviter une ouverture parasite pendant un
        // dialogue (la frappe B fuyait vers Shop dans des fenetres de focus
        // transitoires apres Enter).
        bool isTyping = UIInputUtils.IsTypingInInputField();
        bool dialogueOpen = DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueOpen();
        bool blockedByUI = UnifiedUIManager.Instance != null && UnifiedUIManager.Instance.IsBlockingGameplay();

        if (playerInRange && Input.GetKeyDown(openKey) && !isTyping && !dialogueOpen && !blockedByUI)
        {
            if (ShopUI.Instance == null)
            {
                var go = new GameObject("ShopUI");
                go.AddComponent<ShopUI>();
            }
            ShopUI.Instance.OpenFor(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInRange = true;
            // L'affichage "[B] Boutique" est desormais fusionne dans
            // NPCNameDisplay (cf. NPC.ShowInteractionPrompt) — pas de
            // billboard separe pour eviter le chevauchement.
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    public string GetShopName()
    {
        if (!string.IsNullOrWhiteSpace(shopName)) return shopName;
        var npc = GetComponent<NPC>();
        return npc != null ? $"Boutique de {npc.npcName}" : "Boutique";
    }
}
