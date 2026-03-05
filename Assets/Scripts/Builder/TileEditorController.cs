using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// The tile editor controller controls the tiles (background) in the map editor.
/// </summary>
public class TileEditorController : MonoBehaviour
{
    private Tilemap tilemap;
    private TileBase selectedTile;
    private Vector3 currentPos;
    private bool holding;

    /// <summary>
    /// Gets or sets the tilemap that is being edited by this controller.
    /// </summary>
    public Tilemap Tilemap
    {
        get { return this.tilemap; }
        set { this.tilemap = value; }
    }

    /// <summary>
    /// Gets or sets the currently selected tile.
    /// </summary>
    public TileBase SelectedTile
    {
        get { return this.selectedTile; }
        set { this.selectedTile = value; }
    }

    private void OnEnable()
    {
        EditorEventBus.OnPointerMoved += this.UpdatePointer;
        EditorEventBus.OnPrimaryDown += this.StartAction;
        EditorEventBus.OnPrimaryUp += this.StopAction;
    }

    private void OnDisable()
    {
        EditorEventBus.OnPointerMoved -= this.UpdatePointer;
        EditorEventBus.OnPrimaryDown -= this.StartAction;
        EditorEventBus.OnPrimaryUp -= this.StopAction;
    }

    private void UpdatePointer(Vector3 pos)
    {
        this.currentPos = pos;

        if (this.holding)
        {
            this.ApplyTool();
        }
    }

    private void StartAction()
    {
        if (MapEditorManager.Instance.CurrentLayer != EditLayer.Background)
        {
            return;
        }

        this.holding = true;
        this.ApplyTool();
    }

    private void StopAction() => this.holding = false;

    private void ApplyTool()
    {
        var tool = MapEditorManager.Instance.CurrentTool;
        Vector3Int cell = this.Tilemap.WorldToCell(this.currentPos);

        if (tool == EditorTool.Brush)
        {
            this.Tilemap.SetTile(cell, this.SelectedTile);
        }

        if (tool == EditorTool.Eraser)
        {
            this.Tilemap.SetTile(cell, null);
        }
    }
}