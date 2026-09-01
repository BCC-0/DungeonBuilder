using UnityEngine;

/// <summary>
/// The slots of the bottom bar in the builder.
/// </summary>
public class ItemBarSlot : MonoBehaviour
{
    [SerializeField]
    private int slotIndex;

    private InventoryItem item;

    public InventoryItem Item
    {
        get { return item; }
    }

    /// <summary>
    /// Sets the item of this slot and animates setting it from the given position.
    /// </summary>
    /// <param name="item">The item this slot should contain.</param>
    /// <param name="screenPos">The on screen position from where to animate the item coming from.</param>
    public void SetItem(InventoryItem item, Vector3 screenPos)
    {
        
    }
}
