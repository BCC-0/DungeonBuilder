using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls tile placement and removal in the map editor background layer,
/// fully integrated with SaveableTilemap for persistence.
/// </summary>
public class TileEditorController : MonoBehaviour
{
    [SerializeField]
    private SaveableTilemap saveableTilemap;
    [SerializeField]
    private TileBase selectedTile;

    private Vector3 currentPos;
    private bool primaryHolding;
    private bool secondaryHolding;

    /// <summary>
    /// Gets or sets the currently selected tile for painting.
    /// </summary>
    public TileBase SelectedTile
    {
        get => this.selectedTile;
        set => this.selectedTile = value;
    }

    /// <summary>
    /// Updates the pointer world position.
    /// </summary>
    /// <param name="worldPos">The position of the pointer.</param>
    public void OnPointerMoved(Vector3 worldPos)
    {
        this.currentPos = worldPos;
    }

    /// <summary>
    /// Called on primary button pressed when in the background layer.
    /// Places or erases tiles depending on the current tool.
    /// </summary>
    public void OnPrimaryDown()
    {
        this.primaryHolding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Stops dragging tool if it was.
    /// </summary>
    public void OnPrimaryUp() => this.primaryHolding = false;

    /// <summary>
    /// Called on secondary button pressed when in the background layer.
    /// Erases tiles depending on the current tool.
    /// </summary>
    public void OnSecondaryDown()
    {
        this.secondaryHolding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Stops dragging tool if it was.
    /// </summary>
    public void OnSecondaryUp() => this.secondaryHolding = false;

    private void Update()
    {
        if (this.primaryHolding || this.secondaryHolding)
        {
            this.ApplyTool();
        }
    }

    private void ApplyTool()
    {
        Debug.Log("Background triggered.");
        if (this.saveableTilemap == null || (this.selectedTile == null && this.primaryHolding))
        {
            return;
        }

        Vector3Int cellPos = this.saveableTilemap.Tilemap.WorldToCell(this.currentPos);
        EditorTool tool = MapEditorManager.Instance.CurrentTool;

        switch (tool)
        {
            case EditorTool.Brush:
                if (this.secondaryHolding)
                {
                    // Erase tile
                    this.saveableTilemap.SetTile(cellPos.x, cellPos.y, tileID: null);
                }
                else if (this.primaryHolding)
                {
                    // Place tile
                    string tileID = this.saveableTilemap.TileLibrary.GetIDForTile(this.selectedTile);
                    this.saveableTilemap.SetTile(cellPos.x, cellPos.y, tileID, hasCollision: false);
                }

                break;

            case EditorTool.Eraser:
                if (this.primaryHolding || this.secondaryHolding)
                {
                    this.saveableTilemap.SetTile(cellPos.x, cellPos.y, tileID: null);
                }

                break;
        }
    }
}