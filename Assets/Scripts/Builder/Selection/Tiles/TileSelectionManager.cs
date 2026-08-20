using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Selection manager for tiles.
/// </summary>
public class TileSelectionManager : SelectionManagerBase
{
    /// <summary>
    /// Tilemap this manager selects cells from.
    /// </summary>
    [SerializeField]
    private SaveableTilemap saveableTilemap;

    /// <summary>
    /// Temporary tilemap used to display tiles while they are being moved.
    /// </summary>
    [SerializeField]
    private Tilemap movementPreviewTilemap;

    /// <summary>
    /// Visualizer that highlights the current tile selection.
    /// </summary>
    private TileSelectionVisualizer selectionVisualizer;

    /// <summary>
    /// Original tile data and positions when movement started.
    /// </summary>
    private Dictionary<Vector2Int, TileData> moveStartTiles = new();

    /// <summary>
    /// Cell under the pointer when movement started.
    /// </summary>
    private Vector3Int moveStartPointerCell;

    /// <summary>
    /// Prevents confirming immediately on the same frame movement starts.
    /// </summary>
    private bool startedMovingThisFrame;

    /// <summary>
    /// Prevents restarting the animation of the outline every frame.
    /// </summary>
    private Vector3Int lastPointerPos;

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
            this.saveableTilemap.SetTile(
                tile.x,
                tile.y,
                tileID: null);
        }

        this.ClearSelection();
    }

    /// <inheritdoc/>
    public override void MoveSelected()
    {
        if (this.IsMovingSelected)
        {
            return;
        }

        List<Vector2Int> selectedTiles =
            MapEditorManager.Instance.SelectedTiles;

        if (selectedTiles == null || selectedTiles.Count == 0)
        {
            return;
        }

        this.startedMovingThisFrame = true;
        this.IsMovingSelected = true;

        this.moveStartTiles.Clear();

        this.moveStartPointerCell =
            this.saveableTilemap.Tilemap.WorldToCell(
                this.CurrentPos);

        this.DisableSelectionButtons();
        this.EnableMoveButtons(this.GetTileBoundingRect(selectedTiles));

        foreach (Vector2Int cell in selectedTiles)
        {
            TileData tile = this.saveableTilemap.GetTileData(cell);

            if (tile == null)
            {
                continue;
            }

            this.moveStartTiles[cell] = tile;

            this.saveableTilemap.SetTile(
                cell.x,
                cell.y,
                tileID: null);
        }

        this.UpdateMovementPreview(Vector3Int.zero);
    }

    /// <inheritdoc/>
    public override void ConfirmMovingSelected()
    {
        if (this.startedMovingThisFrame)
        {
            this.startedMovingThisFrame = false;
            return;
        }

        if (!this.IsMovingSelected)
        {
            return;
        }

        Vector3Int currentPointerCell =
            this.saveableTilemap.Tilemap.WorldToCell(
                this.CurrentPos);

        Vector3Int cellDelta =
            currentPointerCell - this.moveStartPointerCell;

        foreach (KeyValuePair<Vector2Int, TileData> entry
                 in this.moveStartTiles)
        {
            Vector3Int destinationCell =
                new Vector3Int(
                    entry.Key.x,
                    entry.Key.y,
                    0) + cellDelta;

            this.saveableTilemap.SetTile(
                destinationCell.x,
                destinationCell.y,
                entry.Value.TileID,
                entry.Value.HasCollision,
                entry.Value.Tag);
        }

        this.movementPreviewTilemap.ClearAllTiles();

        this.IsMovingSelected = false;
        this.moveStartTiles.Clear();

        this.DisableMoveButtons();
        this.ClearSelection();
    }

    /// <inheritdoc/>
    public override void CancelMovingSelected()
    {
        if (!this.IsMovingSelected)
        {
            return;
        }

        foreach (KeyValuePair<Vector2Int, TileData> entry
                in this.moveStartTiles)
        {
            this.saveableTilemap.SetTile(
                entry.Key.x,
                entry.Key.y,
                entry.Value.TileID,
                entry.Value.HasCollision,
                entry.Value.Tag);
        }

        this.movementPreviewTilemap.ClearAllTiles();

        this.IsMovingSelected = false;
        this.moveStartTiles.Clear();

        this.DisableMoveButtons();
        this.ClearSelection();
    }

    /// <inheritdoc/>
    protected override void ContinueMove(Vector2 worldPos)
    {

    }

    /// <summary>
    /// Finds the correct visualizer.
    /// </summary>
    protected override void Awake()
    {
        this.selectionVisualizer =
            FindAnyObjectByType<TileSelectionVisualizer>();

        base.Awake();
    }

    /// <summary>
    /// Selects the tile at the clicked cell, if one exists.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected override void OnClickSelect(Vector2 position)
    {
        Vector2Int cellPos =
            (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(position);

        if (this.saveableTilemap.Tilemap.HasTile(
                new Vector3Int(cellPos.x, cellPos.y, 0)))
        {
            this.SetSelection(
                new List<Vector2Int> { cellPos });
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
        List<Vector2Int> selected = new ();

        Vector2Int min =
            (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(rect.min);

        Vector2Int max =
            (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(rect.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new (x, y);

                if (this.saveableTilemap.Tilemap.HasTile(
                        new Vector3Int(cell.x, cell.y, 0)))
                {
                    selected.Add(cell);
                }
            }
        }

        this.SetSelection(selected);
    }

    /// <summary>
    /// Writes the given selection to <see cref="MapEditorManager"/> and refreshes
    /// the visualizer.
    /// </summary>
    /// <param name="tiles">The tile cells to select.</param>
    private void SetSelection(List<Vector2Int> tiles)
    {
        MapEditorManager.Instance.SelectedTiles = tiles;
        this.selectionVisualizer.Refresh(tiles);

        // While moving, the move buttons own this slot; don't fight them.
        if (this.IsMovingSelected)
        {
            return;
        }

        if (tiles == null || tiles.Count == 0)
        {
            this.DisableSelectionButtons();
        }
        else
        {
            this.EnableSelectionButtons(this.GetTileBoundingRect(tiles));
        }
    }

    /// <summary>
    /// Computes the world-space bounding rect of a set of tile cells.
    /// </summary>
    /// <param name="tiles">The tile cells.</param>
    /// <returns>The bounding rectangle in world space.</returns>
    private Rect GetTileBoundingRect(List<Vector2Int> tiles)
    {
        return GetBoundingRect(
            tiles.Select(t =>
                (Vector2)this.saveableTilemap.Tilemap.GetCellCenterWorld(
                    new Vector3Int(t.x, t.y, 0))));
    }

    /// <summary>
    /// Updates the temporary tile movement preview.
    /// </summary>
    private void Update()
    {
        if (!this.IsMovingSelected)
        {
            return;
        }

        if (this.startedMovingThisFrame)
        {
            this.startedMovingThisFrame = false;
            return;
        }

        Vector3Int currentPointerCell =
            this.saveableTilemap.Tilemap.WorldToCell(
                this.CurrentPos);

        Vector3Int cellDelta =
            currentPointerCell - this.moveStartPointerCell;

        if (currentPointerCell != this.lastPointerPos)
        {
            this.lastPointerPos = currentPointerCell;
            this.UpdateMovementPreview(cellDelta);
        }
    }

    /// <summary>
    /// Updates both the tile preview and the selection outline.
    /// </summary>
    /// <param name="cellDelta">
    /// The movement delta in grid cells.
    /// </param>
    private void UpdateMovementPreview(Vector3Int cellDelta)
    {
        this.movementPreviewTilemap.ClearAllTiles();

        List<Vector2Int> previewSelection = new();

        foreach (KeyValuePair<Vector2Int, TileData> entry
                 in this.moveStartTiles)
        {
            Vector3Int originalCell =
                new (entry.Key.x, entry.Key.y, 0);

            Vector3Int previewCell =
                originalCell + cellDelta;

            TileBase previewTile =
                string.IsNullOrEmpty(entry.Value.TileID)
                    ? null
                    : this.saveableTilemap.TileLibrary.GetTileByID(entry.Value.TileID);

            this.movementPreviewTilemap.SetTile(
                previewCell,
                previewTile);

            previewSelection.Add(
                new Vector2Int(
                    previewCell.x,
                    previewCell.y));
        }

        this.SetSelection(previewSelection);
    }
}