using System.Collections;
using UnityEngine;

/// <summary>
/// An egg shot by a chicken. It flies in a straight line, damages the player
/// on contact and plays its break effects when destroyed.
/// </summary>
public class EggProjectile : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private Collider2D eggCollider;

    [SerializeField]
    private Renderer eggRenderer;

    [SerializeField]
    private ParticleSystem eggBreakEffect;

    [SerializeField]
    private ParticleSystem eggYolkEffect;

    [SerializeField]
    private float lifetime = 5f;

    private EnemyBody owner;
    private bool destroying;

    /// <summary>
    /// Launches the egg.
    /// </summary>
    /// <param name="shooter">The enemy that shot the egg, so its damage multiplier applies.</param>
    /// <param name="direction">The direction to fly in.</param>
    /// <param name="speed">The speed of the egg.</param>
    /// <param name="damageAmount">The damage before scaling by the shooter.</param>
    public void Initialize(EnemyBody shooter, Vector2 direction, float speed, int damageAmount)
    {
        this.owner = shooter;
        this.rb.linearVelocity = direction * speed;
        this.GetComponent<DamageSource>().Amount = damageAmount * shooter.DamageMultiplier;
    }

    /// <summary>
    /// Initializes the projectile components and starts the lifetime timer.
    /// </summary>
    private void Awake()
    {
        if (this.rb == null)
        {
            this.rb = this.GetComponent<Rigidbody2D>();
        }

        if (this.eggCollider == null)
        {
            this.eggCollider = this.GetComponent<Collider2D>();
        }

        if (this.eggRenderer == null)
        {
            this.eggRenderer = this.GetComponent<Renderer>();
        }

        this.StartCoroutine(this.LifetimeRoutine());
    }

    /// <summary>
    /// Starts the egg destruction sequence when it collides with another object.
    /// </summary>
    /// <param name="collision">The collision involving the egg.</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        this.DestroyEgg();
    }

    /// <summary>
    /// Disables the egg's physical interactions and rendering, then plays its destruction effects.
    /// </summary>
    private void DestroyEgg()
    {
        if (this.destroying)
        {
            return;
        }

        this.destroying = true;

        if (this.eggCollider != null)
        {
            this.eggCollider.enabled = false;
        }

        if (this.rb != null)
        {
            this.rb.linearVelocity = Vector2.zero;
            this.rb.simulated = false;
        }

        if (this.eggRenderer != null)
        {
            this.eggRenderer.enabled = false;
        }

        this.StartCoroutine(this.PlayDestroyEffects());
    }

    /// <summary>
    /// Plays both particle effects and destroys the egg after they finish.
    /// </summary>
    /// <returns>An enumerator for the destruction effect coroutine.</returns>
    private IEnumerator PlayDestroyEffects()
    {
        if (this.eggBreakEffect != null)
        {
            this.eggBreakEffect.Play();
        }

        if (this.eggYolkEffect != null)
        {
            this.eggYolkEffect.Play();
        }

        while ((this.eggBreakEffect != null && this.eggBreakEffect.IsAlive()) ||
               (this.eggYolkEffect != null && this.eggYolkEffect.IsAlive()))
        {
            yield return null;
        }

        Destroy(this.gameObject);
    }

    /// <summary>
    /// Starts the egg destruction sequence when its lifetime expires.
    /// </summary>
    /// <returns>An enumerator for the lifetime coroutine.</returns>
    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(this.lifetime);

        this.DestroyEgg();
    }
}