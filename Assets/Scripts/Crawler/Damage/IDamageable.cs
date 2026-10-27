using UnityEngine;

/// <summary>
/// The side something fights for.
/// </summary>
public enum Team
{
    /// <summary>
    /// Neutral entities.
    /// </summary>
    Neutral,

    /// <summary>
    /// Player entities.
    /// </summary>
    Player,

    /// <summary>
    /// Enemy entities.
    /// </summary>
    Enemy,
}

/// <summary>
/// Implemented by anything that can take damage.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Gets the team of this object.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// Applies damage to this object.
    /// </summary>
    /// <param name="info">The damage details.</param>
    void TakeDamage(DamageInfo info);
}
