using UnityEngine;

/// <summary>
/// A script that should be attached to all objects that deal damage.
/// The layer (EnemyDamage, PlayerDamage and Enemy) decides the source of damage.
/// </summary>
public class Damage : MonoBehaviour
{
    [SerializeField]
    private int amount;

    [SerializeField]
    private bool melee;

    /// <summary>
    /// Gets or sets the amount of damage to deal on first hit.
    /// </summary>
    public int Amount
    {
        get => this.amount;
        set => this.amount = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this damage was dealt melee.
    /// If it was, knockback can be applied to the damage dealer.
    /// </summary>
    public bool Melee
    {
        get => this.melee;
        set => this.melee = value;
    }
}
