using UnityEngine;

/// <summary>
/// The base class for weapon items.
/// Weapons are equippable items that deal damage and can attack enemies.
/// </summary>
public abstract class Weapon : EquippableItem
{
    [Header("Weapon Stats")]
    [SerializeField]
    [RuntimeEditable]
    private float baseDamage;

    [SerializeField]
    private float knockbackForce;

    /// <summary>
    /// Gets the base damage of this weapon.
    /// </summary>
    public float BaseDamage => this.baseDamage;

    /// <summary>
    /// Gets the knockback force of this weapon.
    /// </summary>
    public float KnockbackForce => this.knockbackForce;

    /// <summary>
    /// Tool action, can check first if it is allowed here.
    /// </summary>
    protected override void PerformAction()
    {
        this.PerformAttack(Vector2.down, null);
    }

    /// <summary>
    /// Performs the weapon action using the player's facing direction.
    /// </summary>
    /// <param name="direction">The direction of the attack.</param>
    /// <param name="playerHandler">The player handler.</param>
    protected override void PerformAction(Vector2 direction, CrawlerPlayerHandler playerHandler)
    {
        this.PerformAttack(direction, playerHandler);
    }

    /// <summary>
    /// Executes the weapon-specific attack logic.
    /// Must be implemented in implementations.
    /// </summary>
    /// <param name="direction">The direction of the attack.</param>
    /// <param name="playerHandler">The player handler.</param>
    protected abstract void PerformAttack(Vector2 direction, CrawlerPlayerHandler playerHandler);

    /// <summary>
    /// Creates the damage info for an attack with this weapon.
    /// </summary>
    /// <param name="direction">The direction of the attack.</param>
    /// <returns>The damage info for this attack.</returns>
    protected DamageInfo CreateDamage(Vector2 direction)
    {
        return new DamageInfo(this.baseDamage, direction, Team.Player, true, this, this.knockbackForce);
    }
}