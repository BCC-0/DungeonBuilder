using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of the grid cells that contain a wall, so walls can find their
/// neighbours and update their sprite when a neighbour appears, moves or is
/// removed. The builder and the game each have their own grid, because builder
/// entities and real walls never connect to each other. A cell can hold more
/// than one wall, for example in the builder while a wall is being moved over
/// another wall, so removing one wall never hides the others.
/// </summary>
public sealed class WallGrid
{
    private static readonly Vector2Int[] NeighbourOffsets =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 1),
    };

    private readonly Dictionary<Vector2Int, List<Entry>> entries =
        new Dictionary<Vector2Int, List<Entry>>();

    private WallGrid()
    {
    }

    /// <summary>
    /// Gets the grid used by the walls in the builder.
    /// </summary>
    public static WallGrid Builder { get; } = new WallGrid();

    /// <summary>
    /// Gets the grid used by the walls in the game.
    /// </summary>
    public static WallGrid Game { get; } = new WallGrid();

    /// <summary>
    /// Converts a world position to a grid cell.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="cellSize">The size of one cell in world units.</param>
    /// <returns>The cell that contains the position.</returns>
    public static Vector2Int ToCell(Vector3 position, float cellSize)
    {
        float size = Mathf.Max(0.0001f, cellSize);

        return new Vector2Int(
            Mathf.FloorToInt((position.x / size) + 0.01f),
            Mathf.FloorToInt((position.y / size) + 0.01f));
    }

    /// <summary>
    /// Registers a wall in a cell. Walls that are already in the cell stay
    /// registered.
    /// </summary>
    /// <param name="cell">The cell the wall is in.</param>
    /// <param name="owner">The object that owns the wall.</param>
    /// <param name="refresh">The action that updates the sprite of the wall.</param>
    public void Register(Vector2Int cell, object owner, Action refresh)
    {
        if (!this.entries.TryGetValue(cell, out List<Entry> list))
        {
            list = new List<Entry>();
            this.entries[cell] = list;
        }

        list.RemoveAll(e => e.Owner == owner);
        list.Add(new Entry(owner, refresh));
    }

    /// <summary>
    /// Removes a wall from a cell. Other walls in the same cell stay
    /// registered, so the cell only becomes empty when its last wall is gone.
    /// </summary>
    /// <param name="cell">The cell the wall was in.</param>
    /// <param name="owner">The object that owns the wall.</param>
    public void Unregister(Vector2Int cell, object owner)
    {
        if (!this.entries.TryGetValue(cell, out List<Entry> list))
        {
            return;
        }

        list.RemoveAll(e => e.Owner == owner);

        if (list.Count == 0)
        {
            this.entries.Remove(cell);
        }
    }

    /// <summary>
    /// Gets the sides of a cell that have a wall next to them.
    /// </summary>
    /// <param name="cell">The cell to check.</param>
    /// <returns>The sides that connect to another wall.</returns>
    public WallConnections GetConnections(Vector2Int cell)
    {
        WallConnections connections = WallConnections.None;

        if (this.Has(cell + Vector2Int.up))
        {
            connections |= WallConnections.Up;
        }

        if (this.Has(cell + Vector2Int.right))
        {
            connections |= WallConnections.Right;
        }

        if (this.Has(cell + Vector2Int.down))
        {
            connections |= WallConnections.Down;
        }

        if (this.Has(cell + Vector2Int.left))
        {
            connections |= WallConnections.Left;
        }

        return connections;
    }

    /// <summary>
    /// Gets the diagonal cells of a cell that have a wall.
    /// </summary>
    /// <param name="cell">The cell to check.</param>
    /// <returns>The diagonals that contain a wall.</returns>
    public WallCorners GetCorners(Vector2Int cell)
    {
        WallCorners corners = WallCorners.None;

        if (this.Has(cell + new Vector2Int(1, 1)))
        {
            corners |= WallCorners.UpRight;
        }

        if (this.Has(cell + new Vector2Int(1, -1)))
        {
            corners |= WallCorners.DownRight;
        }

        if (this.Has(cell + new Vector2Int(-1, -1)))
        {
            corners |= WallCorners.DownLeft;
        }

        if (this.Has(cell + new Vector2Int(-1, 1)))
        {
            corners |= WallCorners.UpLeft;
        }

        return corners;
    }

    /// <summary>
    /// Updates the sprite of every wall next to a cell.
    /// </summary>
    /// <param name="cell">The cell whose neighbours are updated.</param>
    public void RefreshNeighbours(Vector2Int cell)
    {
        foreach (Vector2Int offset in NeighbourOffsets)
        {
            if (!this.entries.TryGetValue(cell + offset, out List<Entry> list))
            {
                continue;
            }

            // Copy, so a refresh that changes the grid cannot break the loop.
            foreach (Entry entry in list.ToArray())
            {
                entry.Refresh();
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGrids()
    {
        Builder.entries.Clear();
        Game.entries.Clear();
    }

    private bool Has(Vector2Int cell)
    {
        return this.entries.TryGetValue(cell, out List<Entry> list) &&
               list.Count > 0;
    }

    private sealed class Entry
    {
        public Entry(object owner, Action refresh)
        {
            this.Owner = owner;
            this.Refresh = refresh;
        }

        public object Owner { get; }

        public Action Refresh { get; }
    }
}