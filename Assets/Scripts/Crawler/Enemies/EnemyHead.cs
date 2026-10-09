using UnityEngine;

/// <summary>
/// Base class for enemy heads. A head is consumed by an <see cref="EnemyBody"/>
/// and decides when and how that enemy acts: it picks a target, checks its
/// trigger conditions and then executes its behaviour, at most once per
/// <see cref="CooldownDuration"/>.
/// </summary>
/// <remarks>
/// Fields marked with [SaveField] on a head can be private. Only the fields
/// declared on this base class would need to be protected, because
/// SaveableEntity does not find private fields of a base class.
/// </remarks>
public abstract class EnemyHead : SaveableEntity
{
    private EnemyBody body;
    private float nextTriggerTime;

    /// <summary>
    /// Gets the body that uses this head. Null until attached.
    /// </summary>
    public EnemyBody Body => this.body;

    /// <summary>
    /// Gets the sprite of this head, used by the body to draw it.
    /// </summary>
    public Sprite Sprite =>
        this.SpriteRenderer != null ? this.SpriteRenderer.sprite : null;

    /// <summary>
    /// Gets the time in seconds between two triggers.
    /// Override this and return a [SaveField] field of the head, for
    /// example the attack speed or a timer length.
    /// </summary>
    protected virtual float CooldownDuration => 0f;

    /// <summary>
    /// Gets how far this head can target. Override to return a [SaveField]
    /// field such as the eyesight length.
    /// </summary>
    protected virtual float TargetRange => float.PositiveInfinity;

    /// <summary>
    /// Gets the position of the body this head sits on.
    /// </summary>
    protected Vector2 Origin =>
        this.body != null ? (Vector2)this.body.transform.position : (Vector2)this.transform.position;

    /// <summary>
    /// Attaches this head to the body that uses it.
    /// </summary>
    /// <param name="enemyBody">The body that uses this head.</param>
    public void Attach(EnemyBody enemyBody)
    {
        this.body = enemyBody;
        this.OnAttached();
    }

    /// <summary>
    /// Runs the targeting, trigger and behaviour of this head. Called every
    /// frame by the body while the enemy is alive.
    /// </summary>
    public void Tick()
    {
        if (Time.time < this.nextTriggerTime)
        {
            return;
        }

        Transform target = this.FindTarget();

        if (!this.ShouldTrigger(target))
        {
            return;
        }

        this.nextTriggerTime = Time.time + this.CooldownDuration;
        this.Execute(target);
    }

    /// <summary>
    /// Called when the enemy is about to receive damage. Change the amount
    /// to block or scale the damage, or react to being hit.
    /// </summary>
    /// <param name="damage">The damage that is about to be applied.</param>
    public virtual void OnDamageReceived(ref Damage damage)
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
    /// Targeting behaviour. By default the player, when within range.
    /// </summary>
    /// <returns>The target, or null when there is none.</returns>
    protected virtual Transform FindTarget()
    {
        Transform player = this.body.PlayerTransform;

        if (player == null)
        {
            return null;
        }

        float distance = Vector2.Distance(this.Origin, player.position);
        return distance <= this.TargetRange ? player : null;
    }

    /// <summary>
    /// Trigger condition. By default the head triggers when it has a target.
    /// </summary>
    /// <param name="target">The target found by <see cref="FindTarget"/>.</param>
    /// <returns>True when the behaviour should execute.</returns>
    protected virtual bool ShouldTrigger(Transform target)
    {
        return target != null;
    }

    /// <summary>
    /// The behaviour of this head.
    /// </summary>
    /// <param name="target">The target, which can be null for heads that trigger without one.</param>
    protected abstract void Execute(Transform target);
}