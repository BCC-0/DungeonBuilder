using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls tile placement and removal in the background layer.
/// </summary>
public class TileEditorController : EditorControllerBase
{
    [SerializeField]
    private TileBase selectedTile;

    private TileStrokeAction stroke;
    private InventoryController inventory;

    /// <summary>
    /// Gets or sets the currently selected tile.
    /// </summary>
    public TileBase SelectedTile
    {
        get => this.selectedTile;
        set => this.selectedTile = value;
    }

    /// <summary>
    /// Executes tile editing logic based on the resolved editor action.
    /// </summary>
    /// <param name="tool">The active editor tool.</param>
    protected override void OnApplyTool(EditorTool tool)
    {
        if (this.SaveableTilemap == null)
        {
            return;
        }

        EditorAction action = this.GetAction(tool);
        Vector3Int cellPos = this.SaveableTilemap.Tilemap.WorldToCell(this.CurrentPos);
        Vector2Int cell = new Vector2Int(cellPos.x, cellPos.y);

        switch (action)
        {
            case EditorAction.Paint:
                if (this.selectedTile == null)
                {
                    return;
                }

                string tileID = TileLibrary.GetIDForTileGlobal(this.selectedTile);
                this.ChangeCell(cell, new TileState(tileID, false, null));
                break;

            case EditorAction.Erase:
                this.ChangeCell(cell, TileState.Empty);
                break;
        }
    }

    /// <summary>
    /// Starts recording a new stroke.
    /// </summary>
    protected override void OnStrokeBegin()
    {
        SaveableTilemap tilemap = this.SaveableTilemap;

        this.stroke = new TileStrokeAction(
            (cell, state) => tilemap.SetTile(
                cell.x,
                cell.y,
                state.TileID,
                state.HasCollision,
                state.Tag));
    }

    /// <summary>
    /// Registers the finished stroke as one undo step.
    /// </summary>
    protected override void OnStrokeEnd()
    {
        TileStrokeAction finished = this.stroke;
        this.stroke = null;

        if (finished != null &&
            finished.HasChanges &&
            MapEditorManager.Instance != null)
        {
            MapEditorManager.Instance.RegisterAction(finished);
        }
    }

    /// <summary>
    /// Picks the tile under the pointer into the toolbar.
    /// </summary>
    protected override void OnPick()
    {
        if (this.SaveableTilemap == null || this.inventory == null)
        {
            return;
        }

        Vector3Int cellPos = this.SaveableTilemap.Tilemap.WorldToCell(this.CurrentPos);

        TileState state = TileState.FromData(
            this.SaveableTilemap.GetTileData(new Vector2Int(cellPos.x, cellPos.y)));

        if (string.IsNullOrEmpty(state.TileID))
        {
            return;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(this.CurrentPos);

        this.inventory.PickTile(state.TileID, screenPos);
    }

    private void Start()
    {
        this.SelectionManager = FindAnyObjectByType<TileSelectionManager>();
        this.inventory = FindAnyObjectByType<InventoryController>();
    }

    /// <summary>
    /// Changes one cell and records it in the current stroke.
    /// Cells that already have the wanted state are left alone.
    /// </summary>
    /// <param name="cell">The cell to change.</param>
    /// <param name="after">The wanted state.</param>
    private void ChangeCell(Vector2Int cell, TileState after)
    {
        TileState before =
            TileState.FromData(this.SaveableTilemap.GetTileData(cell));

        if (before.Equals(after))
        {
            return;
        }

        this.SaveableTilemap.SetTile(
            cell.x,
            cell.y,
            after.TileID,
            after.HasCollision,
            after.Tag);

        this.stroke?.Record(cell, before, after);
    }
}
