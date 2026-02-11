using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script that decides which item buttons to show when opening inventory.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    private Transform contentParent;
    [SerializeField]
    private GameObject itemButtonPrefab;
    private ScrollRect scrollRect;

    /// <summary>
    /// Updates the inventory UI button.
    /// </summary>
    /// <param name="player">The player that owns the inventories.</param>
    /// <param name="items">The items to put into this inventory part.</param>
    public void PopulateInventory(CrawlerPlayerData player, Item[] items)
    {
        // First delete old buttons.
        foreach (Transform child in this.contentParent)
        {
            Destroy(child.gameObject);
        }

        // Then, spawn new buttons.
        foreach (Item item in items)
        {
            GameObject buttonObj = Instantiate(this.itemButtonPrefab, this.contentParent);
            ItemButtonUI buttonUI = buttonObj.GetComponent<ItemButtonUI>();
            buttonUI.SetItem(player, item);
        }

        Canvas.ForceUpdateCanvases();
        this.scrollRect.verticalNormalizedPosition = 1f;
    }

    private void Awake()
    {
        this.scrollRect = this.GetComponent<ScrollRect>();
    }
}
