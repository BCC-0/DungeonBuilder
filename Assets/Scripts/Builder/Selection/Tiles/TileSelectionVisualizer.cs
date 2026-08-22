using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Renders an outline around a collection of selected Tilemap cells.
///
/// Each direction has its own overlay Tilemap, allowing multiple outline
/// segments to occupy the same cell.
/// </summary>
public class TileSelectionVisualizer : MonoBehaviour
{
    private readonly HashSet<Vector2Int> selectedCells = new ();

    [Header("Overlay Tilemaps")]
    [SerializeField]
    private Tilemap topOverlay;

    [SerializeField]
    private Tilemap bottomOverlay;

    [SerializeField]
    private Tilemap leftOverlay;

    [SerializeField]
    private Tilemap rightOverlay;

    [Header("Outline Tiles")]
    [SerializeField]
    private TileBase topTile;

    [SerializeField]
    private TileBase bottomTile;

    [SerializeField]
    private TileBase leftTile;

    [SerializeField]
    private TileBase rightTile;

    /// <summary>
    /// Rebuilds the selection outline for the supplied cells.
    /// </summary>
    /// <param name="cells">The currently selected tiles.</param>
    public void Refresh(List<Vector2Int> cells)
    {
        this.Clear();

        foreach (Vector2Int cell in cells)
        {
            this.selectedCells.Add(cell);
        }

        foreach (Vector2Int cell in this.selectedCells)
        {
            this.UpdateCell(cell);
        }
    }

    /// <summary>
    /// Removes the entire selection outline.
    /// </summary>
    public void Clear()
    {
        this.topOverlay.ClearAllTiles();
        this.bottomOverlay.ClearAllTiles();
        this.leftOverlay.ClearAllTiles();
        this.rightOverlay.ClearAllTiles();

        this.selectedCells.Clear();
    }

    /// <summary>
    /// Determines which sides of a cell are exposed and renders the
    /// corresponding outline segments.
    /// </summary>
    private void UpdateCell(Vector2Int cell)
    {
        Vector2Int above = cell + Vector2Int.up;
        Vector2Int below = cell + Vector2Int.down;
        Vector2Int left = cell + Vector2Int.left;
        Vector2Int right = cell + Vector2Int.right;

        if (!this.selectedCells.Contains(above))
        {
            this.topOverlay.SetTile(new (cell.x, cell.y, 0), this.topTile);
        }

        if (!this.selectedCells.Contains(below))
        {
            this.bottomOverlay.SetTile(new (cell.x, cell.y, 0), this.bottomTile);
        }

        if (!this.selectedCells.Contains(left))
        {
            this.leftOverlay.SetTile(new (cell.x, cell.y, 0), this.leftTile);
        }

        if (!this.selectedCells.Contains(right))
        {
            this.rightOverlay.SetTile(new (cell.x, cell.y, 0), this.rightTile);
        }
    }
}