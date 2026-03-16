using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls tile placement and removal in the map editor background layer.
/// Works with the Unity Events system (no subscriptions needed).
/// </summary>
public class TileEditorController : MonoBehaviour
{
    private Tilemap tilemap;
    private TileBase selectedTile;
    private Vector3 currentPos;
    private bool holding;

    /// <summary>
    /// Gets or sets the Tilemap being edited.
    /// </summary>
    public Tilemap Tilemap
    {
        get => this.tilemap;
        set => this.tilemap = value;
    }

    /// <summary>
    /// Gets or sets the currently selected TileBase for painting.
    /// </summary>
    public TileBase SelectedTile
    {
        get => this.selectedTile;
        set => this.selectedTile = value;
    }

    /// <summary>
    /// Updates the current pointer position (screen-to-world) for editing tiles.
    /// Hook to PlayerInput Point action.
    /// </summary>
    /// <param name="worldPos">The world position of the pointer.</param>
    public void OnPointerMoved(Vector3 worldPos)
    {
        this.currentPos = worldPos;

        if (this.holding)
        {
            this.ApplyTool();
        }
    }

    /// <summary>
    /// Called when the primary button/touch is pressed.
    /// Starts the brush or eraser action.
    /// Hook to PlayerInput Primary action started event.
    /// </summary>
    public void OnPrimaryDown()
    {
        if (MapEditorManager.Instance.CurrentLayer != EditLayer.Background)
        {
            return;
        }

        this.holding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Called when the primary button/touch is released.
    /// Stops the brush or eraser action.
    /// Hook to PlayerInput Primary action canceled event.
    /// </summary>
    public void OnPrimaryUp()
    {
        this.holding = false;
    }

    private void ApplyTool()
    {
        Vector3Int cell = this.tilemap.WorldToCell(this.currentPos);
        var tool = MapEditorManager.Instance.CurrentTool;

        if (tool == EditorTool.Brush && this.SelectedTile != null)
        {
            this.tilemap.SetTile(cell, this.SelectedTile);
        }

        if (tool == EditorTool.Eraser)
        {
            this.tilemap.SetTile(cell, null);
        }
    }
}