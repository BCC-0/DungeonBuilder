using UnityEngine;

/// <summary>
/// Shared physics operations for knockback.
/// </summary>
public static class KnockbackUtility
{
    /// <summary>
    /// Applies an impulse to a Rigidbody2D.
    /// </summary>
    /// <param name="body">The body to apply knockback to.</param>
    /// <param name="direction">The direction to give knockback in.</param>
    /// <param name="force">The force of the knockback.</param>
    /// <returns>A value indicating whether the knockback was applied.</returns>
    public static bool Apply(
        Rigidbody2D body,
        Vector2 direction,
        float force)
    {
        if (body == null || force <= 0f)
        {
            return false;
        }

        body.AddForce(
            direction.normalized * force,
            ForceMode2D.Impulse);

        return true;
    }
}
