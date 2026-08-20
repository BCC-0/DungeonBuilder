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
    /// Visualizer that highlights the current tile selection.
    /// </summary>
    private TileSelectionVisualizer selectionVisualizer;

    /// <inheritdoc/>
    public override void ClearSelection()
    {
        this.SetSelection(new List<Vector2Int>());
    }

    /// <inheritdoc/>
    public override void DeleteSelected()
    {
        foreach (Vector2Int tile in MapEditorManager.Instance.SelectedTiles)
        {
            this.saveableTilemap.SetTile(tile.x, tile.y, tileID: null);
        }

        this.ClearSelection();
    }

    /// <summary>
    /// Finds the correct visualizer.
    /// </summary>
    protected override void Awake()
    {
        this.selectionVisualizer = FindAnyObjectByType<TileSelectionVisualizer>();
        base.Awake();
    }

    /// <summary>
    /// Selects the tile at the clicked cell, if one exists.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected override void OnClickSelect(Vector2 position)
    {
        Vector2Int cellPos = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(position);

        if (this.saveableTilemap.Tilemap.HasTile(new (cellPos.x, cellPos.y, 0)))
        {
            this.SetSelection(new List<Vector2Int> { cellPos });
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
        List<Vector2Int> selected = new List<Vector2Int>();

        Vector2Int min = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(rect.min);
        Vector2Int max = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(rect.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (this.saveableTilemap.Tilemap.HasTile(new (cell.x, cell.y, 0)))
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
    private void SetSelection(List<Vector2Int> tiles)
    {
        MapEditorManager.Instance.SelectedTiles = tiles;

        this.selectionVisualizer.Refresh(tiles);
    }
}
