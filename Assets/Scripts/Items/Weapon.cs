using UnityEngine;

/// <summary>
/// The base class for weapon items.
/// </summary>
public abstract class Weapon : Item
{
    [Header("Weapon Stats")]
    [SerializeField]
    private float baseDamage;
    [SerializeField]
    private float attackCooldown;

    [Header("Weapon Visuals")]
    public AnimationClip attackAnimation;

    protected float lastAttackTime;
    
    /// <summary>
    /// Gets the base damage of this weapon.
    /// </summary>
    public float BaseDamage => this.baseDamage;

    /// <summary>
    /// Gets the attack cooldown of this weapon.
    /// </summary>
    public float AttackCooldown => this.attackCooldown;

    /// <summary>
    /// Called when the player tries to use the weapon.
    /// </summary>
    public override void Use()
    {
        if (!this.CanUse())
        {
            return;
        }

        this.lastAttackTime = Time.time;
        this.PerformAttack();
    }

    /// <summary>
    /// Gets whether the player can currently use this weapon.
    /// Default returns true when the attack cooldown is over.
    /// </summary>
    /// <returns>Whether the player can use weapon.</returns>
    public override bool CanUse()
    {
        return Time.time >= this.lastAttackTime + this.attackCooldown;
    }

    /// <summary>
    /// Weapon-specific attack logic (melee hit, raycast, projectile, etc.)
    /// </summary>
    protected abstract void PerformAttack();
}
