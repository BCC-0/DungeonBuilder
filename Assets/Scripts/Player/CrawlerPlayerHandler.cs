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

    private bool canMove;

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
        this.movementDirection = move;
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
        return this.playerData.OpenInventory();
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
}
