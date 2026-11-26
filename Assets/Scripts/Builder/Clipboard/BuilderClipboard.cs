using System;
using UnityEngine;

/// <summary>
/// Holds what was last copied in the builder so it can be pasted again.
/// One kind of content is held at a time, because only one layer can be edited at a time.
/// </summary>
public static class BuilderClipboard
{
    /// <summary>
    /// Called whenever the clipboard content changes, so the UI can follow it.
    /// </summary>
    public static event Action Changed;

    /// <summary>
    /// Gets a value indicating whether the clipboard holds anything.
    /// </summary>
    public static bool HasContent => Tiles != null || Entities != null;

    /// <summary>
    /// Gets the copied tiles, or null when entities were copied last.
    /// </summary>
    public static ClipboardTiles Tiles { get; private set; }

    /// <summary>
    /// Gets the copied entities, or null when tiles were copied last.
    /// </summary>
    public static ClipboardEntities Entities { get; private set; }

    /// <summary>
    /// Stores copied tiles, replacing whatever was on the clipboard before.
    /// </summary>
    /// <param name="tiles">The tiles to store.</param>
    public static void StoreTiles(ClipboardTiles tiles)
    {
        Tiles = tiles;
        Entities = null;

        Changed?.Invoke();
    }

    /// <summary>
    /// Stores copied entities, replacing whatever was on the clipboard before.
    /// </summary>
    /// <param name="entities">The entities to store.</param>
    public static void StoreEntities(ClipboardEntities entities)
    {
        Entities = entities;
        Tiles = null;

        Changed?.Invoke();
    }

    /// <summary>
    /// Empties the clipboard.
    /// </summary>
    public static void Clear()
    {
        Tiles = null;
        Entities = null;

        Changed?.Invoke();
    }
}
