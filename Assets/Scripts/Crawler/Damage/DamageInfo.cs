using UnityEngine;

/// <summary>
/// Describes a single damage event.
/// </summary>
public readonly struct DamageInfo
{
    /// <summary>
    /// The amount of damage this object deals.
    /// </summary>
    public readonly float Amount;

    /// <summary>
    /// The direction this damage is in. Can be used for knockback or animations.
    /// </summary>
    public readonly Vector2 Direction;

    /// <summary>
    /// The team this damage belongs to.
    /// </summary>
    public readonly Team Team;

    /// <summary>
    /// A value indicating whether this damage is melee damage or not.
    /// </summary>
    public readonly bool Melee;

    /// <summary>
    /// The source of this damage.
    /// </summary>
    public readonly object Source;

    /// <summary>
    /// The strength of the knockback this damage applies. Zero means no knockback.
    /// </summary>
    public readonly float Knockback;

    /// <summary>
    /// Initializes a new instance of the <see cref="DamageInfo"/> struct.
    /// </summary>
    /// <param name="amount">The amount.</param>
    /// <param name="direction">The direction.</param>
    /// <param name="team">The team of this damage object.</param>
    /// <param name="melee">Indicating whether this is melee damage.</param>
    /// <param name="source">The source of this damage.</param>
    /// <param name="knockback">The strength of the knockback, zero for none.</param>
    public DamageInfo(float amount, Vector2 direction, Team team, bool melee, object source, float knockback = 0f)
    {
        this.Amount = amount;
        this.Direction = direction;
        this.Team = team;
        this.Melee = melee;
        this.Source = source;
        this.Knockback = knockback;
    }

    /// <summary>
    /// Creates a copy of this damage with a different amount.
    /// </summary>
    /// <param name="amount">The new amount.</param>
    /// <returns>The new damage info.</returns>
    public DamageInfo WithAmount(float amount)
    {
        return new DamageInfo(amount, this.Direction, this.Team, this.Melee, this.Source, this.Knockback);
    }
}