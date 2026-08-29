using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a folder/category in the inventory.
/// </summary>
public class InventoryFolder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryFolder"/> class.
    /// </summary>
    /// <param name="name">The name of this folder.</param>
    /// <param name="image">The image of this folder.</param>
    public InventoryFolder(string name, Sprite image)
    {
        this.Name = name;
        this.Image = image;
        this.Items = new List<InventoryItem>();
    }

    /// <summary>
    /// Gets the name of the folder.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the image of the folder.
    /// </summary>
    public Sprite Image { get; }

    /// <summary>
    /// Gets the items contained in this folder.
    /// </summary>
    public List<InventoryItem> Items { get; }
}