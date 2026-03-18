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
    /// </summary>
    /// <param name="worldPos">The world position of the pointer.</param>
    public void OnPointerMoved(Vector3 worldPos)
    {
        this.currentPos = worldPos;
    }

    /// <summary>
    /// Called on primary button pressed when in the background layer.
    /// Places or erases entities depending on the current tool.
    /// </summary>
    public void OnPrimaryDown()
    {
        // TODO
    }

    /// <summary>
    /// // TODO: Add primary up.
    /// </summary>
    public void OnPrimaryUp()
    {
        //TODO
    }

    private void ApplyTool()
    {
        // TODO
    }
}