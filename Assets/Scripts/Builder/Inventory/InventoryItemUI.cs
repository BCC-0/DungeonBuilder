using UnityEngine;

/// <summary>
/// Controls a single item in the inventory UI.
/// </summary>
public class InventoryItemUI : MonoBehaviour
{
    private InventoryItem item;

    /// <summary>
    /// Initializes this UI element with an inventory item.
    /// </summary>
    /// <param name="inventoryItem">The item represented by this UI element.</param>
    public void Initialize(InventoryItem inventoryItem)
    {
        this.item = inventoryItem;

        // Set text/icon here.
    }

    /// <summary>
    /// Selects this item for entity placement.
    /// </summary>
    public void Select()
    {
        EntityEditorController controller =
            FindAnyObjectByType<EntityEditorController>();

        if (controller == null)
        {
            return;
        }

        controller.SelectedPrefab = this.item.Prefab;
    }
}