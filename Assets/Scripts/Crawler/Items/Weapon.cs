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

    /// <summary>
    /// Gets the base damage of this weapon.
    /// </summary>
    public float BaseDamage => this.baseDamage;

    /// <summary>
    /// Tool action, can check first if it is allowed here.
    /// </summary>
    protected override void PerformAction()
    {
        // Can do extra things beside just attacking here.
        this.PerformAttack();
    }

    /// <summary>
    /// Executes the weapon-specific attack logic.
    /// Must be implemented in implementations.
    /// </summary>
    protected abstract void PerformAttack();
}
