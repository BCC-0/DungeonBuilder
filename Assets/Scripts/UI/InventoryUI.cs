using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the inventory UI for a player, updating item buttons incrementally.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    private Transform contentParent;
    [SerializeField]
    private GameObject itemButtonPrefab;

    private ScrollRect scrollRect;

    // Keeps track of current buttons in the UI
    private Dictionary<Item, ItemButtonUI> itemButtons = new Dictionary<Item, ItemButtonUI>();

    /// <summary>
    /// Updates the inventory UI button incrementally.
    /// Only adds new items and removes items that are no longer in the inventory.
    /// </summary>
    /// <param name="player">The player that owns the inventory.</param>
    /// <param name="items">Items that should be displayed in this section.</param>
    public void PopulateInventory(CrawlerPlayerData player, Item[] items)
    {
        HashSet<Item> currentItems = new HashSet<Item>(items);

        List<Item> toRemove = new List<Item>();
        foreach (var kvp in this.itemButtons)
        {
            if (!currentItems.Contains(kvp.Key))
            {
                Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var item in toRemove)
        {
            this.itemButtons.Remove(item);
        }

        foreach (Item item in items)
        {
            if (!this.itemButtons.TryGetValue(item, out ItemButtonUI buttonUI))
            {
                GameObject buttonObj = Instantiate(this.itemButtonPrefab, this.contentParent);
                buttonUI = buttonObj.GetComponent<ItemButtonUI>();
                this.itemButtons[item] = buttonUI;
            }

            buttonUI.SetItem(player, item);

            if ((item is Weapon && item == player.EquippedWeapon) ||
                (item is Tool && item == player.EquippedTool))
            {
                buttonUI.OnSelect();
            }
            else
            {
                buttonUI.Deselect();
            }
        }
    }

    private void Start()
    {
        this.scrollRect = this.GetComponent<ScrollRect>();
    }
}
