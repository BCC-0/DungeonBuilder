using System;
using UnityEngine;

/// <summary>
/// The tile data we use for saving.
/// </summary>
[Serializable]
public class TileData
{
    [SerializeField]
    private int x;
    [SerializeField]
    private int y;
    [SerializeField]
    private string tileID;

    [SerializeField]
    private bool hasCollision;
    [SerializeField]
    private string tag;

    /// <summary>
    /// Gets or sets the x position.
    /// </summary>
    public int X
    {
        get { return this.x; }
        set { this.x = value; }
    }

    /// <summary>
    /// Gets or sets the y position.
    /// </summary>
    public int Y
    {
        get { return this.y; }
        set { this.y = value; }
    }

    /// <summary>
    /// Gets or sets the tile ID.
    /// </summary>
    public string TileID
    {
        get { return this.tileID; }
        set { this.tileID = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this tile has collision.
    /// </summary>
    public bool HasCollision
    {
        get { return this.hasCollision; }
        set { this.hasCollision = value; }
    }

    /// <summary>
    /// Gets or sets the tag of this tile.
    /// </summary>
    public string Tag
    {
        get { return this.tag; }
        set { this.tag = value; }
    }
}