using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls tile placement and removal in the map editor background layer.
/// Works with SaveableTilemap for persistence.
/// </summary>
public class TileEditorController : EditorControllerBase
{
    [SerializeField]
    private SaveableTilemap saveableTilemap;

    [SerializeField]
    private TileBase selectedTile;

    /// <summary>
    /// Gets or sets the currently selected tile for painting.
    /// </summary>
    public TileBase SelectedTile
    {
        get => this.selectedTile;
        set => this.selectedTile = value;
    }

    /// <summary>
    /// Executes tile editing logic based on the active tool.
    /// </summary>
    /// <param name="tool">The current editor tool.</param>
    protected override void OnApplyTool(EditorTool tool)
    {
        if (this.saveableTilemap == null || (this.selectedTile == null && this.PrimaryHolding))
        {
            return;
        }

        Vector3Int cellPos = this.saveableTilemap.Tilemap.WorldToCell(this.CurrentPos);

        switch (tool)
        {
            case EditorTool.Brush:
                if (this.SecondaryHolding)
                {
                    // Erase tile
                    this.saveableTilemap.SetTile(cellPos.x, cellPos.y, tileID: null);
                }
                else if (this.PrimaryHolding)
                {
                    // Place tile
                    string tileID = this.saveableTilemap.TileLibrary.GetIDForTile(this.selectedTile);
                    this.saveableTilemap.SetTile(cellPos.x, cellPos.y, tileID, hasCollision: false);
                }

                break;

            case EditorTool.Eraser:
                if (this.PrimaryHolding || this.SecondaryHolding)
                {
                    this.saveableTilemap.SetTile(cellPos.x, cellPos.y, tileID: null);
                }

                break;
        }
    }
}