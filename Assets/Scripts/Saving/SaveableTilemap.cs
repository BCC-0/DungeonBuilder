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
    /// Updates an existing tile's data and refreshes its visual/collision state.
    /// </summary>
    /// <param name="position">The position to apply the update to.</param>
    /// <param name="data">The tiledata to apply to the given position.</param>
    public void UpdateTileData(Vector2Int position, TileData data)
    {
        if (data == null)
        {
            return;
        }

        this.tiles[position] = data;
        this.UpdateSingleTile(data);
    }

    /// <summary>
    /// Updates the Tilemap visually and creates colliders for tiles that need them.
    /// Full rebuild used mainly on load.
    /// </summary>
    public void RebuildTilemap()
    {
        if (this.tilemap == null)
        {
            Debug.LogWarning("Tilemap not assigned.");
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
    public void SetTile(
            int x,
            int y,
            string tileID,
            bool hasCollision = false,
            string tag = null)
    {
        Vector2Int key = new Vector2Int(x, y);
        Vector3Int pos = new Vector3Int(x, y, 0);

        // Remove tile.
        if (string.IsNullOrEmpty(tileID))
        {
            this.tiles.Remove(key);

            this.tilemap.SetTile(pos, null);

            if (this.collisionObjects.TryGetValue(
                key,
                out GameObject existing))
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
    /// Gets the TileBehaviour associated with the tile at the given position.
    /// </summary>
    /// <param name="position">The tile position.</param>
    /// <returns>
    /// The TileBehaviour associated with the tile, or null if the tile
    /// does not have a behaviour.
    /// </returns>
    public TileBehaviour GetTileBehaviour(Vector2Int position)
    {
        if (!this.collisionObjects.TryGetValue(
            position,
            out GameObject collisionObject))
        {
            return null;
        }

        if (collisionObject == null)
        {
            return null;
        }

        return collisionObject.GetComponent<TileBehaviour>();
    }

    /// <summary>
    /// Gets the tile data at the given position, or null if no tile exists there.
    /// </summary>
    /// <param name="position">The position of the tile data to get.</param>
    /// <returns>The tile data at the given position.</returns>
    public TileData GetTileData(Vector2Int position)
    {
        this.tiles.TryGetValue(position, out TileData tile);
        return tile;
    }

    /// <summary>
    /// Moves a tile from one grid position to another.
    /// </summary>
    /// <param name="oldPosition">The current tile position.</param>
    /// <param name="newPosition">The new tile position.</param>
    /// <returns>
    /// True if the tile was moved successfully; otherwise false.
    /// </returns>
    public bool MoveTile(Vector2Int oldPosition, Vector2Int newPosition)
    {
        if (oldPosition == newPosition)
        {
            return true;
        }

        if (!this.tiles.TryGetValue(
                oldPosition,
                out TileData tile))
        {
            return false;
        }

        if (this.tiles.ContainsKey(newPosition))
        {
            return false;
        }

        Vector3Int oldCell =
            new Vector3Int(
                oldPosition.x,
                oldPosition.y,
                0);

        this.tilemap.SetTile(
            oldCell,
            null);

        if (this.collisionObjects.TryGetValue(
                oldPosition,
                out GameObject oldCollision))
        {
            this.collisionObjects.Remove(
                oldPosition);

            if (oldCollision != null)
            {
                Destroy(oldCollision);
            }
        }

        this.tiles.Remove(oldPosition);

        tile.X = newPosition.x;
        tile.Y = newPosition.y;

        this.tiles[newPosition] = tile;

        this.UpdateSingleTile(tile);

        return true;
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
            : TileLibrary.GetTileByIDGlobal(tile.TileID);

        this.tilemap.SetTile(pos, tileBase);

        Vector2Int key = new Vector2Int(tile.X, tile.Y);

        if (this.collisionObjects.TryGetValue(
            key,
            out GameObject existing))
        {
            Destroy(existing);
            this.collisionObjects.Remove(key);
        }

        if (tile.HasCollision)
        {
            GameObject colObj = new GameObject(
                $"Collider_{tile.X}_{tile.Y}");

            colObj.transform.position =
                this.tilemap.CellToWorld(pos) +
                new Vector3(0.5f, 0.5f, 0);

            colObj.transform.parent = this.tilemap.transform;

            BoxCollider2D collider =
                colObj.AddComponent<BoxCollider2D>();

            collider.isTrigger = false;

            if (!string.IsNullOrEmpty(tile.Tag))
            {
                colObj.tag = tile.Tag;
            }

            string behaviorTypeName =
                string.IsNullOrEmpty(tile.TileID)
                    ? null
                    : TileLibrary.GetBehaviorTypeGlobal(tile.TileID);

            if (!string.IsNullOrEmpty(behaviorTypeName))
            {
                System.Type type =
                    System.Type.GetType(behaviorTypeName);

                if (type != null &&
                    typeof(TileBehaviour).IsAssignableFrom(type))
                {
                    colObj.AddComponent(type);
                }
            }

            this.collisionObjects[key] = colObj;
        }
    }
}