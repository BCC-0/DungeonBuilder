using UnityEngine;

/// <summary>
/// Consumable items are items that are consumed when picked up. (Effect boosts, health, etc.)
/// </summary>
public abstract class ConsumableItem : Item
{
    // Optional shared properties for all consumables can go here
    // e.g., public int maxStackSize = 1;

    /// <summary>
    /// /// The action when the item is used.
    /// Must be overridden by subclasses.
    /// Should be called immediately for Consumable types.
    /// </summary>
    /// <param name="playerData">The player to use the item.</param>
    public abstract override void Use(CrawlerPlayerData playerData);
}
