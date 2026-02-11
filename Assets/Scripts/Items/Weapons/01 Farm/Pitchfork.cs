using UnityEngine;

/// <summary>
/// A simple pitchfork weapon.
/// </summary>
[CreateAssetMenu(fileName = "NewPitchfork", menuName = "Items/Weapons/Pitchfork")]
public class Pitchfork : Weapon
{
    /// <summary>
    /// The actual attack logic for the pitchfork.
    /// </summary>
    protected override void PerformAttack()
    {
        // For now, just log the attack
        Debug.Log($"{this.ItemName} attacks dealing {this.BaseDamage} damage.");

        // TODO after implementing enemies.
    }
}
