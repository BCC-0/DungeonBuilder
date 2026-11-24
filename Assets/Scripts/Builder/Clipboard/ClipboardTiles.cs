using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A copied set of tile cells, stored relative to the clipboard's own origin cell.
/// </summary>
public class ClipboardTiles
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardTiles"/> class.
    /// </summary>
    /// <param name="cells">The copied cells, normalized so the smallest cell is at the origin.</param>
    public ClipboardTiles(Dictionary<Vector2Int, TileState> cells)
    {
        this.Cells = cells;
    }

    /// <summary>
    /// Gets the copied cells and their states, relative to the clipboard's origin.
    /// </summary>
    public Dictionary<Vector2Int, TileState> Cells { get; }
}
