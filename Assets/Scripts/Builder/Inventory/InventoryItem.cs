using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Represents a placeable item in the inventory.
/// </summary>
public class InventoryItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItem"/> class.
    /// Initializes an inventory item representing a prefab.
    /// </summary>
    /// <param name="prefab">The prefab this item represents.</param>
    /// <param name="sprite">The sprite representing the item.</param>
    public InventoryItem(GameObject prefab, Sprite sprite)
    {
        this.Prefab = prefab;
        this.Sprite = sprite;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItem"/> class.
    /// Initializes an inventory item representing a tile.
    /// </summary>
    /// <param name="tile">The tile this item represents.</param>
    /// <param name="sprite">The sprite representing the item.</param>
    public InventoryItem(TileBase tile, Sprite sprite)
    {
        this.Tile = tile;
        this.Sprite = sprite;
    }

    /// <summary>
    /// Gets the prefab represented by this inventory item.
    /// </summary>
    public GameObject Prefab { get; }

    /// <summary>
    /// Gets the tile represented by this inventory item.
    /// </summary>
    public TileBase Tile { get; }

    /// <summary>
    /// Gets a value indicating whether this item represents a tile.
    /// </summary>
    public bool IsTile => this.Tile != null;

    /// <summary>
    /// Gets the display name of the item.
    /// </summary>
    public string Name =>
        this.Prefab != null
            ? this.Prefab.name
            : this.Tile != null
                ? this.Tile.name
                : string.Empty;

    /// <summary>
    /// Gets the sprite image of this item.
    /// </summary>
    public Sprite Sprite { get; }
}
