using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Selection manager for tile-only editor layers. No entity awareness
/// needed — everything here only deals with cells on a <see cref="SaveableTilemap"/>.
/// </summary>
public class TileSelectionManager : SelectionManagerBase
{
    /// <summary>
    /// Tilemap this manager selects cells from.
    /// </summary>
    [SerializeField]
    private SaveableTilemap saveableTilemap;

    /// <summary>
    /// Optional visualizer that highlights the current tile selection.
    /// </summary>
    [SerializeField]
    private TileSelectionVisualizer selectionOverlay;

    /// <inheritdoc/>
    public override void ClearSelection()
    {
        this.SetSelection(new List<Vector3Int>());
    }

    /// <inheritdoc/>
    public override void DeleteSelected()
    {
        // TODO: Add actual deletion of selected tiles from the tilemap.
        this.ClearSelection();
    }

    /// <summary>
    /// Selects the tile at the clicked cell, if one exists.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected override void OnClickSelect(Vector3 position)
    {
        Vector3Int cellPos = this.saveableTilemap.Tilemap.WorldToCell(position);

        if (this.saveableTilemap.Tilemap.HasTile(cellPos))
        {
            this.SetSelection(new List<Vector3Int> { cellPos });
        }
        else
        {
            this.ClearSelection();
        }
    }

    /// <summary>
    /// Selects every tile whose cell falls within the dragged rectangle.
    /// </summary>
    /// <param name="rect">The world-space rectangle of the drag.</param>
    protected override void OnBoxSelect(Rect rect)
    {
        List<Vector3Int> selected = new List<Vector3Int>();

        Vector3Int min = this.saveableTilemap.Tilemap.WorldToCell(rect.min);
        Vector3Int max = this.saveableTilemap.Tilemap.WorldToCell(rect.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (this.saveableTilemap.Tilemap.HasTile(cell))
                {
                    selected.Add(cell);
                }
            }
        }

        this.SetSelection(selected);
    }

    /// <summary>
    /// Writes the given selection to <see cref="MapEditorManager"/> and refreshes
    /// the visualizer, if one is assigned.
    /// </summary>
    /// <param name="tiles">The tile cells to select.</param>
    private void SetSelection(List<Vector3Int> tiles)
    {
        MapEditorManager.Instance.SelectedTiles = tiles;

        this.selectionOverlay.Refresh(tiles);
    }
}
