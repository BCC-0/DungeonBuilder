using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of the grid cells that contain a wall, so walls can find their
/// neighbours and update their sprite when a neighbour appears, moves or is
/// removed. The builder and the game each have their own grid, because builder
/// entities and real walls never connect to each other.
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

    private readonly Dictionary<Vector2Int, Entry> entries =
        new Dictionary<Vector2Int, Entry>();

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
    /// Registers a wall in a cell.
    /// </summary>
    /// <param name="cell">The cell the wall is in.</param>
    /// <param name="owner">The object that owns the wall.</param>
    /// <param name="refresh">The action that updates the sprite of the wall.</param>
    public void Register(Vector2Int cell, object owner, Action refresh)
    {
        this.entries[cell] = new Entry(owner, refresh);
    }

    /// <summary>
    /// Removes a wall from a cell. Nothing happens when another wall has
    /// taken over the cell.
    /// </summary>
    /// <param name="cell">The cell the wall was in.</param>
    /// <param name="owner">The object that owns the wall.</param>
    public void Unregister(Vector2Int cell, object owner)
    {
        if (this.entries.TryGetValue(cell, out Entry entry) &&
            entry.Owner == owner)
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

        if (this.entries.ContainsKey(cell + Vector2Int.up))
        {
            connections |= WallConnections.Up;
        }

        if (this.entries.ContainsKey(cell + Vector2Int.right))
        {
            connections |= WallConnections.Right;
        }

        if (this.entries.ContainsKey(cell + Vector2Int.down))
        {
            connections |= WallConnections.Down;
        }

        if (this.entries.ContainsKey(cell + Vector2Int.left))
        {
            connections |= WallConnections.Left;
        }

        return connections;
    }

    /// <summary>
    /// Gets the diagonal cells of a cell that have a wall.
    /// </summary>
    public WallCorners GetCorners(Vector2Int cell)
    {
        WallCorners corners = WallCorners.None;

        if (this.entries.ContainsKey(cell + new Vector2Int(1, 1)))
        {
            corners |= WallCorners.UpRight;
        }

        if (this.entries.ContainsKey(cell + new Vector2Int(1, -1)))
        {
            corners |= WallCorners.DownRight;
        }

        if (this.entries.ContainsKey(cell + new Vector2Int(-1, -1)))
        {
            corners |= WallCorners.DownLeft;
        }

        if (this.entries.ContainsKey(cell + new Vector2Int(-1, 1)))
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
            if (this.entries.TryGetValue(cell + offset, out Entry entry))
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
