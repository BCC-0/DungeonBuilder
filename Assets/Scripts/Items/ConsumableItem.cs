using UnityEngine;

/// <summary>
/// Consumable items are items that are consumed when picked up. (Effect boosts, health, etc.)
/// </summary>
public abstract class ConsumableItem : Item
{
    // Optional shared properties for all consumables can go here
    // e.g., public int maxStackSize = 1;

    /// <summary>
    /// The action when the item is used.
    /// Is called immediately for used items.
    /// </summary>
    public abstract override void Use();
}
