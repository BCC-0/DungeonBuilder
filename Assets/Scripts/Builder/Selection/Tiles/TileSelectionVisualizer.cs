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
    private readonly HashSet<Vector3Int> selectedCells = new ();

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
    public void Refresh(List<Vector3Int> cells)
    {
        this.Clear();

        foreach (Vector3Int cell in cells)
        {
            this.selectedCells.Add(cell);
        }

        foreach (Vector3Int cell in this.selectedCells)
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
    private void UpdateCell(Vector3Int cell)
    {
        Vector3Int above = cell + Vector3Int.up;
        Vector3Int below = cell + Vector3Int.down;
        Vector3Int left = cell + Vector3Int.left;
        Vector3Int right = cell + Vector3Int.right;

        if (!this.selectedCells.Contains(above))
        {
            this.topOverlay.SetTile(cell, this.topTile);
        }

        if (!this.selectedCells.Contains(below))
        {
            this.bottomOverlay.SetTile(cell, this.bottomTile);
        }

        if (!this.selectedCells.Contains(left))
        {
            this.leftOverlay.SetTile(cell, this.leftTile);
        }

        if (!this.selectedCells.Contains(right))
        {
            this.rightOverlay.SetTile(cell, this.rightTile);
        }
    }
}