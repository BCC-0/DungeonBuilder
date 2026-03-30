using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A saveable tilemap that stores tile data and can rebuild the visual map.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class SaveableTilemap : SaveableEntity
{
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private TileLibrary tileLibrary;

    private Dictionary<Vector2Int, TileData> tiles = new ();
    private Dictionary<Vector2Int, GameObject> collisionObjects = new ();

    /// <summary>
    /// Gets a value indicating whether this gameobject should already be in the scene before loading.
    /// </summary>
    public bool IsSceneEntity => true;

    /// <summary>
    /// Gets the TileMap.
    /// </summary>
    public Tilemap Tilemap
    {
        get { return this.tilemap; }
    }

    /// <summary>
    /// Gets the TileLibrary.
    /// </summary>
    public TileLibrary TileLibrary
    {
        get { return this.tileLibrary; }
    }

    /// <summary>
    /// Writes the tilemap data to the save file.
    /// </summary>
    /// <param name="writer">Binary writer.</param>
    public override void Write(BinaryWriter writer)
    {
        base.Write(writer);

        writer.Write(this.tiles.Count);

        foreach (TileData tile in this.tiles.Values)
        {
            writer.Write(tile.X);
            writer.Write(tile.Y);
            writer.Write(tile.TileID ?? string.Empty);
            writer.Write(tile.HasCollision);
            writer.Write(tile.Tag ?? string.Empty);
        }
    }

    /// <summary>
    /// Reads tilemap data from the save file and rebuilds the map.
    /// </summary>
    /// <param name="reader">Binary reader.</param>
    public override void Read(BinaryReader reader)
    {
        base.Read(reader);

        this.tiles.Clear();

        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            TileData tile = new TileData
            {
                X = reader.ReadInt32(),
                Y = reader.ReadInt32(),
                TileID = reader.ReadString(),
                HasCollision = reader.ReadBoolean(),
                Tag = reader.ReadString(),
            };

            Vector2Int key = new Vector2Int(tile.X, tile.Y);
            this.tiles[key] = tile;
        }

        this.RebuildTilemap();
    }

    /// <summary>
    /// Updates the Tilemap visually and creates colliders for tiles that need them.
    /// Full rebuild used mainly on load.
    /// </summary>
    public void RebuildTilemap()
    {
        if (this.tilemap == null || this.tileLibrary == null)
        {
            Debug.LogWarning("Tilemap or TileLibrary not assigned.");
            return;
        }

        this.tilemap.ClearAllTiles();

        foreach (GameObject col in this.collisionObjects.Values)
        {
            if (col != null)
            {
                Destroy(col);
            }
        }

        this.collisionObjects.Clear();

        foreach (TileData tile in this.tiles.Values)
        {
            this.UpdateSingleTile(tile);
        }
    }

    /// <summary>
    /// Updates a single tile at runtime and rebuilds its collider if necessary.
    /// </summary>
    /// <param name="x">The x position of the tile.</param>
    /// <param name="y">The y position of the tile.</param>
    /// <param name="tileID">The tile ID.</param>
    /// <param name="hasCollision">Whether this tile has a collision or not.</param>
    /// <param name="tag">The tag of this tile.</param>
    public void SetTile(int x, int y, string tileID, bool hasCollision = false, string tag = null)
    {
        Vector2Int key = new Vector2Int(x, y);
        Vector3Int pos = new Vector3Int(x, y, 0);

        // Remove tile.
        if (string.IsNullOrEmpty(tileID))
        {
            this.tiles.Remove(key);

            this.tilemap.SetTile(pos, null);

            if (this.collisionObjects.TryGetValue(key, out GameObject existing))
            {
                this.collisionObjects.Remove(key);
                Destroy(existing);
            }

            return;
        }

        // Add or update tile.
        TileData tile = new TileData
        {
            X = x,
            Y = y,
            TileID = tileID,
            HasCollision = hasCollision,
            Tag = tag,
        };

        // Overwrite or add.
        this.tiles[key] = tile;

        this.UpdateSingleTile(tile);
    }

    /// <summary>
    /// Updates a single tile visually and updates or adds its collider/behavior.
    /// </summary>
    /// <param name="tile">The tile data to update.</param>
    private void UpdateSingleTile(TileData tile)
    {
        Vector3Int pos = new Vector3Int(tile.X, tile.Y, 0);

        TileBase tileBase = string.IsNullOrEmpty(tile.TileID)
            ? null
            : this.tileLibrary.GetTileByID(tile.TileID);

        this.tilemap.SetTile(pos, tileBase);

        // Remove existing collider if it exists
        Vector2Int key = new Vector2Int(tile.X, tile.Y);
        if (this.collisionObjects.TryGetValue(key, out var existing))
        {
            Destroy(existing);
            this.collisionObjects.Remove(key);
        }

        // Add collider and behavior if needed
        if (tile.HasCollision)
        {
            GameObject colObj = new GameObject($"Collider_{tile.X}_{tile.Y}");
            colObj.transform.position = this.tilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);
            colObj.transform.parent = this.tilemap.transform;

            BoxCollider2D collider = colObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;

            if (!string.IsNullOrEmpty(tile.Tag))
            {
                colObj.tag = tile.Tag;
            }

            string behaviorTypeName = string.IsNullOrEmpty(tile.TileID)
                ? null
                : this.tileLibrary.GetBehaviorType(tile.TileID);

            if (!string.IsNullOrEmpty(behaviorTypeName))
            {
                System.Type type = System.Type.GetType(behaviorTypeName);
                if (type != null && typeof(TileBehaviour).IsAssignableFrom(type))
                {
                    colObj.AddComponent(type);
                }
            }

            this.collisionObjects[key] = colObj;
        }
    }
}