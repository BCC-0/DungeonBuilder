using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all enemies. The body is the enemy itself: it has the
/// health, moves, takes damage and reacts when the player touches it. It
/// consumes an <see cref="EnemyHead"/> that decides when and how it acts.
/// </summary>
/// <remarks>
/// Fields marked with [SaveField] on this class are protected, because
/// SaveableEntity finds save fields with reflection on the runtime type, and
/// that does not return private fields declared in a base class.
/// </remarks>
public abstract class EnemyBody : SaveableEntity
{
    private static List<EnemyBody> activeEnemies = new List<EnemyBody>();

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
    /// The head of this enemy. It is consumed, so it disappears from the
    /// world and is drawn on this body instead.
    /// </summary>
    [SerializeField]
    [SaveField(referenceMode: ReferenceMode.Consuming)]
    private EnemyHead head;

    [SerializeField]
    private string displayName;

    [SerializeField]
    private SpriteRenderer headRenderer;

    private int currentHealth;
    private float damageMultiplier = 1f;
    private Rigidbody2D rigidBody;
    private CrawlerPlayerData player;
    private int playerDamageLayer;

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
    /// Gets the movement speed.
    /// </summary>
    public float MoveSpeed => this.moveSpeed;

    /// <summary>
    /// Gets the maximum health.
    /// </summary>
    public int MaxHealth => this.maxHealth;

    /// <summary>
    /// Gets the current health.
    /// </summary>
    public int CurrentHealth => this.currentHealth;

    /// <summary>
    /// Gets the head of this enemy.
    /// </summary>
    public EnemyHead Head => this.head;

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
    /// Gets the transform of the player, or null when there is no player.
    /// </summary>
    public Transform PlayerTransform
    {
        get
        {
            if (this.player == null)
            {
                this.player = FindAnyObjectByType<CrawlerPlayerData>();
            }

            return this.player != null ? this.player.transform : null;
        }
    }

    /// <summary>
    /// Gets the position of this enemy.
    /// </summary>
    protected Vector2 Origin => this.transform.position;

    /// <summary>
    /// Decides how this enemy body moves and enables their animations.
    /// </summary>
    /// <param name="direction">The direction to move in.</param>
    /// <param name="speed">The speed to move with.</param>
    public abstract void Move();

    /// <summary>
    /// Damages the player, scaled by the damage multiplier.
    /// </summary>
    /// <param name="target">The player to damage.</param>
    /// <param name="amount">The amount of damage before scaling.</param>
    /// <returns>The health the player has left.</returns>
    public int DamagePlayer(CrawlerPlayerData target, int amount)
    {
        return target.TakeDamage(Mathf.RoundToInt(amount * this.damageMultiplier));
    }

    /// <summary>
    /// Damages this enemy. The body and head can change or block the damage
    /// first, and react to being hit.
    /// </summary>
    /// <param name="damage">The damage to apply.</param>
    public void TakeDamage(Damage damage)
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

        this.currentHealth = Mathf.Max(this.currentHealth - damage.Amount, 0);

        if (this.currentHealth == 0)
        {
            this.Die();
        }
    }

    /// <summary>
    /// Called when the map is finished loading. Attaches the head and draws
    /// it on this enemy.
    /// </summary>
    public override void OnFinishMapLoad()
    {
        base.OnFinishMapLoad();

        this.currentHealth = this.maxHealth;

        if (this.head == null)
        {
            return;
        }

        this.head.Attach(this);

        if (this.headRenderer != null)
        {
            this.headRenderer.sprite = this.head.Sprite;
        }
    }

    /// <summary>
    /// Called when the player touches this enemy. This is the collision
    /// behaviour of the body, for example damage.
    /// </summary>
    /// <param name="touchedPlayer">The player that touched this enemy.</param>
    public virtual void OnPlayerContact(CrawlerPlayerData touchedPlayer)
    {
    }

    /// <summary>
    /// Gets the rigidbody and layer, and keeps health valid before the map is loaded.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        this.rigidBody = this.GetComponent<Rigidbody2D>();
        this.playerDamageLayer = LayerMask.NameToLayer("PlayerDamage");
        this.currentHealth = this.maxHealth;
    }

    /// <summary>
    /// The movement of this enemy, called every frame while it is alive.
    /// Every body decides for itself how it moves. Use
    /// <see cref="Move"/> for simple movement, or move the enemy yourself,
    /// for example for jumping.
    /// </summary>
    protected abstract void UpdateMovement();

    /// <summary>
    /// Called when this enemy is about to receive damage. Change the amount
    /// to block or scale the damage, or react to being hit.
    /// </summary>
    /// <param name="damage">The damage that is about to be applied.</param>
    protected virtual void OnDamageReceived(ref Damage damage)
    {
    }

    /// <summary>
    /// Called when this enemy dies, before it is removed from the world.
    /// </summary>
    protected virtual void OnDeath()
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

        this.UpdateMovement();

        if (this.head != null)
        {
            this.head.Tick();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        this.HandleContact(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        this.HandleContact(collision.gameObject);
    }

    private void HandleContact(GameObject other)
    {
        // Damage dealt by the player, for example from a weapon.
        if (other.layer == this.playerDamageLayer &&
            other.TryGetComponent(out Damage damage))
        {
            this.TakeDamage(damage);
            return;
        }

        // The player running into this enemy.
        CrawlerPlayerData touchedPlayer = other.GetComponentInParent<CrawlerPlayerData>();

        if (touchedPlayer != null)
        {
            this.OnPlayerContact(touchedPlayer);
        }
    }
}