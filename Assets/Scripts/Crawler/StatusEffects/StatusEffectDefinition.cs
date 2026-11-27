using UnityEngine;

/// <summary>
/// Describes one status effect as data. Create an asset per effect; no code is
/// needed for a new effect, because <see cref="StatusEffectController"/> reads
/// every value from this asset. Multipliers of one change nothing, so an
/// effect only touches the values it actually changes.
/// </summary>
[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Combat/Status Effects/Status Effect")]
public class StatusEffectDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField]
    private string effectName;

    [SerializeField]
    [TextArea]
    private string description;

    [SerializeField]
    private Sprite icon;

    [Header("Duration")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("How long the effect lasts in seconds. Zero lasts until it is removed.")]
    private float duration = 5f;

    [SerializeField]
    [Min(1)]
    [Tooltip("How often the effect can stack. One means a new application only refreshes it.")]
    private int maxStacks = 1;

    [Header("Stat Multipliers")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("Multiplier on the damage the entity deals. One changes nothing.")]
    private float damageDealtMultiplier = 1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Multiplier on the damage the entity takes. One changes nothing.")]
    private float damageTakenMultiplier = 1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Multiplier on the time between attacks. Below one attacks faster.")]
    private float attackSpeedMultiplier = 1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Multiplier on the movement speed. One changes nothing.")]
    private float moveSpeedMultiplier = 1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Multiplier on the detection range. One changes nothing.")]
    private float visionMultiplier = 1f;

    [Header("Damage over Time")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("Damage per second while the effect is active. Scales with the amount of stacks.")]
    private float damagePerSecond;

    [Header("Restrictions")]
    [SerializeField]
    private bool preventsMovement;

    [SerializeField]
    private bool preventsAttack;

    [SerializeField]
    private bool preventsItemUse;

    [Header("Behaviour")]
    [SerializeField]
    [Tooltip("Makes the entity run away from the player instead of towards it.")]
    private bool fleesFromAttacker;

    [SerializeField]
    [Tooltip("Makes the entity chase a point that circles around the player.")]
    private bool confusesMovement;

    [SerializeField]
    [Tooltip("Makes enemies treat the player as detected, whatever the distance.")]
    private bool revealsTarget;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Fraction of the damage taken that is dealt back to the attacker.")]
    private float damageReflection;

    /// <summary>
    /// Gets the name shown to the player.
    /// </summary>
    public string EffectName => this.effectName;

    /// <summary>
    /// Gets the description shown to the player.
    /// </summary>
    public string Description => this.description;

    /// <summary>
    /// Gets the icon shown above the head of the entity.
    /// </summary>
    public Sprite Icon => this.icon;

    /// <summary>
    /// Gets the duration in seconds. Zero lasts until the effect is removed.
    /// </summary>
    public float Duration => this.duration;

    /// <summary>
    /// Gets how often the effect can stack.
    /// </summary>
    public int MaxStacks => this.maxStacks;

    /// <summary>
    /// Gets the multiplier on the damage the entity deals.
    /// </summary>
    public float DamageDealtMultiplier => this.damageDealtMultiplier;

    /// <summary>
    /// Gets the multiplier on the damage the entity takes.
    /// </summary>
    public float DamageTakenMultiplier => this.damageTakenMultiplier;

    /// <summary>
    /// Gets the multiplier on the time between the attacks of the entity.
    /// </summary>
    public float AttackSpeedMultiplier => this.attackSpeedMultiplier;

    /// <summary>
    /// Gets the multiplier on the movement speed of the entity.
    /// </summary>
    public float MoveSpeedMultiplier => this.moveSpeedMultiplier;

    /// <summary>
    /// Gets the multiplier on the detection range of the entity.
    /// </summary>
    public float VisionMultiplier => this.visionMultiplier;

    /// <summary>
    /// Gets the damage per second, before stacks are applied.
    /// </summary>
    public float DamagePerSecond => this.damagePerSecond;

    /// <summary>
    /// Gets a value indicating whether the entity cannot move.
    /// </summary>
    public bool PreventsMovement => this.preventsMovement;

    /// <summary>
    /// Gets a value indicating whether the entity cannot attack.
    /// </summary>
    public bool PreventsAttack => this.preventsAttack;

    /// <summary>
    /// Gets a value indicating whether the entity cannot use items or tools.
    /// </summary>
    public bool PreventsItemUse => this.preventsItemUse;

    /// <summary>
    /// Gets a value indicating whether the entity runs away from the player.
    /// </summary>
    public bool FleesFromAttacker => this.fleesFromAttacker;

    /// <summary>
    /// Gets a value indicating whether the entity moves towards a point that
    /// circles around the player instead of towards the player itself.
    /// </summary>
    public bool ConfusesMovement => this.confusesMovement;

    /// <summary>
    /// Gets a value indicating whether enemies treat the player as detected.
    /// </summary>
    public bool RevealsTarget => this.revealsTarget;

    /// <summary>
    /// Gets the fraction of the damage taken that is dealt back to the attacker.
    /// </summary>
    public float DamageReflection => this.damageReflection;
}
