using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger hitbox that damages each target once per activation.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WeaponHitbox : MonoBehaviour
{
    private readonly HashSet<Collider2D> alreadyHit = new ();

    [SerializeField]
    private Collider2D hitCollider;
    private DamageInfo damage;
    private Coroutine activeRoutine;

    /// <summary>
    /// Enables the hitbox for a duration.
    /// </summary>
    /// <param name="damageInfo">The damage to apply to targets.</param>
    /// <param name="duration">How long the hitbox stays active.</param>
    public void Activate(DamageInfo damageInfo, float duration)
    {
        if (this.activeRoutine != null)
        {
            this.StopCoroutine(this.activeRoutine);
        }

        this.damage = damageInfo;
        this.alreadyHit.Clear();
        this.hitCollider.enabled = true;
        this.activeRoutine = this.StartCoroutine(this.DeactivateAfter(duration));
    }

    private void Awake()
    {
        this.hitCollider = this.GetComponent<Collider2D>();
        this.hitCollider.isTrigger = true;
        this.hitCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!this.alreadyHit.Add(other))
        {
            return;
        }

        if (other.TryGetComponent(out IDamageable target) && TeamUtility.CanDamage(this.damage.Team, target.Team))
        {
            target.TakeDamage(this.damage);
        }
    }

    private IEnumerator DeactivateAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        this.hitCollider.enabled = false;
        this.activeRoutine = null;
    }
}