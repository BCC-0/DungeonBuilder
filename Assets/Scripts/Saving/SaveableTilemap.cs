using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A saveable tilemap that stores tile data and can rebuild the visual map.
/// </summary>
public class SaveableTilemap : SaveableEntity
{
    [SerializeField]
    private Tilemap tilemap;

    [SerializeField]
    private TileLibrary tileLibrary;

    [SerializeField]
    private List<TileData> tiles = new List<TileData>();

    private List<GameObject> collisionObjects = new List<GameObject>();

    /// <summary>
    /// Writes the tilemap data to the save file.
    /// </summary>
    /// <param name="writer">Binary writer.</param>
    public override void Write(BinaryWriter writer)
    {
        base.Write(writer);

        writer.Write(this.tiles.Count);

        foreach (TileData tile in this.tiles)
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
            this.tiles.Add(tile);
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

        foreach (GameObject col in this.collisionObjects)
        {
            if (col != null)
            {
                Destroy(col);
            }
        }

        this.collisionObjects.Clear();

        foreach (TileData tile in this.tiles)
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
        this.tiles.RemoveAll(t => t.X == x && t.Y == y);

        TileData tile = new TileData
        {
            X = x,
            Y = y,
            TileID = tileID,
            HasCollision = hasCollision,
            Tag = tag,
        };

        this.tiles.Add(tile);
        this.UpdateSingleTile(tile);
    }

    /// <summary>
    /// Updates a single tile visually and updates or adds its collider/behavior.
    /// </summary>
    /// <param name="tile">The tile data to update.</param>
    private void UpdateSingleTile(TileData tile)
    {
        Vector3Int pos = new Vector3Int(tile.X, tile.Y, 0);
        TileBase tileBase = this.tileLibrary.GetTileByID(tile.TileID);

        this.tilemap.SetTile(pos, tileBase);

        // Remove existing collider if it exists
        GameObject existing = this.collisionObjects.Find(c => c.name == $"Collider_{tile.X}_{tile.Y}");
        if (existing != null)
        {
            this.collisionObjects.Remove(existing);
            Destroy(existing);
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

            // Attach behavior if defined in library
            string behaviorTypeName = this.tileLibrary.GetBehaviorType(tile.TileID);
            if (!string.IsNullOrEmpty(behaviorTypeName))
            {
                System.Type type = System.Type.GetType(behaviorTypeName);
                if (type != null && typeof(TileBehaviour).IsAssignableFrom(type))
                {
                    colObj.AddComponent(type);
                }
            }

            this.collisionObjects.Add(colObj);
        }
    }
}