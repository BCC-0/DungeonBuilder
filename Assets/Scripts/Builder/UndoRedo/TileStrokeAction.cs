using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A set of tile changes made in one go (one paint/erase stroke, a tile
/// delete, a tile move), stored as a single undo step.
/// For every cell only the state before the first change and the latest
/// state are kept, so painting over the same cell repeatedly stays small.
/// </summary>
public class TileStrokeAction : IUndoableAction
{
    private readonly Dictionary<Vector2Int, TileState> before = new ();
    private readonly Dictionary<Vector2Int, TileState> after = new ();
    private readonly Action<Vector2Int, TileState> applyTile;

    private List<Vector2Int> selectionBefore;
    private List<Vector2Int> selectionAfter;

    /// <summary>
    /// Initializes a new instance of the <see cref="TileStrokeAction"/> class.
    /// </summary>
    /// <param name="applyTile">Writes a tile state to a cell of the tilemap.</param>
    public TileStrokeAction(Action<Vector2Int, TileState> applyTile)
    {
        this.applyTile = applyTile;
    }

    /// <summary>
    /// Gets a value indicating whether the stroke changed at least one cell.
    /// </summary>
    public bool HasChanges
    {
        get
        {
            foreach (KeyValuePair<Vector2Int, TileState> entry in this.before)
            {
                if (!entry.Value.Equals(this.after[entry.Key]))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Stores the tile selection to restore when this action is undone or redone.
    /// </summary>
    public void SetSelection(
        IEnumerable<Vector2Int> before,
        IEnumerable<Vector2Int> after)
    {
        this.selectionBefore = new List<Vector2Int>(before);
        this.selectionAfter = new List<Vector2Int>(after);
    }

    /// <summary>
    /// Records a change of one cell.
    /// </summary>
    /// <param name="cell">The cell that changed.</param>
    /// <param name="stateBefore">The state of the cell before this change.</param>
    /// <param name="stateAfter">The state of the cell after this change.</param>
    public void Record(
        Vector2Int cell,
        TileState stateBefore,
        TileState stateAfter)
    {
        // Only the first touch decides what "before" is.
        if (!this.before.ContainsKey(cell))
        {
            this.before[cell] = stateBefore;
        }

        this.after[cell] = stateAfter;
    }

    /// <inheritdoc/>
    public void Undo()
    {
        this.Apply(this.before);
        ApplySelection(this.selectionBefore);
    }

    /// <inheritdoc/>
    public void Redo()
    {
        this.Apply(this.after);
        ApplySelection(this.selectionAfter);
    }

    private void Apply(Dictionary<Vector2Int, TileState> states)
    {
        foreach (KeyValuePair<Vector2Int, TileState> entry in states)
        {
            // Skip cells that ended up the same as they started.
            if (this.before[entry.Key].Equals(this.after[entry.Key]))
            {
                continue;
            }

            this.applyTile(entry.Key, entry.Value);
        }
    }

    private static void ApplySelection(List<Vector2Int> selection)
    {
        if (selection == null ||
            MapEditorManager.Instance == null)
        {
            return;
        }

        MapEditorManager.Instance.SelectedTiles =
            new List<Vector2Int>(selection);
    }
}
