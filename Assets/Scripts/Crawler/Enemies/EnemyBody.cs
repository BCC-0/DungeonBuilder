
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Base class for all enemies. The body is the enemy itself: it has the
/// health, moves, takes damage and reacts when the player touches it. It
/// consumes an <see cref="EnemyHead"/> that decides when and how it acts.
/// </summary>
[RequireComponent(typeof(SortingGroup))]
public abstract class EnemyBody : SaveableEntity, IDamageable
{
    private static List<EnemyBody> activeEnemies = new List<EnemyBody>();

    [SaveField]
    private string displayName;

    /// <summary>
    /// The maximum health.
    /// </summary>
    [SerializeField]
    [SaveField(1, 500)]
    private int maxHealth = 10;

    /// <summary>
    /// The movement speed.
    /// </summary>
    [SerializeField]
    [SaveField(0, 12)]
    private float moveSpeed = 2f;

    /// <summary>
    /// The distance within which the enemy detects the player. Outside of
    /// this range the enemy wanders instead of reacting to the player.
    /// </summary>
    [SerializeField]
    [SaveField(1, 30)]
    private float detectionRange = 8f;

    /// <summary>
    /// The head that is attached to this body.
    /// </summary>
    [SaveField(referenceMode: ReferenceMode.Consuming)]
    private EnemyHead head;

    /// <summary>
    /// The transform under which the head is instantiated.
    /// </summary>
    [SerializeField]
    private Transform headAnchor;

    [Header("Head Anchor Positions")]
    [SerializeField]
    private Vector3 headDownPosition;

    [SerializeField]
    private Vector3 headRightPosition;

    [SerializeField]
    private Vector3 headUpPosition;

    [SerializeField]
    private Vector3 headLeftPosition;

    [Header("Direction Switching")]
    [SerializeField]
    private float directionSwitchDelay = 0.2f;

    [Header("Bobbing")]
    [SerializeField]
    private Transform visualRoot;
    [SerializeField]
    private float bobHeight = 0.05f;
    [SerializeField]
    private float bobSpeed = 12f;

    [Header("Knockback")]
    [SerializeField]
    private bool canReceiveKnockback = true;

    private float bobTime;

    private int currentHealth;
    private float damageMultiplier = 1f;
    private CrawlerPlayerData player;
    private Direction direction = Direction.Down;
    private Direction visibleDirection = Direction.Down;
    private bool directionSwitchPending;
    private float directionSwitchTime;

    /// <summary>
    /// The four directions an enemy can face.
    /// Values correspond to the Animator direction parameter.
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// The down direction.
        /// </summary>
        Down = 0,

        /// <summary>
        /// The right direction.
        /// </summary>
        Right = 1,

        /// <summary>
        /// The up direction.
        /// </summary>
        Up = 2,

        /// <summary>
        /// The left direction.
        /// </summary>
        Left = 3,
    }

    /// <summary>
    /// Gets all enemies that are currently active, for range checks such as
    /// alerting or buffing other enemies.
    /// </summary>
    public static IReadOnlyList<EnemyBody> ActiveEnemies => activeEnemies;

    /// <summary>
    /// Gets the name of this enemy.
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(this.displayName) ? this.name : this.displayName;

    /// <summary>
    /// Gets the team of this enemy.
    /// </summary>
    public Team Team => Team.Enemy;

    /// <summary>
    /// Gets or sets the multiplier applied to all damage this enemy deals.
    /// Used for buffs.
    /// </summary>
    public float DamageMultiplier
    {
        get => this.damageMultiplier;
        set => this.damageMultiplier = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Gets a value indicating whether this enemy can receive knockback.
    /// </summary>
    public bool CanReceiveKnockback => this.canReceiveKnockback;

    /// <summary>
    /// Gets the transform of the player, or null when there is no player.
    /// </summary>
    public Transform PlayerTransform
    {
        get
        {
            CrawlerPlayerData currentPlayer = this.Player;
            return currentPlayer != null ? currentPlayer.transform : null;
        }
    }

    /// <summary>
    /// Gets the distance within which this enemy detects the player.
    /// </summary>
    public float DetectionRange => this.detectionRange;

    /// <summary>
    /// Gets a value indicating whether the player is within the detection
    /// range. False when there is no player.
    /// </summary>
    public bool IsPlayerDetected
    {
        get
        {
            Transform playerTransform = this.PlayerTransform;

            if (playerTransform == null)
            {
                return false;
            }

            Vector2 position = this.Rb != null
                ? this.Rb.position
                : this.Origin;

            return Vector2.Distance(position, (Vector2)playerTransform.position)
                <= this.detectionRange;
        }
    }

    /// <summary>
    /// Gets the direction that is actually shown by the body and the head.
    /// It follows <see cref="FacingDirection"/> after a short delay, so the
    /// enemy does not flicker between directions when the player is close to
    /// the angle where two directions switch.
    /// </summary>
    public Direction VisibleDirection => this.visibleDirection;

    /// <summary>
    /// Gets or sets the direction this enemy wants to face. The visible
    /// direction of the body and head follows this with a delay.
    /// </summary>
    protected Direction FacingDirection
    {
        get => this.direction;
        set => this.direction = value;
    }

    /// <summary>
    /// Gets the movement speed.
    /// </summary>
    protected float MoveSpeed => this.moveSpeed;

    /// <summary>
    /// Gets the maximum health.
    /// </summary>
    protected int MaxHealth => this.maxHealth;

    /// <summary>
    /// Gets the current health. Only the enemy itself can change it.
    /// </summary>
    protected int CurrentHealth => this.currentHealth;

    /// <summary>
    /// Gets the head of this enemy.
    /// </summary>
    protected EnemyHead Head => this.head;

    /// <summary>
    /// Gets or sets a value indicating whether this enemy is moving.
    /// </summary>
    protected bool IsMoving { get; set; }

    /// <summary>
    /// Gets the rigidbody of this enemy.
    /// </summary>
    protected Rigidbody2D Rb { get; private set; }

    /// <summary>
    /// Gets the player data, or null when there is no player. It is looked
    /// up the first time it is needed.
    /// </summary>
    protected CrawlerPlayerData Player
    {
        get
        {
            if (this.player == null)
            {
                this.player = FindAnyObjectByType<CrawlerPlayerData>();
            }

            return this.player;
        }
    }

    /// <summary>
    /// Gets the position of this enemy.
    /// </summary>
    protected Vector2 Origin => this.transform.position;

    /// <summary>
    /// Damages this enemy. The body and head can change or block the damage
    /// first, and react to being hit.
    /// </summary>
    /// <param name="damage">The damage to apply.</param>
    public void TakeDamage(DamageInfo damage)
    {
        this.OnDamageReceived(ref damage);

        if (this.head != null)
        {
            this.head.OnDamageReceived(ref damage);
        }

        if (damage.Amount <= 0)
        {
            return;
        }

        this.currentHealth = Mathf.Max(
            this.currentHealth - Mathf.RoundToInt(damage.Amount),
            0);

        this.FlashHurtEffect();

        if (damage.Knockback > 0f)
        {
            this.ApplyKnockback(damage.Direction, damage.Knockback);
        }

        if (this.currentHealth == 0)
        {
            this.Die();
        }
    }

    /// <summary>
    /// Applies knockback if this enemy can receive it.
    /// </summary>
    /// <param name="direction">The direction to give knockback in.</param>
    /// <param name="force">The force of the knockback.</param>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (!this.canReceiveKnockback ||
            !this.ExistsInWorld ||
            !KnockbackUtility.Apply(this.Rb, direction, force))
        {
            return;
        }

        this.OnKnockbackApplied(direction, force);
    }

    /// <summary>
    /// Called after knockback has been successfully applied.
    /// Override for enemy-specific reactions.
    /// </summary>
    /// <param name="direction">The direction to give knockback in.</param>
    /// <param name="force">The force of the knockback.</param>
    protected virtual void OnKnockbackApplied(
        Vector2 direction,
        float force)
    {
    }

    /// <summary>
    /// Called when the map is finished loading. Attaches the head and draws
    /// it on this enemy.
    /// </summary>
    /// <summary>
    /// Called when the map is finished loading. Attaches the head to this enemy.
    /// </summary>
    public override void OnFinishMapLoad()
    {
        base.OnFinishMapLoad();

        this.currentHealth = this.maxHealth;

        // Start with the wanted direction, without any delay.
        this.visibleDirection = this.direction;
        this.directionSwitchPending = false;

        if (this.head == null)
        {
            return;
        }

        Transform parent = this.headAnchor != null
            ? this.headAnchor
            : this.transform;

        this.UpdateHeadAnchorPosition();

        this.head.transform.SetParent(parent, false);
        this.head.transform.localPosition = Vector3.zero;
        this.head.transform.localRotation = Quaternion.identity;
        this.head.transform.localScale = Vector3.one;

        this.head.Attach(this);
        this.head.SetFacingDirection(this.visibleDirection);
    }

    /// <summary>
    /// Each enemy implementation defines its own movement behaviour for when
    /// the player is detected, so within the detection range.
    /// </summary>
    protected abstract void UpdateMovement();

    /// <summary>
    /// Called instead of <see cref="UpdateMovement"/> while the player is not
    /// detected. By default the enemy stands still. Override it to give the
    /// enemy its own wander behaviour.
    /// </summary>
    protected virtual void UpdateWander()
    {
        this.IsMoving = false;
    }

    /// <summary>
    /// Gets the rigidbody, and keeps health valid before the map is loaded.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        this.Rb = this.GetComponent<Rigidbody2D>();
        this.currentHealth = this.maxHealth;
    }

    /// <summary>
    /// Called when this enemy is about to receive damage. Change the amount
    /// to block or scale the damage, or react to being hit.
    /// </summary>
    /// <param name="damage">The damage that is about to be applied.</param>
    protected virtual void OnDamageReceived(ref DamageInfo damage)
    {
    }

    /// <summary>
    /// Called when this enemy dies, before it is removed from the world.
    /// </summary>
    protected virtual void OnDeath()
    {
    }

    /// <summary>
    /// Called when the visible direction changed, after the head anchor and
    /// the head were updated. Use it to switch the body animation.
    /// </summary>
    protected virtual void OnVisibleDirectionChanged()
    {
    }

    /// <summary>
    /// Removes the enemy, after the body and head had their say.
    /// </summary>
    protected virtual void Die()
    {
        this.OnDeath();

        if (this.head != null)
        {
            this.head.OnDeath();
        }

        Destroy(this.gameObject);
    }

    private void OnEnable()
    {
        if (!activeEnemies.Contains(this))
        {
            activeEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    private void Update()
    {
        if (!this.ExistsInWorld)
        {
            return;
        }

        this.UpdateVisibleDirection();

        if (this.head != null)
        {
            this.head.Tick();
        }
    }

    private void FixedUpdate()
    {
        if (!this.ExistsInWorld)
        {
            return;
        }

        // React to the player when detected, otherwise wander.
        if (this.IsPlayerDetected)
        {
            this.UpdateMovement();
        }
        else
        {
            this.UpdateWander();
        }
    }

    private void LateUpdate()
    {
        if (this.visualRoot == null)
        {
            return;
        }

        float offset = 0f;

        if (this.IsMoving)
        {
            this.bobTime += Time.deltaTime * this.bobSpeed;
            offset = Mathf.Abs(Mathf.Sin(this.bobTime)) * this.bobHeight;
        }
        else
        {
            this.bobTime = 0f;
        }

        this.visualRoot.localPosition = new Vector3(0f, offset, 0f);
    }

    /// <summary>
    /// Makes the visible direction follow the wanted direction after the
    /// switch delay. If the wanted direction returns to the visible one
    /// before the delay is over, nothing changes.
    /// </summary>
    private void UpdateVisibleDirection()
    {
        if (this.direction == this.visibleDirection)
        {
            this.directionSwitchPending = false;
            return;
        }

        if (!this.directionSwitchPending)
        {
            this.directionSwitchPending = true;
            this.directionSwitchTime = Time.time + this.directionSwitchDelay;
        }

        if (Time.time < this.directionSwitchTime)
        {
            return;
        }

        // Apply the latest wanted direction. The body moves the head anchor
        // and the head in the same moment, so they never disagree.
        this.directionSwitchPending = false;
        this.visibleDirection = this.direction;

        this.UpdateHeadAnchorPosition();

        if (this.head != null)
        {
            this.head.SetFacingDirection(this.visibleDirection);
        }

        this.OnVisibleDirectionChanged();
    }

    private void UpdateHeadAnchorPosition()
    {
        if (this.headAnchor == null)
        {
            return;
        }

        this.headAnchor.localPosition = this.visibleDirection switch
        {
            Direction.Down => this.headDownPosition,
            Direction.Right => this.headRightPosition,
            Direction.Up => this.headUpPosition,
            Direction.Left => this.headLeftPosition,
            _ => Vector3.zero
        };
    }
}
