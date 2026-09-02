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
    private InventoryController inventoryController;

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
    /// <param name="inventoryItem">The inventory item represented by this UI element.</param>
    /// <param name="controller">The inventory controller.</param>
    public void Initialize(
        InventoryItem inventoryItem,
        InventoryController controller)
    {
        this.item = inventoryItem;
        this.inventoryController = controller;

        if (this.itemNameText != null)
        {
            this.itemNameText.text = this.item.Name;
        }

        if (this.itemImage != null)
        {
            this.itemImage.sprite = this.item.Sprite;
        }
    }

    /// <summary>
    /// Selects this item for assigning to a toolbar slot.
    /// </summary>
    public void Select()
    {
        if (this.item == null || this.inventoryController == null)
        {
            return;
        }

        this.inventoryController.SelectItem(this);
    }
}
