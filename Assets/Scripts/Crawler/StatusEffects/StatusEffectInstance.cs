using UnityEngine;

/// <summary>
/// One running instance of a <see cref="StatusEffectDefinition"/> on an entity.
/// It keeps the remaining time and the amount of stacks of that effect.
/// </summary>
public class StatusEffectInstance
{
    private float duration;
    private float remainingTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusEffectInstance"/> class.
    /// </summary>
    /// <param name="definition">The effect this instance runs.</param>
    /// <param name="duration">The duration in seconds. Zero lasts until it is removed.</param>
    public StatusEffectInstance(StatusEffectDefinition definition, float duration)
    {
        this.Definition = definition;
        this.duration = Mathf.Max(0f, duration);
        this.remainingTime = this.duration;
        this.Stacks = 1;
    }

    /// <summary>
    /// Gets the effect this instance runs.
    /// </summary>
    public StatusEffectDefinition Definition { get; }

    /// <summary>
    /// Gets how many times the effect is stacked.
    /// </summary>
    public int Stacks { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this instance has run out.
    /// </summary>
    public bool IsExpired => this.duration > 0f && this.remainingTime <= 0f;

    /// <summary>
    /// Advances the timer of this instance.
    /// </summary>
    /// <param name="deltaTime">The time that passed in seconds.</param>
    public void Tick(float deltaTime)
    {
        if (this.duration <= 0f)
        {
            return;
        }

        this.remainingTime -= deltaTime;
    }

    /// <summary>
    /// Restarts the timer and adds a stack, up to the maximum of the definition.
    /// </summary>
    /// <param name="newDuration">The duration to restart with, in seconds.</param>
    public void Refresh(float newDuration)
    {
        this.duration = Mathf.Max(0f, newDuration);
        this.remainingTime = this.duration;

        int maxStacks = Mathf.Max(1, this.Definition.MaxStacks);
        this.Stacks = Mathf.Min(this.Stacks + 1, maxStacks);
    }
}
