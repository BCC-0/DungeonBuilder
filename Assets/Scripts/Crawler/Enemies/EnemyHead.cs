using UnityEngine;

/// <summary>
/// Base class for enemy heads. A head is consumed by an <see cref="EnemyBody"/>
/// and decides when and how that enemy acts. The base class only provides the
/// link to the body and the hooks the body calls. Everything else, such as
/// targeting, cooldowns and animation, is up to the head itself.
/// </summary>
/// <remarks>
/// Fields marked with [SaveField] on a head can be private. Only the fields
/// declared on this base class would need to be protected, because
/// SaveableEntity does not find private fields of a base class.
/// </remarks>
public abstract class EnemyHead : SaveableEntity
{
    private EnemyBody body;

    /// <summary>
    /// Gets the body that uses this head. Null until attached.
    /// </summary>
    public EnemyBody Body => this.body;

    /// <summary>
    /// Gets the direction the body is facing. The body decides when this
    /// changes, so the head always matches the visible body direction.
    /// </summary>
    public EnemyBody.Direction FacingDirection { get; private set; } = EnemyBody.Direction.Down;

    /// <summary>
    /// Gets the sprite of this head, used by the body to draw it.
    /// </summary>
    public Sprite Sprite => this.SpriteRenderer != null ? this.SpriteRenderer.sprite : null;

    /// <summary>
    /// Gets the position of the body this head sits on.
    /// </summary>
    protected Vector2 Origin =>
        this.body != null ? (Vector2)this.body.transform.position : (Vector2)this.transform.position;

    /// <summary>
    /// Sets the direction this head should face.
    /// </summary>
    /// <param name="direction">The direction the body is facing.</param>
    public void SetFacingDirection(EnemyBody.Direction direction)
    {
        if (this.FacingDirection == direction)
        {
            return;
        }

        this.FacingDirection = direction;
        this.OnFacingDirectionChanged();
    }

    /// <summary>
    /// Attaches this head to the body that uses it.
    /// </summary>
    /// <param name="enemyBody">The body that uses this head.</param>
    public void Attach(EnemyBody enemyBody)
    {
        this.body = enemyBody;

        // Start with the direction the body shows, so OnAttached already
        // sees the right direction.
        this.FacingDirection = enemyBody.VisibleDirection;

        this.SpriteRenderer.enabled = true;
        this.OnAttached();
    }

    /// <summary>
    /// Called every frame by the body while the enemy is alive. The head
    /// decides what to do with it.
    /// </summary>
    public abstract void Tick();

    /// <summary>
    /// Called when the enemy is about to receive damage. Change the amount
    /// to block or scale the damage, or react to being hit.
    /// </summary>
    /// <param name="damage">The damage that is about to be applied.</param>
    public virtual void OnDamageReceived(ref DamageInfo damage)
    {
    }

    /// <summary>
    /// Called when the enemy dies, before it is removed from the world.
    /// </summary>
    public virtual void OnDeath()
    {
    }

    /// <summary>
    /// Called when this head is attached to its body.
    /// </summary>
    protected virtual void OnAttached()
    {
    }

    /// <summary>
    /// Called when the direction this head faces changes.
    /// </summary>
    protected virtual void OnFacingDirectionChanged()
    {
    }
}