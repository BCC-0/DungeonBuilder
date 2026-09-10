using UnityEngine;

/// <summary>
/// A script that should be attached to all objects that should keep repeating damage every n seconds.
/// </summary>
public class RepeatingDamage : Damage
{
    [SerializeField]
    private int repeatingAmount;
    [SerializeField]
    private float repeatInterval;

    /// <summary>
    /// Gets or sets the repeating amount of damage to deal.
    /// </summary>
    public int RepeatingAmount
    {
        get => this.repeatingAmount;
        set => this.repeatingAmount = value;
    }

    /// <summary>
    /// Gets or sets the repeating amount of damage to deal.
    /// </summary>
    public float RepeatInterval
    {
        get => this.repeatInterval;
        set => this.repeatInterval = value;
    }
}
