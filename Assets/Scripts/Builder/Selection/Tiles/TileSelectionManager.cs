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
    /// Current cell positions of the selected tiles when movement is continued.
    /// </summary>
    private Dictionary<Vector2Int, TileData> extraMoveStartTiles = new();

    /// <summary>
    /// Cell under the pointer when movement started.
    /// </summary>
    private Vector2Int moveStartPointerCell;

    /// <summary>
    /// Prevents confirming immediately on the same frame movement starts.
    /// </summary>
    private bool startedMovingThisFrame;

    /// <summary>
    /// Whether the current movement was started by a paste. Cancelling that
    /// movement must also remove the pasted tiles again.
    /// </summary>
    private bool pasteTransactionOpen;

    /// <summary>
    /// Tiles shown while a resize is previewed, by cell.
    /// </summary>
    private Dictionary<Vector2Int, TileState> resizePreviewTiles = new ();

    /// <summary>
    /// Cells that are emptied when the previewed resize is confirmed.
    /// </summary>
    private List<Vector2Int> resizeClearCells = new ();

    /// <summary>
    /// Cells of the selection before the resize, so the selection can be restored
    /// when the preview is cancelled.
    /// </summary>
    private List<Vector2Int> resizeSourceSelection = new ();

    /// <inheritdoc/>
    protected override bool HasSelection =>
        MapEditorManager.Instance.SelectedTiles != null &&
        MapEditorManager.Instance.SelectedTiles.Count > 0;

    /// <summary>
    /// Copies the selected tiles to the clipboard, using the lowest selected
    /// cell as the clipboard origin.
    /// </summary>
    public override void CopySelected()
    {
        List<Vector2Int> selected = MapEditorManager.Instance.SelectedTiles;

        if (selected == null || selected.Count == 0)
        {
            return;
        }

        Dictionary<Vector2Int, TileState> cells = new ();
        Vector2Int origin = selected[0];

        foreach (Vector2Int cell in selected)
        {
            TileState state =
                TileState.FromData(this.saveableTilemap.GetTileData(cell));

            if (state.TileID == null)
            {
                continue;
            }

            cells[cell] = state;
            origin = Vector2Int.Min(origin, cell);
        }

        if (cells.Count == 0)
        {
            return;
        }

        Dictionary<Vector2Int, TileState> normalized = new ();

        foreach (KeyValuePair<Vector2Int, TileState> entry in cells)
        {
            normalized[entry.Key - origin] = entry.Value;
        }

        BuilderClipboard.StoreTiles(new ClipboardTiles(normalized));

        MapEditorManager.Instance.ShowClipboardMessage(
            $"Copied {cells.Count} {(cells.Count == 1 ? "tile" : "tiles")}");

        this.DisableSelectionButtons();
    }

    /// <summary>
    /// Copies the selected tiles and deletes them as one undo step.
    /// </summary>
    public override void CutSelected()
    {
        MapEditorManager manager = MapEditorManager.Instance;

        manager.BeginTransaction();
        this.CopySelected();
        this.DeleteSelected();
        manager.EndTransaction();
    }

    /// <summary>
    /// Previews the clipboard tiles at the pointer and starts moving them, so the
    /// position can be chosen before the paste is confirmed. The map is not changed
    /// until then, so the tiles already there stay where they are. Confirming places
    /// the tiles, cancelling leaves the map untouched.
    /// </summary>
    public override void PasteAtPointer()
    {
        if (this.IsMovementMode)
        {
            return;
        }

        ClipboardTiles clipboard = BuilderClipboard.Tiles;

        if (clipboard == null || clipboard.Cells.Count == 0)
        {
            return;
        }

        Vector2Int targetCell =
            (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(this.CurrentPos);

        Dictionary<Vector2Int, TileData> previewTiles = new ();
        List<Vector2Int> pasted = new ();

        foreach (KeyValuePair<Vector2Int, TileState> entry in clipboard.Cells)
        {
            Vector2Int cell = targetCell + entry.Key;

            previewTiles[cell] = new TileData
            {
                TileID = entry.Value.TileID,
                HasCollision = entry.Value.HasCollision,
                Tag = entry.Value.Tag,
            };

            pasted.Add(cell);
        }

        this.pasteTransactionOpen = true;
        MapEditorManager.Instance.BeginTransaction();

        this.BeginPasteMovement(pasted, previewTiles);

        MapEditorManager.Instance.ShowClipboardMessage(
            $"Pasting {pasted.Count} {(pasted.Count == 1 ? "tile" : "tiles")}");
    }

    /// <inheritdoc/>
    public override void DeleteSelected()
    {
        TileStrokeAction action = new TileStrokeAction(this.ApplyTileState);

        foreach (Vector2Int tile in MapEditorManager.Instance.SelectedTiles)
        {
            TileState before =
                TileState.FromData(this.saveableTilemap.GetTileData(tile));

            if (before.Equals(TileState.Empty))
            {
                continue;
            }

            this.saveableTilemap.SetTile(tile.x, tile.y, tileID: null);
            action.Record(tile, before, TileState.Empty);
        }

        if (action.HasChanges)
        {
            MapEditorManager.Instance.RegisterAction(action);
        }

        this.ClearSelection();
    }

    /// <inheritdoc/>
    public override void MoveSelected()
    {
        if (this.IsMovementMode)
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
        this.IsMovementMode = true;
        this.IsMoving = true;

        this.moveStartTiles.Clear();

        this.moveStartPointerCell = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(this.CurrentPos);

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

            this.saveableTilemap.SetTile(cell.x, cell.y, tileID: null);
        }

        this.extraMoveStartTiles = new Dictionary<Vector2Int, TileData>(this.moveStartTiles);

        this.UpdateMovementPreview();
    }

    /// <inheritdoc/>
    public override void ConfirmMovingSelected()
    {
        if (this.startedMovingThisFrame)
        {
            this.startedMovingThisFrame = false;
            return;
        }

        if (!this.IsMovementMode)
        {
            return;
        }

        List<Vector2Int> movedTiles = new (this.extraMoveStartTiles.Keys);

        TileStrokeAction action = new TileStrokeAction(this.ApplyTileState);

        // The source cells were emptied when the move started.
        foreach (KeyValuePair<Vector2Int, TileData> entry in this.moveStartTiles)
        {
            action.Record(
                entry.Key,
                TileState.FromData(entry.Value),
                TileState.Empty);
        }

        foreach (KeyValuePair<Vector2Int, TileData> entry in this.extraMoveStartTiles)
        {
            action.Record(
                entry.Key,
                TileState.FromData(this.saveableTilemap.GetTileData(entry.Key)),
                TileState.FromData(entry.Value));

            this.saveableTilemap.SetTile(
                entry.Key.x,
                entry.Key.y,
                entry.Value.TileID,
                entry.Value.HasCollision,
                entry.Value.Tag);
        }

        action.SetSelection(this.moveStartTiles.Keys, movedTiles);

        if (action.HasChanges)
        {
            MapEditorManager.Instance.RegisterAction(action);
        }

        this.movementPreviewTilemap.ClearAllTiles();

        this.IsMovementMode = false;
        this.IsMoving = false;
        this.moveStartTiles.Clear();
        this.extraMoveStartTiles.Clear();

        this.DisableMoveButtons();
        this.SetSelection(movedTiles);
        this.CommitPasteTransaction();
    }

    /// <inheritdoc/>
    public override void CancelMovingSelected()
    {
        if (!this.IsMovementMode)
        {
            return;
        }

        foreach (KeyValuePair<Vector2Int, TileData> entry in this.moveStartTiles)
        {
            this.saveableTilemap.SetTile(
                entry.Key.x,
                entry.Key.y,
                entry.Value.TileID,
                entry.Value.HasCollision,
                entry.Value.Tag);
        }

        this.movementPreviewTilemap.ClearAllTiles();

        this.IsMovementMode = false;
        this.IsMoving = false;
        this.moveStartTiles.Clear();
        this.extraMoveStartTiles.Clear();

        this.DisableMoveButtons();
        this.ClearSelection();
        this.CancelPasteTransaction();
    }

    /// <summary>
    /// Refreshses the selection of tiles.
    /// </summary>
    public void RefreshSelection()
    {
        this.SetSelection(
            MapEditorManager.Instance.SelectedTiles
                .Where(t => this.saveableTilemap.GetTileData(t) != null)
                .ToList());
    }

    /// <inheritdoc/>
    protected override void ClearSelection()
    {
        this.SetSelection(new List<Vector2Int>());
    }

    /// <inheritdoc/>
    protected override void ContinueMove(Vector2 worldPos)
    {
        if (!this.IsMovementMode)
        {
            return;
        }

        this.moveStartPointerCell = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(worldPos);

        this.CurrentPos = worldPos;
        this.startedMovingThisFrame = true;
        this.IsMoving = true;
    }

    /// <summary>
    /// Gets the current rect of the selection.
    /// </summary>
    /// <returns>A rect containing the selected tiles.</returns>
    protected override Rect GetCurrentSelectionBounds()
    {
        List<Vector2Int> tiles = MapEditorManager.Instance.SelectedTiles;

        return this.GetTileBoundingRect(tiles);
    }

    /// <summary>
    /// Gets the grid cells the selected tiles cover.
    /// </summary>
    /// <returns>The selected cells.</returns>
    protected override RectInt GetSelectedCellRect()
    {
        List<Vector2Int> tiles = MapEditorManager.Instance.SelectedTiles;

        if (tiles == null || tiles.Count == 0)
        {
            return default(RectInt);
        }

        Vector2Int min = tiles[0];
        Vector2Int max = tiles[0];

        foreach (Vector2Int tile in tiles)
        {
            min = Vector2Int.Min(min, tile);
            max = Vector2Int.Max(max, tile);
        }

        return new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);
    }

    /// <summary>
    /// Gets the world rectangle covered by a block of cells.
    /// </summary>
    /// <param name="cells">The cells to cover.</param>
    /// <returns>The world rectangle of the cells.</returns>
    protected override Rect GetCellsWorldRect(RectInt cells)
    {
        if (cells.width <= 0 || cells.height <= 0 || this.saveableTilemap == null)
        {
            return default(Rect);
        }

        Tilemap tilemap = this.saveableTilemap.Tilemap;

        if (tilemap == null)
        {
            return default(Rect);
        }

        // One selected cell is one cell of the grid, so the box is the footprint
        // of the selected cells. The tile art is not measured: a tile sprite can
        // report twice the size it is drawn at, which made the box twice as big.
        Vector3 min = tilemap.CellToWorld(
            new Vector3Int(cells.xMin, cells.yMin, 0));

        Vector3 max = tilemap.CellToWorld(
            new Vector3Int(cells.xMax, cells.yMax, 0));

        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    /// <summary>
    /// Draws the resized tiles on the movement preview tilemap. The real tilemap
    /// is not touched, so the tiles that are already there stay visible until the
    /// resize is confirmed.
    /// </summary>
    /// <param name="source">The cells of the current selection.</param>
    /// <param name="target">The cells the selection is resized to.</param>
    protected override void BeginResizePreview(RectInt source, RectInt target)
    {
        this.StopResizePreview();

        this.resizeSourceSelection = new List<Vector2Int>(
            MapEditorManager.Instance.SelectedTiles);

        Dictionary<Vector2Int, TileState> block = this.SnapshotCells(source);

        List<Vector2Int> previewSelection = new ();

        for (int x = target.xMin; x < target.xMax; x++)
        {
            for (int y = target.yMin; y < target.yMax; y++)
            {
                int sourceX = SelectionResize.SourceIndexForTarget(
                    x, target.xMin, target.width, source.xMin, source.width);

                int sourceY = SelectionResize.SourceIndexForTarget(
                    y, target.yMin, target.height, source.yMin, source.height);

                TileState state = block[new Vector2Int(sourceX, sourceY)];
                Vector2Int cell = new (x, y);

                this.resizePreviewTiles[cell] = state;

                if (state.TileID == null)
                {
                    continue;
                }

                previewSelection.Add(cell);

                this.movementPreviewTilemap.SetTile(
                    new Vector3Int(x, y, 0),
                    TileLibrary.GetTileByIDGlobal(state.TileID));
            }
        }

        foreach (Vector2Int cell in this.resizeSourceSelection)
        {
            if (!target.Contains(cell))
            {
                this.resizeClearCells.Add(cell);
            }
        }

        this.SetSelection(previewSelection);
    }

    /// <summary>
    /// Writes the previewed block to the real tilemap as one undo step.
    /// </summary>
    protected override void ApplyResizePreview()
    {
        TileStrokeAction action = new (this.ApplyTileState);

        List<Vector2Int> selected = new ();

        foreach (KeyValuePair<Vector2Int, TileState> entry in this.resizePreviewTiles)
        {
            TileState before = TileState.FromData(
                this.saveableTilemap.GetTileData(entry.Key));

            if (!before.Equals(entry.Value))
            {
                action.Record(entry.Key, before, entry.Value);
                this.ApplyTileState(entry.Key, entry.Value);
            }

            if (entry.Value.TileID != null)
            {
                selected.Add(entry.Key);
            }
        }

        foreach (Vector2Int cell in this.resizeClearCells)
        {
            TileState before = TileState.FromData(
                this.saveableTilemap.GetTileData(cell));

            if (before.Equals(TileState.Empty))
            {
                continue;
            }

            action.Record(cell, before, TileState.Empty);
            this.ApplyTileState(cell, TileState.Empty);
        }

        action.SetSelection(this.resizeSourceSelection, selected);

        this.StopResizePreview();

        if (action.HasChanges)
        {
            MapEditorManager.Instance.RegisterAction(action);
        }

        this.SetSelection(selected);
    }

    /// <summary>
    /// Removes the preview. The real tilemap was never changed, so nothing has
    /// to be restored.
    /// </summary>
    protected override void CancelResizePreview()
    {
        List<Vector2Int> restore = new (this.resizeSourceSelection);

        this.StopResizePreview();

        if (restore.Count > 0)
        {
            this.SetSelection(restore);
        }
    }

    /// <inheritdoc/>
    protected override bool HasSelectableAt(Vector2 position)
    {
        Vector2Int cellPos = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(position);

        return this.saveableTilemap.Tilemap.HasTile(new Vector3Int(cellPos.x, cellPos.y, 0));
    }

    /// <summary>
    /// Selects the tile at the clicked cell, if one exists.
    /// A plain click replaces the selection, and clicking the only selected tile
    /// clears it. Ctrl toggles the clicked tile and Shift adds it.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    /// <param name="selectionMode">How the pick combines with the current selection.</param>
    protected override void OnClickSelect(Vector2 position, SelectionMode selectionMode)
    {
        Vector2Int cellPos = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(position);

        bool hasTile =
            this.saveableTilemap.Tilemap.HasTile(new Vector3Int(cellPos.x, cellPos.y, 0));

        List<Vector2Int> current = MapEditorManager.Instance.SelectedTiles;

        if (selectionMode != SelectionMode.Replace)
        {
            if (!hasTile)
            {
                return;
            }

            this.SetSelection(
                selectionMode == SelectionMode.Toggle
                    ? this.ToggleInSelection(current, cellPos)
                    : this.AddToSelection(current, new List<Vector2Int> { cellPos }));

            return;
        }

        if (!hasTile)
        {
            this.ClearSelection();
            return;
        }

        if (current != null &&
            current.Count == 1 &&
            current[0] == cellPos)
        {
            this.ClearSelection();
            return;
        }

        this.SetSelection(new List<Vector2Int> { cellPos });
    }

    /// <summary>
    /// Selects every tile whose cell falls within the dragged rectangle.
    /// With Ctrl the box contents are toggled: tiles that were already selected
    /// are dropped and the others are added. Shift only adds.
    /// </summary>
    /// <param name="rect">The world-space rectangle of the drag.</param>
    /// <param name="selectionMode">How the box contents combine with the current selection.</param>
    protected override void OnBoxSelect(Rect rect, SelectionMode selectionMode)
    {
        List<Vector2Int> selected = new();

        Vector2Int min = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(rect.min);

        Vector2Int max = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(rect.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (selected.Count >= this.MaxSelectedCells)
                {
                    break;
                }

                Vector2Int cell = new(x, y);

                if (this.saveableTilemap.Tilemap.HasTile(new Vector3Int(cell.x, cell.y, 0)))
                {
                    selected.Add(cell);
                }
            }

            if (selected.Count >= this.MaxSelectedCells)
            {
                break;
            }
        }

        if (selectionMode != SelectionMode.Replace)
        {
            selected = selectionMode == SelectionMode.Toggle
                ? this.ToggleInSelection(
                    MapEditorManager.Instance.SelectedTiles,
                    selected)
                : this.AddToSelection(
                    MapEditorManager.Instance.SelectedTiles,
                    selected);
        }

        this.SetSelection(selected);
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
    /// Writes the given selection to <see cref="MapEditorManager"/> and refreshes
    /// the visualizer.
    /// </summary>
    /// <param name="tiles">The tile cells to select.</param>
    private void SetSelection(List<Vector2Int> tiles)
    {
        MapEditorManager.Instance.SelectedTiles = tiles;
        this.selectionVisualizer.Refresh(tiles);

        if (this.IsMovementMode)
        {
            return;
        }

        this.DisableSelectionButtons();
    }

    /// <summary>
    /// Computes the world-space bounding rect of a set of tile cells, covering the full area of each cell
    /// at the position where the tiles are actually drawn.
    /// </summary>
    /// <param name="tiles">The tile cells.</param>
    /// <returns>The bounding rectangle in world space.</returns>
    private Rect GetTileBoundingRect(List<Vector2Int> tiles)
    {
        Tilemap tilemap = this.saveableTilemap.Tilemap;

        Vector2 cellStep =
            tilemap.GetCellCenterWorld(new Vector3Int(1, 1, 0)) -
            tilemap.GetCellCenterWorld(Vector3Int.zero);

        Vector2 halfCell = new Vector2(
            Mathf.Abs(cellStep.x),
            Mathf.Abs(cellStep.y)) * 0.5f;

        return this.GetBoundingRect(tiles.SelectMany(t =>
        {
            Vector2 center = tilemap.GetCellCenterWorld(new Vector3Int(t.x, t.y, 0));

            return new[]
            {
                center - halfCell,
                center + halfCell,
            };
        }));
    }

    /// <summary>
    /// Updates the temporary tile movement preview.
    /// </summary>
    private void Update()
    {
        if (!this.IsMovementMode || !this.IsMoving)
        {
            return;
        }

        if (this.startedMovingThisFrame)
        {
            this.startedMovingThisFrame = false;
            return;
        }

        Vector2Int currentPointerCell = (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(this.CurrentPos);

        Vector2Int cellDelta = currentPointerCell - this.moveStartPointerCell;

        if (cellDelta == Vector2Int.zero)
        {
            return;
        }

        Vector2Int cellDelta2D = new(cellDelta.x, cellDelta.y);

        Dictionary<Vector2Int, TileData> shifted = new();

        foreach (KeyValuePair<Vector2Int, TileData> entry in this.extraMoveStartTiles)
        {
            shifted[entry.Key + cellDelta2D] = entry.Value;
        }

        this.extraMoveStartTiles = shifted;
        this.moveStartPointerCell = currentPointerCell;

        this.UpdateMovementPreview();
    }

    /// <summary>
    /// Enters movement mode with tiles that come from the clipboard instead of the
    /// map. Nothing is written to the tilemap here, so the tiles that are already
    /// there stay visible while the pasted position is chosen.
    /// </summary>
    /// <param name="tiles">The cells the pasted tiles cover.</param>
    /// <param name="previewTiles">The pasted tiles, by cell.</param>
    private void BeginPasteMovement(
        List<Vector2Int> tiles,
        Dictionary<Vector2Int, TileData> previewTiles)
    {
        this.startedMovingThisFrame = true;
        this.IsMovementMode = true;
        this.IsMoving = true;

        // A paste has no tiles in the map to lift, so the map is left alone.
        this.moveStartTiles.Clear();

        this.moveStartPointerCell =
            (Vector2Int)this.saveableTilemap.Tilemap.WorldToCell(this.CurrentPos);

        this.extraMoveStartTiles = previewTiles;

        this.DisableSelectionButtons();
        this.EnableMoveButtons(this.GetTileBoundingRect(tiles));

        this.UpdateMovementPreview();
    }

    /// <summary>
    /// Draws the tile movement preview and selection outline.
    /// </summary>
    private void UpdateMovementPreview()
    {
        this.movementPreviewTilemap.ClearAllTiles();

        List<Vector2Int> previewSelection = new ();

        foreach (KeyValuePair<Vector2Int, TileData> entry in this.extraMoveStartTiles)
        {
            Vector3Int previewCell = new (entry.Key.x, entry.Key.y, 0);

            TileBase previewTile =
                string.IsNullOrEmpty(entry.Value.TileID)
                    ? null
                    : TileLibrary.GetTileByIDGlobal(entry.Value.TileID);

            this.movementPreviewTilemap.SetTile(previewCell, previewTile);

            previewSelection.Add(entry.Key);
        }

        this.SetSelection(previewSelection);
    }

    /// <summary>
    /// Commits the transaction a paste opened, so the pasted tiles and their
    /// placement become one undo step.
    /// </summary>
    private void CommitPasteTransaction()
    {
        if (!this.pasteTransactionOpen)
        {
            return;
        }

        this.pasteTransactionOpen = false;
        MapEditorManager.Instance.EndTransaction();
    }

    /// <summary>
    /// Cancels the transaction a paste opened, removing the pasted tiles again.
    /// </summary>
    private void CancelPasteTransaction()
    {
        if (!this.pasteTransactionOpen)
        {
            return;
        }

        this.pasteTransactionOpen = false;
        MapEditorManager.Instance.CancelTransaction();

        this.ClearSelection();
    }

    /// <summary>
    /// Gets the size of one cell of the tilemap in world units.
    /// </summary>
    /// <param name="tilemap">The tilemap.</param>
    /// <returns>The size of a cell.</returns>
    /// <summary>
    /// Removes the preview tiles and the state that belongs to them.
    /// </summary>
    private void StopResizePreview()
    {
        this.movementPreviewTilemap.ClearAllTiles();

        this.resizePreviewTiles.Clear();
        this.resizeClearCells.Clear();
        this.resizeSourceSelection.Clear();
    }

    /// <summary>
    /// Copies the tile state of every cell of a block, so the preview is built
    /// from the tiles as they were before it started.
    /// </summary>
    /// <param name="cells">The cells to copy.</param>
    /// <returns>The state of each cell.</returns>
    private Dictionary<Vector2Int, TileState> SnapshotCells(RectInt cells)
    {
        Dictionary<Vector2Int, TileState> states = new ();

        for (int x = cells.xMin; x < cells.xMax; x++)
        {
            for (int y = cells.yMin; y < cells.yMax; y++)
            {
                Vector2Int cell = new (x, y);

                states[cell] = TileState.FromData(
                    this.saveableTilemap.GetTileData(cell));
            }
        }

        return states;
    }

    private void ApplyTileState(Vector2Int cell, TileState state)
    {
        this.saveableTilemap.SetTile(
            cell.x,
            cell.y,
            state.TileID,
            state.HasCollision,
            state.Tag);
    }
}
