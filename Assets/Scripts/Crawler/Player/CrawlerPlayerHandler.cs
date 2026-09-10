using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player movement and using tools/weapons.
/// </summary>
[RequireComponent(typeof(CrawlerInputHandler))]
[RequireComponent(typeof(CrawlerPlayerData))]
public class CrawlerPlayerHandler : MonoBehaviour
{
    private CrawlerPlayerData playerData;

    private Rigidbody2D rigidbody2d;
    private Vector2 movementDirection;

    private int movementLockCount = 0;
    private Vector2 lastFacingDirection = Vector2.down;

    private Animator animator;
    private AnimatorOverrideController overrideController;

    private HashSet<RepeatingDamage> activeRepeatingDamages = new HashSet<RepeatingDamage>();

    private Dictionary<RepeatingDamage, Coroutine> repeatingDamageCoroutines = new Dictionary<RepeatingDamage, Coroutine>();

    /// <summary>
    /// Gets the last direction this player was facing.
    /// </summary>
    public Vector2 LastFacingDirection => this.lastFacingDirection;

    /// <summary>
    /// Gets a value indicating whether the player can currently move.
    /// </summary>
    public bool CanMove => this.movementLockCount <= 0;

    /// <summary>
    /// Sets the current movement direction of the player.
    /// </summary>
    /// <param name="move">The movement direction.</param>
    public void SetMovement(Vector2 move)
    {
        this.movementDirection = move.normalized;
    }

    /// <summary>
    /// Interact with the object at the interaction vector.
    /// </summary>
    /// <param name="interaction">Location of the object we want to interact with.</param>
    public void Interact(Vector2 interaction)
    {
        // TODO: Call interaction of object.
        // First find object using the map's find at location!
        Debug.Log("Interacted with " + interaction);
    }

    /// <summary>
    /// Starts an attack by calling our current weapon's attack function.
    /// </summary>
    public void Attack()
    {
        if (this.CanMove)
        {
            this.playerData.UseWeapon(this);
        }
    }

    /// <summary>
    /// Starts using a tool by calling our current tool's use function.
    /// </summary>
    public void UseTool()
    {
        if (this.CanMove)
        {
            this.playerData.UseTool(this);
        }
    }

    /// <summary>
    /// Opens or closes the inventory.
    /// </summary>
    /// <returns>Returns whether the inventory has opened after calling.</returns>
    public bool OpenInventory()
    {
        // TODO: Check here if we can open the inventory.
        bool open = this.playerData.OpenInventory();

        this.SetCanMove(!open);
        return open;
    }

    /// <summary>
    /// Sets whether the player can do anything.
    /// Used for pausing, animating, stuns, etc.
    /// </summary>
    /// <param name="canMove">Whether the player can move after this.</param>
    public void SetCanMove(bool canMove)
    {
        if (canMove)
        {
            // Removes this lock.
            this.movementLockCount--;
            return;
        }

        // Adds a lock, since the player shouldn't move now.
        this.movementLockCount++;
    }

    /// <summary>
    /// Plays a weapon/tool animation dynamically via override controller.
    /// </summary>
    /// <param name="clip">The clip to set.</param>
    public void PlayUseAnimation(AnimationClip clip)
    {
        this.overrideController["Use"] = clip;

        // this.animator.Update(0f);
        this.animator.SetTrigger("Use");
    }

    /// <summary>
    /// Pauses the player's movement for the given time.
    /// </summary>
    /// <param name="time">The time to pause the player.</param>
    public void PausePlayerMovement(float time)
    {
        this.StartCoroutine(this.PlayerPauser(time));
    }

    private IEnumerator PlayerPauser(float time)
    {
        this.SetCanMove(false);
        yield return new WaitForSeconds(time);
        this.SetCanMove(true);
    }

    private void Start()
    {
        this.rigidbody2d = this.GetComponent<Rigidbody2D>();
        this.playerData = this.GetComponent<CrawlerPlayerData>();
        this.animator = this.GetComponent<Animator>();

        this.overrideController = new AnimatorOverrideController(this.animator.runtimeAnimatorController);
        this.animator.runtimeAnimatorController = this.overrideController;
    }

    private void Update()
    {
        this.UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (this.CanMove)
        {
            Vector2 targetPosition = this.rigidbody2d.position + (this.movementDirection * this.playerData.MoveSpeed * Time.fixedDeltaTime);

            this.rigidbody2d.MovePosition(targetPosition);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject gameObject = collision.gameObject;

        if (gameObject.CompareTag("Item"))
        {
            ItemObject itemObject = gameObject.GetComponent<ItemObject>();
            if (itemObject == null)
            {
                Debug.LogError("Item object doesn't include ItemObject script!");
                return;
            }

            // TODO: Add get item animation!
            this.playerData.AddToInventory(itemObject.Item);
            Destroy(gameObject);
        }

        if (gameObject.layer == 7)
        {
            Damage damage = gameObject.GetComponent<Damage>();

            if (damage == null)
            {
                return;
            }

            this.playerData.TakeDamage(damage.Amount);

            if (damage is RepeatingDamage repeatingDamage)
            {
                this.StartRepeatingDamage(repeatingDamage);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer != 7)
        {
            return;
        }

        RepeatingDamage repeatingDamage =
            collision.gameObject.GetComponent<RepeatingDamage>();

        if (repeatingDamage != null)
        {
            this.StopRepeatingDamage(repeatingDamage);
        }
    }

    private void StartRepeatingDamage(RepeatingDamage damage)
    {
        if (damage == null)
        {
            return;
        }

        if (!this.activeRepeatingDamages.Add(damage))
        {
            return;
        }

        Coroutine coroutine = this.StartCoroutine(this.ApplyRepeatingDamage(damage));

        this.repeatingDamageCoroutines.Add(damage, coroutine);
    }

    private void StopRepeatingDamage(RepeatingDamage damage)
    {
        if (damage == null)
        {
            return;
        }

        this.activeRepeatingDamages.Remove(damage);

        if (this.repeatingDamageCoroutines.TryGetValue(damage, out Coroutine coroutine))
        {
            this.StopCoroutine(coroutine);
            this.repeatingDamageCoroutines.Remove(damage);
        }
    }

    private IEnumerator ApplyRepeatingDamage(RepeatingDamage damage)
    {
        this.playerData.TakeDamage(damage.RepeatingAmount);

        while (this.activeRepeatingDamages.Contains(damage))
        {
            yield return new WaitForSeconds(damage.RepeatInterval);

            if (!this.activeRepeatingDamages.Contains(damage))
            {
                yield break;
            }

            this.playerData.TakeDamage(damage.RepeatingAmount);
        }
    }

    private void UpdateAnimator()
    {
        if (!this.CanMove)
        {
            return;
        }

        Vector2 dir = this.movementDirection;

        if (dir != Vector2.zero)
        {
            this.lastFacingDirection = dir;
        }

        this.animator.SetFloat("MoveX", this.lastFacingDirection.x);
        this.animator.SetFloat("MoveY", this.lastFacingDirection.y);

        this.animator.SetFloat("Speed", dir.sqrMagnitude > 0 ? 1f : 0f);
    }
}
