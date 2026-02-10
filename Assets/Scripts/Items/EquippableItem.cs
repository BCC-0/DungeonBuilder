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

    private float lastUseTime = 0.0f;

    /// <summary>
    /// Gets the cooldown of this item between uses.
    /// </summary>
    public float Cooldown => this.cooldown;

    /// <summary>
    /// Gets the last time this item was used.
    /// Useful for cooldown checks.
    /// </summary>
    public float LastUseTime => this.lastUseTime;

    /// <summary>
    /// Gets the animation played when this item is used.
    /// AnimationClips of equipabble items should have the player as root.
    /// </summary>
    public AnimationClip UseAnimation => this.useAnimation;

    /// <summary>
    /// Called when the player tries to use the item.
    /// Handles cooldown logic automatically and calls the derived PerformAction.
    /// </summary>
    public override void Use()
    {
        if (!this.CanUse())
        {
            return;
        }

        this.lastUseTime = Time.time;
        this.PerformAction();
    }

    /// <summary>
    /// Determines whether the item can currently be used.
    /// Default implementation checks cooldown.
    /// </summary>
    /// <returns>True if the item can be used; otherwise, false.</returns>
    public override bool CanUse()
    {
        Debug.Log("Current time: " + Time.time);
        Debug.Log("versus last + cooldown: " + (this.lastUseTime + this.cooldown));
        return Time.time >= this.lastUseTime + this.cooldown;
    }

    /// <summary>
    /// Performs the item action.
    /// Must be implemented by subclasses.
    /// </summary>
    protected abstract void PerformAction();
}
