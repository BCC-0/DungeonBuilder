using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single item in the inventory UI.
/// </summary>
public class InventoryItemUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private Image itemImage;

    private InventoryItem item;

    /// <summary>
    /// Gets the inventory item this contains.
    /// </summary>
    public InventoryItem Item
    {
        get { return this.item; }
    }

    /// <summary>
    /// Initializes this UI element with an inventory item.
    /// </summary>
    /// <param name="inventoryItem">The item represented by this UI element.</param>
    public void Initialize(InventoryItem inventoryItem)
    {
        this.item = inventoryItem;

        if (this.itemNameText != null)
        {
            this.itemNameText.text = this.item.Name;
        }

        if (this.itemImage != null)
        {
            this.itemImage.sprite = inventoryItem.Sprite;
        }
    }

    /// <summary>
    /// Selects this item for entity placement.
    /// </summary>
    public void Select()
    {
        if (this.item == null)
        {
            return;
        }

        Debug.Log("Selected " + this.itemNameText.text);
    }
}