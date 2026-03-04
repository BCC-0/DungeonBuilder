using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Holds a mapping between string IDs and TileBase objects.
/// Also stores the behavior type to attach at runtime for each tile.
/// </summary>
[CreateAssetMenu(fileName = "TileLibrary", menuName = "Dungeon/TileLibrary")]
public class TileLibrary : ScriptableObject
{
    [SerializeField]
    private List<TileEntry> tiles = new List<TileEntry>();

    private Dictionary<string, TileEntry> tileMap;

    /// <summary>
    /// Initialize the dictionary for fast lookup.
    /// </summary>
    public void Initialize()
    {
        this.tileMap = new Dictionary<string, TileEntry>();
        foreach (TileEntry entry in this.tiles)
        {
            if (!string.IsNullOrEmpty(entry.TileID) && entry.Tile != null)
            {
                this.tileMap[entry.TileID] = entry;
            }
        }
    }

    /// <summary>
    /// Gets a TileBase by its ID.
    /// </summary>
    /// <param name="tileID">The ID of the tile.</param>
    /// <returns>The TileBase, or null if not found.</returns>
    public TileBase GetTileByID(string tileID)
    {
        if (this.tileMap == null)
        {
            this.Initialize();
        }

        this.tileMap.TryGetValue(tileID, out var entry);
        return entry?.Tile;
    }

    /// <summary>
    /// Gets the behavior type name associated with a tile ID.
    /// </summary>
    /// <param name="tileID">The tile ID.</param>
    /// <returns>The behavior type name, or null if not defined.</returns>
    public string GetBehaviorType(string tileID)
    {
        if (this.tileMap == null)
        {
            this.Initialize();
        }

        this.tileMap.TryGetValue(tileID, out var entry);
        return entry?.BehaviorTypeName;
    }

    /// <summary>
    /// Represents a single tile entry in the library.
    /// </summary>
    [Serializable]
    public class TileEntry
    {
        [SerializeField]
        private string tileID;

        [SerializeField]
        private TileBase tile;

        [SerializeField]
        private string behaviorTypeName;

        /// <summary>
        /// Gets or sets the tile ID.
        /// </summary>
        public string TileID
        {
            get { return this.tileID; }
            set { this.tileID = value; }
        }

        /// <summary>
        /// Gets or sets the TileBase object.
        /// </summary>
        public TileBase Tile
        {
            get { return this.tile; }
            set { this.tile = value; }
        }

        /// <summary>
        /// Gets or sets the fully-qualified behavior type name to attach at runtime.
        /// </summary>
        public string BehaviorTypeName
        {
            get { return this.behaviorTypeName; }
            set { this.behaviorTypeName = value; }
        }
    }
}