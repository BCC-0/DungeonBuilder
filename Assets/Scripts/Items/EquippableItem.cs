using UnityEngine;

/// <summary>
/// Base class for items that can be equipped by the player, such as weapons and tools.
/// Contains shared logic for cooldowns and animations.
/// </summary>
public abstract class EquippableItem : Item
{
    [Header("Equippable Item Settings")]
    [SerializeField]
    private float cooldown;

    [SerializeField]
    private AnimationClip useAnimation;

    /// <summary>
    /// Gets the cooldown of this item between uses.
    /// </summary>
    public float Cooldown => this.cooldown;

    /// <summary>
    /// Gets the animation played when this item is used.
    /// AnimationClips of equipabble items should have the player as root.
    /// </summary>
    public AnimationClip UseAnimation => this.useAnimation;



    /// <summary>
    /// Called when the player tries to use the item.
    /// Handles cooldown logic automatically and calls the derived PerformAction.
    /// </summary>
    /// <param name="playerData">The player to use the item.</param>
    public override void Use(CrawlerPlayerData playerData)
    {
        if (!this.CanUse(playerData))
        {
            return;
        }

        playerData.SetLastUseTime(Time.time);
        this.PerformAction();
    }

    /// <summary>
    /// Determines whether the item can currently be used.
    /// Default implementation checks cooldown.
    /// </summary>
    /// <param name="playerData">The player to use the item.</param>
    /// <returns>Whether the player can use this item.</returns>
    public bool CanUse(CrawlerPlayerData playerData)
    {
        float lastUse = playerData.LastUseTime + this.cooldown;
        Debug.Log("Last use time + cooldown: " + lastUse + " and current time: " + Time.time);
        return Time.time >= lastUse;
    }

    /// <summary>
    /// Performs the item action.
    /// Must be implemented by subclasses.
    /// </summary>
    protected abstract void PerformAction();
}
