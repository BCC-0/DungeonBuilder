using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls tile placement and removal in the background layer.
/// </summary>
public class TileEditorController : EditorControllerBase
{
    [SerializeField]
    private SaveableTilemap saveableTilemap;

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
        if (this.saveableTilemap == null)
        {
            return;
        }

        EditorAction action = this.GetAction(tool);
        Vector3Int cellPos = this.saveableTilemap.Tilemap.WorldToCell(this.CurrentPos);

        switch (action)
        {
            case EditorAction.Paint:
                if (this.selectedTile == null)
                {
                    return;
                }

                string tileID = this.saveableTilemap.TileLibrary.GetIDForTile(this.selectedTile);
                this.saveableTilemap.SetTile(cellPos.x, cellPos.y, tileID, hasCollision: false);
                break;

            case EditorAction.Erase:
                this.saveableTilemap.SetTile(cellPos.x, cellPos.y, tileID: null);
                break;
        }
    }

    /// <summary>
    /// Called when the player clicks to select.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected override void OnClickSelect(Vector3 position)
    {
        if (this.saveableTilemap == null)
        {
            return;
        }

        Vector3Int cellPos = this.saveableTilemap.Tilemap.WorldToCell(position);

        if (!this.saveableTilemap.Tilemap.HasTile(cellPos))
        {
            MapEditorManager.Instance.SelectedTiles.Clear();
            return;
        }

        var tileSelection = new List<Vector3Int>
        {
            cellPos,
        };

        MapEditorManager.Instance.SelectedTiles = tileSelection;
    }

    /// <summary>
    /// Called when the player drags to select.
    /// </summary>
    /// <param name="start">The start position of the drag selection.</param>
    /// <param name="end">The end position of the drag selection.</param>
    protected override void OnBoxSelect(Vector3 start, Vector3 end)
    {
        if (this.saveableTilemap == null)
        {
            return;
        }

        Rect rect = this.GetWorldRect(start, end);

        Vector3Int min = this.saveableTilemap.Tilemap.WorldToCell(rect.min);
        Vector3Int max = this.saveableTilemap.Tilemap.WorldToCell(rect.max);

        var selected = new List<Vector3Int>();

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

        MapEditorManager.Instance.SelectedTiles = selected;
    }
}