using UnityEngine;

/// <summary>
/// Handles player movement and using tools/weapons.
/// </summary>
[RequireComponent(typeof(CrawlerInputHandler))]
public class CrawlerPlayerHandler : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    // TODO: Weapon and tool variables -> create scripts

    private Rigidbody2D rb;
    private Vector2 movementDirection;

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
        Debug.Log("Attack");
    }

    /// <summary>
    /// Starts using a tool by calling our current tool's use function.
    /// </summary>
    public void UseTool()
    {
        // TODO: Call tool's own use method.
        Debug.Log("Tool");
    }

    private void Start()
    {
        this.rb = this.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Vector2 targetPosition = this.rb.position + (this.movementDirection * this.moveSpeed * Time.fixedDeltaTime);

        this.rb.MovePosition(targetPosition);
    }

}
