using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the discovered inventory folders and items.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    private readonly List<InventoryFolder> folders = new ();

    /// <summary>
    /// Gets all inventory folders.
    /// </summary>
    public IReadOnlyList<InventoryFolder> Folders => this.folders;

    /// <summary>
    /// Replaces the current inventory with the supplied folders.
    /// </summary>
    /// <param name="newFolders">The folders to use.</param>
    public void SetFolders(List<InventoryFolder> newFolders)
    {
        this.folders.Clear();
        this.folders.AddRange(newFolders);
    }
}