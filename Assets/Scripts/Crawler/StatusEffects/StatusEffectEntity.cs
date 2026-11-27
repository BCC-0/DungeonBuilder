using UnityEngine;

/// <summary>
/// Base class for entities that can carry status effects, such as the player
/// and every enemy. The effects themselves are data only, so a new enemy gets
/// them without writing a single line of effect code.
/// </summary>
public abstract class StatusEffectEntity : HurtFlashEntity
{
    private StatusEffectController statusEffects;
    private EntityWorldUI worldUI;

    /// <summary>
    /// Gets the controller that tracks the active effects of this entity.
    /// </summary>
    public StatusEffectController StatusEffects => this.statusEffects;

    /// <summary>
    /// Gets the team this entity fights for. Reflected damage is dealt with it.
    /// </summary>
    public virtual Team Team => Team.Neutral;

    /// <summary>
    /// Gets or sets the world UI that shows the status effect icons above
    /// this entity.
    /// </summary>
    public EntityWorldUI WorldUI
    {
        get => this.worldUI;
        set => this.worldUI = value;
    }

    /// <summary>
    /// Applies a status effect to this entity.
    /// </summary>
    /// <param name="definition">The effect to apply.</param>
    /// <param name="duration">
    /// The duration in seconds, or zero to use the duration of the definition.
    /// </param>
    public void ApplyStatusEffect(StatusEffectDefinition definition, float duration = 0f)
    {
        this.statusEffects?.Apply(definition, duration);
    }

    /// <summary>
    /// Applies every effect of the given applications to this entity.
    /// </summary>
    /// <param name="applications">The effects to apply, or null for none.</param>
    public void ApplyStatusEffects(StatusEffectApplication[] applications)
    {
        this.statusEffects?.ApplyAll(applications);
    }

    /// <summary>
    /// Applies the effects and the reflection of damage that this entity just
    /// received. Called by the damage receivers before the health is changed.
    /// </summary>
    /// <param name="damage">The damage this entity received.</param>
    public void ProcessIncomingDamage(DamageInfo damage)
    {
        this.statusEffects?.ApplyAll(damage.Effects);

        float reflection = this.statusEffects != null
            ? this.statusEffects.DamageReflection
            : 0f;

        if (reflection <= 0f || damage.Amount <= 0f)
        {
            return;
        }

        if (damage.Source is Component source)
        {
            StatusEffectEntity attacker =
                source.GetComponentInParent<StatusEffectEntity>();

            if (attacker != null && attacker != this)
            {
                attacker.ApplyStatusDamage(damage.Amount * reflection);
            }
        }
    }

    /// <summary>
    /// Applies damage that comes from a status effect or from reflected
    /// damage. It bypasses the damage multipliers, so effects can never feed
    /// into each other.
    /// </summary>
    /// <param name="amount">The amount of damage.</param>
    public abstract void ApplyStatusDamage(float amount);

    /// <summary>
    /// Clears all effects when a new map is loaded.
    /// </summary>
    public override void OnFinishMapLoad()
    {
        base.OnFinishMapLoad();
        this.statusEffects?.ClearAll();
    }

    /// <summary>
    /// Creates the controller that tracks the effects of this entity.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        this.statusEffects = this.gameObject.AddComponent<StatusEffectController>();
        this.statusEffects.Initialize(this);
    }
}
