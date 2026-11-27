using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the status effects of one entity, combines their values into the
/// values the entity uses, ticks their timers and damage over time, and shows
/// their icons above the entity. <see cref="StatusEffectEntity"/> adds this
/// controller to itself, so every enemy and the player get it for free.
/// </summary>
public class StatusEffectController : MonoBehaviour
{
    private readonly List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();

    private StatusEffectEntity entity;
    private float pendingDamage;
    private int revision;
    private int pushedRevision = -1;

    /// <summary>
    /// Gets the effects that are currently active, in the order they were applied.
    /// </summary>
    public IReadOnlyList<StatusEffectInstance> ActiveEffects => this.activeEffects;

    /// <summary>
    /// Gets the number of active effects.
    /// </summary>
    public int Count => this.activeEffects.Count;

    /// <summary>
    /// Gets a value indicating whether the entity cannot move.
    /// </summary>
    public bool PreventsMovement => this.HasEffect(effect => effect.PreventsMovement);

    /// <summary>
    /// Gets a value indicating whether the entity cannot attack.
    /// </summary>
    public bool PreventsAttack => this.HasEffect(effect => effect.PreventsAttack);

    /// <summary>
    /// Gets a value indicating whether the entity cannot use items or tools.
    /// </summary>
    public bool PreventsItemUse => this.HasEffect(effect => effect.PreventsItemUse);

    /// <summary>
    /// Gets a value indicating whether the entity runs away from the player.
    /// </summary>
    public bool FleesFromAttacker => this.HasEffect(effect => effect.FleesFromAttacker);

    /// <summary>
    /// Gets a value indicating whether the entity is confused.
    /// </summary>
    public bool ConfusesMovement => this.HasEffect(effect => effect.ConfusesMovement);

    /// <summary>
    /// Gets a value indicating whether enemies treat the player as detected.
    /// </summary>
    public bool RevealsTarget => this.HasEffect(effect => effect.RevealsTarget);

    /// <summary>
    /// Gets a value indicating whether an effect changes the position the
    /// entity reacts to, so its target is no longer the player itself.
    /// </summary>
    public bool ChangesTargetPosition => this.FleesFromAttacker || this.ConfusesMovement;

    /// <summary>
    /// Gets the combined multiplier on the damage the entity deals.
    /// </summary>
    public float DamageDealtMultiplier => this.Multiply(effect => effect.DamageDealtMultiplier);

    /// <summary>
    /// Gets the combined multiplier on the damage the entity takes.
    /// </summary>
    public float DamageTakenMultiplier => this.Multiply(effect => effect.DamageTakenMultiplier);

    /// <summary>
    /// Gets the combined multiplier on the time between the attacks of the entity.
    /// </summary>
    public float AttackSpeedMultiplier => this.Multiply(effect => effect.AttackSpeedMultiplier);

    /// <summary>
    /// Gets the combined multiplier on the movement speed of the entity.
    /// </summary>
    public float MoveSpeedMultiplier => this.Multiply(effect => effect.MoveSpeedMultiplier);

    /// <summary>
    /// Gets the combined multiplier on the detection range of the entity.
    /// </summary>
    public float VisionMultiplier => this.Multiply(effect => effect.VisionMultiplier);

    /// <summary>
    /// Gets the combined fraction of the damage taken that is dealt back to the attacker.
    /// </summary>
    public float DamageReflection => this.Multiply(effect => effect.DamageReflection);

    /// <summary>
    /// Links this controller to the entity that owns it.
    /// </summary>
    /// <param name="statusEntity">The entity that owns this controller.</param>
    public void Initialize(StatusEffectEntity statusEntity)
    {
        this.entity = statusEntity;
    }

    /// <summary>
    /// Applies an effect to the entity. An effect that already runs is
    /// refreshed and gets a stack instead of running twice.
    /// </summary>
    /// <param name="definition">The effect to apply.</param>
    /// <param name="duration">
    /// The duration in seconds, or zero to use the duration of the definition.
    /// </param>
    public void Apply(StatusEffectDefinition definition, float duration = 0f)
    {
        if (definition == null)
        {
            return;
        }

        float effectDuration = duration > 0f ? duration : definition.Duration;
        StatusEffectInstance instance = this.Find(definition);

        if (instance == null)
        {
            this.activeEffects.Add(new StatusEffectInstance(definition, effectDuration));
        }
        else
        {
            instance.Refresh(effectDuration);
        }

        this.revision++;
    }

    /// <summary>
    /// Applies every effect of the given applications.
    /// </summary>
    /// <param name="applications">The effects to apply, or null for none.</param>
    public void ApplyAll(StatusEffectApplication[] applications)
    {
        if (applications == null)
        {
            return;
        }

        foreach (StatusEffectApplication application in applications)
        {
            this.Apply(application.Definition, application.Duration);
        }
    }

    /// <summary>
    /// Removes every effect from the entity.
    /// </summary>
    public void ClearAll()
    {
        if (this.activeEffects.Count == 0)
        {
            return;
        }

        this.activeEffects.Clear();
        this.pendingDamage = 0f;
        this.revision++;
    }

    private void Update()
    {
        if (this.entity == null || !this.entity.ExistsInWorld)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        for (int i = this.activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectInstance instance = this.activeEffects[i];
            instance.Tick(deltaTime);

            if (instance.IsExpired)
            {
                this.activeEffects.RemoveAt(i);
                this.revision++;
                continue;
            }

            this.pendingDamage +=
                instance.Definition.DamagePerSecond * instance.Stacks * deltaTime;
        }

        // Damage is dealt in whole points, so damage over time that deals a
        // fraction per frame still hurts the integer health of the entity.
        if (this.pendingDamage >= 1f)
        {
            float damage = Mathf.Floor(this.pendingDamage);
            this.pendingDamage -= damage;
            this.entity.ApplyStatusDamage(damage);
        }

        this.UpdateWorldUI();
    }

    private void UpdateWorldUI()
    {
        EntityWorldUI worldUI = this.entity.WorldUI;

        if (worldUI == null || this.revision == this.pushedRevision)
        {
            return;
        }

        this.pushedRevision = this.revision;

        List<StatusEffectDefinition> definitions =
            new List<StatusEffectDefinition>(this.activeEffects.Count);

        foreach (StatusEffectInstance instance in this.activeEffects)
        {
            definitions.Add(instance.Definition);
        }

        worldUI.SetStatusEffects(definitions);
    }

    private StatusEffectInstance Find(StatusEffectDefinition definition)
    {
        foreach (StatusEffectInstance instance in this.activeEffects)
        {
            if (instance.Definition == definition)
            {
                return instance;
            }
        }

        return null;
    }

    private float Multiply(Func<StatusEffectDefinition, float> selector)
    {
        float result = 1f;

        foreach (StatusEffectInstance instance in this.activeEffects)
        {
            result *= selector(instance.Definition);
        }

        return result;
    }

    private bool HasEffect(Func<StatusEffectDefinition, bool> predicate)
    {
        foreach (StatusEffectInstance instance in this.activeEffects)
        {
            if (predicate(instance.Definition))
            {
                return true;
            }
        }

        return false;
    }
}
