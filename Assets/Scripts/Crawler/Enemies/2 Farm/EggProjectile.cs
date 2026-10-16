using UnityEngine;

/// <summary>
/// An egg shot by a chicken. It flies in a straight line, damages the player
/// on contact and disappears when it hits something solid or runs out of time.
/// </summary>
public class EggProjectile : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D rb;
    [SerializeField]
    private float lifetime = 5f;

    private EnemyBody owner;
    private int damage;

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
        this.damage = damageAmount;
        this.rb.linearVelocity = direction * speed;
    }

    private void Awake()
    {
        if (this.rb == null)
        {
            this.rb = this.GetComponent<Rigidbody2D>();
        }

        Destroy(this.gameObject, this.lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Eggs fly through enemies.
        if (other.GetComponentInParent<EnemyBody>() != null)
        {
            return;
        }

        CrawlerPlayerData player = other.GetComponentInParent<CrawlerPlayerData>();

        if (player != null)
        {
            if (this.owner != null)
            {
                this.owner.DamagePlayer(player, this.damage);
            }
            else
            {
                // The chicken died while the egg was in the air.
                player.TakeDamage(this.damage);
            }

            Destroy(this.gameObject);
            return;
        }

        // Break on walls and other solid things.
        if (!other.isTrigger)
        {
            Destroy(this.gameObject);
        }
    }
}