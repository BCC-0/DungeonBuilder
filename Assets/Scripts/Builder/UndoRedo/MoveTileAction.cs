using UnityEngine;

/// <summary>
/// Moving a single tile to another cell.
/// </summary>
public class MoveTileAction : IUndoableAction
{
    private readonly SaveableTilemap tilemap;
    private readonly Vector2Int oldPosition;
    private readonly Vector2Int newPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveTileAction"/> class.
    /// </summary>
    /// <param name="tilemap">The tilemap that contains the tile.</param>
    /// <param name="oldPosition">The cell the tile was moved from.</param>
    /// <param name="newPosition">The cell the tile was moved to.</param>
    public MoveTileAction(
        SaveableTilemap tilemap,
        Vector2Int oldPosition,
        Vector2Int newPosition)
    {
        this.tilemap = tilemap;
        this.oldPosition = oldPosition;
        this.newPosition = newPosition;
    }

    /// <inheritdoc/>
    public void Undo()
    {
        this.Move(this.newPosition, this.oldPosition);
    }

    /// <inheritdoc/>
    public void Redo()
    {
        this.Move(this.oldPosition, this.newPosition);
    }

    private void Move(Vector2Int from, Vector2Int to)
    {
        if (this.tilemap == null)
        {
            return;
        }

        if (!this.tilemap.MoveTile(from, to))
        {
            WarningManager.Warn(
                $"Could not move tile from {from} to {to}.");
        }
    }
}
