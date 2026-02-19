using UnityEngine;

/// <summary>
/// Handles player movement and using tools/weapons.
/// </summary>
[RequireComponent(typeof(CrawlerInputHandler))]
[RequireComponent(typeof(CrawlerPlayerData))]
public class CrawlerPlayerHandler : MonoBehaviour
{
    private CrawlerPlayerData playerData;

    private Rigidbody2D rb;
    private Vector2 movementDirection;

    private bool canMove = true;

    private Animator animator;

    /// <summary>
    /// Gets a value indicating whether the player can currently move.
    /// </summary>
    public bool CanMove => this.canMove;

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
        this.playerData.UseWeapon();
    }

    /// <summary>
    /// Starts using a tool by calling our current tool's use function.
    /// </summary>
    public void UseTool()
    {
        this.playerData.UseTool();
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
    /// Currently only used for pausing the game (inventory).
    /// </summary>
    /// <param name="canMove">Whether the player can move after this.</param>
    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
    }

    private void Start()
    {
        this.rb = this.GetComponent<Rigidbody2D>();
        this.playerData = this.GetComponent<CrawlerPlayerData>();
        this.animator = this.GetComponent<Animator>();
    }

    private void Update()
    {
        this.UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Vector2 targetPosition = this.rb.position + (this.movementDirection * this.playerData.MoveSpeed * Time.fixedDeltaTime);

        this.rb.MovePosition(targetPosition);
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

        // TODO: Add damage
    }

    private void UpdateAnimator()
    {
        if (!this.canMove)
        {
            return;
        }

        if (this.movementDirection != Vector2.zero)
        {
            Vector2 dir = this.movementDirection.normalized;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                dir.y = 0;
            }
            else
            {
                dir.x = 0;
            }

            this.animator.SetFloat("MoveX", dir.x);
            this.animator.SetFloat("MoveY", dir.y);
        }

        float speed = this.movementDirection.sqrMagnitude > 0f ? 1f : 0f;
        this.animator.SetFloat("Speed", speed);
    }
}
