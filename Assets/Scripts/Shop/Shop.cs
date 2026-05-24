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
[RequireComponent(typeof(Collider))]
public class Shop : MonoBehaviour
{
    [Tooltip("Nom affiché en titre de la boutique. Si vide, le nom du NPC est utilisé.")]
    public string shopName;
    public List<ShopItem> catalog = new List<ShopItem>();

    [Tooltip("Touche pour ouvrir la boutique quand le joueur est dans le trigger du PNJ.")]
    public KeyCode openKey = KeyCode.B;

    bool playerInRange;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(openKey))
        {
            if (ShopUI.Instance == null)
            {
                // ShopUI est auto-créée si absente.
                var go = new GameObject("ShopUI");
                go.AddComponent<ShopUI>();
            }
            ShopUI.Instance.OpenFor(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag("Player")) playerInRange = false;
    }

    public string GetShopName()
    {
        if (!string.IsNullOrWhiteSpace(shopName)) return shopName;
        var npc = GetComponent<NPC>();
        return npc != null ? $"Boutique de {npc.npcName}" : "Boutique";
    }
}
