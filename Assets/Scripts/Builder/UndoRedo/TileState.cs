using System;

/// <summary>
/// The state of a single tile cell.
/// </summary>
public readonly struct TileState : IEquatable<TileState>
{
    /// <summary>
    /// The state of an empty cell.
    /// </summary>
    public static readonly TileState Empty = new TileState(null, false, null);

    /// <summary>
    /// The tile ID, or null when the cell is empty.
    /// </summary>
    public readonly string TileID;

    /// <summary>
    /// Whether the tile has collision.
    /// </summary>
    public readonly bool HasCollision;

    /// <summary>
    /// The tag of the tile, or null when it has none.
    /// </summary>
    public readonly string Tag;

    /// <summary>
    /// Initializes a new instance of the <see cref="TileState"/> struct.
    /// </summary>
    /// <param name="tileID">The tile ID, or null for an empty cell.</param>
    /// <param name="hasCollision">Whether the tile has collision.</param>
    /// <param name="tag">The tag of the tile.</param>
    public TileState(string tileID, bool hasCollision, string tag)
    {
        this.TileID = string.IsNullOrEmpty(tileID) ? null : tileID;
        this.HasCollision = hasCollision;
        this.Tag = string.IsNullOrEmpty(tag) ? null : tag;
    }

    /// <summary>
    /// Creates the state from tile data.
    /// </summary>
    /// <param name="data">The tile data, or null for an empty cell.</param>
    /// <returns>The state of the cell.</returns>
    public static TileState FromData(TileData data)
    {
        return data == null
            ? Empty
            : new TileState(data.TileID, data.HasCollision, data.Tag);
    }

    /// <inheritdoc/>
    public bool Equals(TileState other)
    {
        return this.TileID == other.TileID &&
               this.HasCollision == other.HasCollision &&
               this.Tag == other.Tag;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        return obj is TileState other && this.Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.TileID, this.HasCollision, this.Tag);
    }
}