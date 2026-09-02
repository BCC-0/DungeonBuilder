using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls tile placement and removal in the background layer.
/// </summary>
public class TileEditorController : EditorControllerBase
{
    [SerializeField]
    private TileBase selectedTile;

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

        switch (action)
        {
            case EditorAction.Paint:
                if (this.selectedTile == null)
                {
                    return;
                }

                string tileID = TileLibrary.GetIDForTileGlobal(this.selectedTile);
                this.SaveableTilemap.SetTile(cellPos.x, cellPos.y, tileID, hasCollision: false);
                break;

            case EditorAction.Erase:
                this.SaveableTilemap.SetTile(cellPos.x, cellPos.y, tileID: null);
                break;
        }
    }

    /// <summary>
    /// Sets the correct selection manager.
    /// </summary>
    private void Start()
    {
        this.SelectionManager = FindAnyObjectByType<TileSelectionManager>();
    }
}