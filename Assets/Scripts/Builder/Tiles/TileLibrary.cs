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
    private static List<TileLibrary> allLibraries;

    [SerializeField]
    private List<TileEntry> tiles = new List<TileEntry>();

    private Dictionary<string, TileEntry> tileMap;

    /// <summary>
    /// Gets all tile entries in this library.
    /// </summary>
    public IReadOnlyList<TileEntry> Tiles => this.tiles;

    /// <summary>
    /// Gets the tile by ID from all TileLibraries in Resources.
    /// </summary>
    /// <param name="tileID">The ID to find a tile for.</param>
    /// <returns>The TileBase if it was found.</returns>
    public static TileBase GetTileByIDGlobal(string tileID)
    {
        LoadAllLibraries();

        foreach (TileLibrary library in allLibraries)
        {
            TileBase tile = library.GetTileByID(tileID);

            if (tile != null)
            {
                return tile;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the ID for a given tile from all TileLibraries in Resources.
    /// </summary>
    /// <param name="tile">The tile to find.</param>
    /// <returns>The ID of the tile if it was found.</returns>
    public static string GetIDForTileGlobal(TileBase tile)
    {
        if (tile == null)
        {
            return null;
        }

        LoadAllLibraries();

        foreach (TileLibrary library in allLibraries)
        {
            string tileID = library.GetIDForTile(tile);

            if (!string.IsNullOrEmpty(tileID))
            {
                return tileID;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the tile behavior by ID from all TileLibraries in Resources.
    /// </summary>
    /// <param name="tileID">The ID to find a behavior for.</param>
    /// <returns>The behavior string if it was found.</returns>
    public static string GetBehaviorTypeGlobal(string tileID)
    {
        LoadAllLibraries();

        foreach (TileLibrary library in allLibraries)
        {
            string behavior = library.GetBehaviorType(tileID);

            if (!string.IsNullOrEmpty(behavior))
            {
                return behavior;
            }
        }

        return null;
    }

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

        this.tileMap.TryGetValue(tileID, out TileEntry entry);
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

        this.tileMap.TryGetValue(tileID, out TileEntry entry);
        return entry?.BehaviorTypeName;
    }

    /// <summary>
    /// Gets an ID for a given tile.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>The found ID.</returns>
    public string GetIDForTile(TileBase tile)
    {
        if (tile == null)
        {
            return null;
        }

        if (this.tileMap == null)
        {
            this.Initialize();
        }

        foreach (TileEntry entry in this.tileMap.Values)
        {
            if (entry.Tile == tile)
            {
                return entry.TileID;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all valid tile entries as inventory items.
    /// </summary>
    /// <returns>The inventory items represented by this library.</returns>
    public IEnumerable<InventoryItem> GetInventoryItems()
    {
        foreach (TileEntry entry in this.tiles)
        {
            if (entry.Tile == null)
            {
                continue;
            }

            yield return new InventoryItem(
                entry.Tile,
                this.GetTileSprite(entry.Tile));
        }
    }

    /// <summary>
    /// Loads all TileLibrary assets from Resources.
    /// </summary>
    private static void LoadAllLibraries()
    {
        if (allLibraries != null)
        {
            return;
        }

        allLibraries = new List<TileLibrary>(Resources.LoadAll<TileLibrary>(string.Empty));
    }

    /// <summary>
    /// Gets the sprite used to display a tile in the inventory.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>The sprite representing the tile.</returns>
    private Sprite GetTileSprite(TileBase tile)
    {
        if (tile is Tile normalTile)
        {
            return normalTile.sprite;
        }

        return null;
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