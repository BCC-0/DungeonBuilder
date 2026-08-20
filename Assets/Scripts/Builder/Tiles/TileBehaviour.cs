using UnityEngine;

/// <summary>
/// Base class for tile logic. Can handle interactions, collisions, or triggers.
/// </summary>
public abstract class TileBehaviour : MonoBehaviour
{
    /// <summary>
    /// Called when the player interacts with this tile.
    /// </summary>
    /// <param name="player">The player that interacts with this tile.</param>
    public virtual void OnInteract(CrawlerPlayerHandler player)
    {
        Debug.Log($"Interacted with tile at {this.transform.position}");
    }

    /// <summary>
    /// Called when something collides with this tile.
    /// </summary>
    /// <param name="collider">The collider that triggers this method.</param>
    public virtual void OnTriggerEnter2D(Collider2D collider)
    {
        // Optional override
    }

    /// <summary>
    /// Called every frame, optional for dynamic tiles.
    /// </summary>
    public virtual void OnTileUpdate()
    {
    }
}