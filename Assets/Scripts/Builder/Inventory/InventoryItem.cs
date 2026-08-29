using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents a placeable entity in the inventory.
/// </summary>
public class InventoryItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItem"/> class.
    /// </summary>
    /// <param name="prefab">The prefab this item represents.</param>
    /// <param name="sprite">The sprite representing the item.</param>
    public InventoryItem(GameObject prefab, Sprite sprite)
    {
        this.Prefab = prefab;
        this.Sprite = sprite;
    }

    /// <summary>
    /// Gets the prefab represented by this inventory item.
    /// </summary>
    public GameObject Prefab { get; }

    /// <summary>
    /// Gets the display name of the item.
    /// </summary>
    public string Name => this.Prefab.name;

    /// <summary>
    /// Gets the sprite image of this item.
    /// </summary>
    public Sprite Sprite { get; }
}