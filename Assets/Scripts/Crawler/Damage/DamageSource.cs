using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Damages opposing IDamageable targets on contact.
/// A repeat interval of 0 damages each target once per contact.
/// </summary>
public class DamageSource : MonoBehaviour
{
    private readonly Dictionary<IDamageable, float> nextHitTime = new();

    [SerializeField]
    private float amount;

    [SerializeField]
    private Team team;

    [SerializeField]
    private bool melee;

    [SerializeField]
    private float repeatInterval;

    [SerializeField]
    [Min(0f)]
    private float knockback;

    /// <summary>
    /// Gets or sets the damage dealt per hit.
    /// </summary>
    public float Amount
    {
        get => this.amount;
        set => this.amount = value;
    }

    /// <summary>
    /// Gets or sets the team of this damage source.
    /// </summary>
    public Team Team
    {
        get => this.team;
        set => this.team = value;
    }

    /// <summary>
    /// Gets or sets the knockback strength applied per hit.
    /// </summary>
    public float Knockback
    {
        get => this.knockback;
        set => this.knockback = Mathf.Max(0f, value);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        this.TryDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        this.TryDamage(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        this.Forget(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        this.TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        this.TryDamage(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        this.Forget(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (!other.TryGetComponent(out IDamageable target)
            || !TeamUtility.CanDamage(this.team, target.Team))
        {
            return;
        }

        if (this.nextHitTime.TryGetValue(target, out float next)
            && (Time.time < next || this.repeatInterval <= 0f))
        {
            return;
        }

        Vector2 direction =
            (other.bounds.center - this.transform.position).normalized;

        target.TakeDamage(new DamageInfo(
            this.amount,
            direction,
            this.team,
            this.melee,
            this,
            this.knockback));

        this.nextHitTime[target] = Time.time + this.repeatInterval;
    }

    private void Forget(Collider2D other)
    {
        if (other.TryGetComponent(out IDamageable target))
        {
            this.nextHitTime.Remove(target);
        }
    }
}