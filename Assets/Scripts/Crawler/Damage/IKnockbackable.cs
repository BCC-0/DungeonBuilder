using UnityEngine;

/// <summary>
/// Implemented by entities that can be pushed away when they are hit.
/// </summary>
public interface IKnockbackable
{
	/// <summary>
	/// Applies a knockback impulse to this entity.
	/// </summary>
	/// <param name="direction">The normalized direction to push in.</param>
	/// <param name="force">The strength of the push.</param>
	void ApplyKnockback(Vector2 direction, float force);
}