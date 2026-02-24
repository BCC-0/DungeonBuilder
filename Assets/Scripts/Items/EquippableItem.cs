using UnityEngine;

/// <summary>
/// Base class for items that can be equipped by the player, such as weapons and tools.
/// Contains shared logic for cooldowns and animations.
/// </summary>
public abstract class EquippableItem : Item
{
    [Header("Equippable Item Settings")]
    [SerializeField] private float cooldown;

    [Header("Directional Animations")]
    [SerializeField] private AnimationClip useUp;
    [SerializeField] private AnimationClip useDown;
    [SerializeField] private AnimationClip useLeft;
    [SerializeField] private AnimationClip useRight;

    /// <summary>
    /// Gets the cooldown of this item between uses.
    /// </summary>
    public float Cooldown => this.cooldown;

    /// <summary>
    /// Returns the correct animation clip for the given direction.
    /// </summary>
    /// <param name="direction">The direction of the clip we want.</param>
    /// <returns>The corresponding animation clip.</returns>
    public AnimationClip GetAnimation(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x > 0 ? this.useRight : this.useLeft;
        }

        return direction.y > 0 ? this.useUp : this.useDown;
    }

    /// <summary>
    /// Called when the player tries to use the item.
    /// Handles cooldown logic automatically and calls the derived PerformAction.
    /// </summary>
    /// <param name="playerHandler">The player handler to use the item.</param>
    /// <param name="playerData">The player data to use the item.</param>
    public override void Use(CrawlerPlayerHandler playerHandler, CrawlerPlayerData playerData)
    {
        if (!this.CanUse(playerData))
        {
            return;
        }

        AnimationClip clip = this.GetAnimation(playerHandler.LastFacingDirection);
        playerHandler.PlayUseAnimation(clip);

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
        return Time.time >= lastUse;
    }

    /// <summary>
    /// Performs the item action.
    /// Must be implemented by subclasses.
    /// </summary>
    protected abstract void PerformAction();
}
