using UnityEngine;

/// <summary>
/// Consumable items are items that are consumed when picked up. (Effect boosts, health, etc.)
/// </summary>
public abstract class ConsumableItem : Item
{
    // Optional shared properties for all consumables can go here
    // e.g., public int maxStackSize = 1;

    // TODO: Set up animations for consumables.

    /// <summary>
    /// Called when the player tries to use the item.
    /// </summary>
    /// <param name="playerHandler">The player handler to use the item.</param>
    /// <param name="playerData">The player data to use the item.</param>
    public abstract override void Use(CrawlerPlayerHandler playerHandler, CrawlerPlayerData playerData);
}
