using UnityEngine;

/// <summary>
/// The sprites of one wall type. Each field is named after the neighbours the
/// wall connects to, for example UpRight is a corner with walls above and to
/// the right of it.
/// </summary>
[CreateAssetMenu(fileName = "WallSpriteSet", menuName = "Dungeon/Wall Sprite Set")]
public class WallSpriteSet : ScriptableObject
{
    [Header("No neighbours (chosen by orientation)")]
    [SerializeField]
    private Sprite isolatedUp;

    [SerializeField]
    private Sprite isolatedRight;

    [SerializeField]
    private Sprite isolatedDown;

    [SerializeField]
    private Sprite isolatedLeft;

    [Header("One neighbour (end pieces)")]
    [SerializeField]
    private Sprite connectedUp;

    [SerializeField]
    private Sprite connectedRight;

    [SerializeField]
    private Sprite connectedDown;

    [SerializeField]
    private Sprite connectedLeft;

    [Header("Two neighbours (straight)")]
    [SerializeField]
    private Sprite upDown;

    [SerializeField]
    private Sprite leftRight;

    [Header("Two neighbours (corners)")]
    [SerializeField]
    private Sprite upRight;

    [SerializeField]
    private Sprite rightDown;

    [SerializeField]
    private Sprite downLeft;

    [SerializeField]
    private Sprite leftUp;

    [Header("Three neighbours (T-junctions)")]
    [SerializeField]
    private Sprite upRightDown;

    [SerializeField]
    private Sprite rightDownLeft;

    [SerializeField]
    private Sprite downLeftUp;

    [SerializeField]
    private Sprite leftUpRight;

    [Header("Four neighbours (fill)")]
    [SerializeField]
    private Sprite allSides;

    /// <summary>
    /// Gets the sprite for a wall.
    /// </summary>
    /// <param name="connections">The sides that connect to another wall.</param>
    /// <param name="orientation">The orientation to use when there are no connections.</param>
    /// <returns>The sprite, or null when the set has no sprite for this case.</returns>
    public Sprite GetSprite(
        WallConnections connections,
        WallOrientation orientation)
    {
        switch (connections)
        {
            case WallConnections.None:
                return this.GetIsolatedSprite(orientation);
            case WallConnections.Up:
                return this.connectedUp;
            case WallConnections.Right:
                return this.connectedRight;
            case WallConnections.Down:
                return this.connectedDown;
            case WallConnections.Left:
                return this.connectedLeft;
            case WallConnections.Up | WallConnections.Down:
                return this.upDown;
            case WallConnections.Left | WallConnections.Right:
                return this.leftRight;
            case WallConnections.Up | WallConnections.Right:
                return this.upRight;
            case WallConnections.Right | WallConnections.Down:
                return this.rightDown;
            case WallConnections.Down | WallConnections.Left:
                return this.downLeft;
            case WallConnections.Left | WallConnections.Up:
                return this.leftUp;
            case WallConnections.Up | WallConnections.Right | WallConnections.Down:
                return this.upRightDown;
            case WallConnections.Right | WallConnections.Down | WallConnections.Left:
                return this.rightDownLeft;
            case WallConnections.Down | WallConnections.Left | WallConnections.Up:
                return this.downLeftUp;
            case WallConnections.Left | WallConnections.Up | WallConnections.Right:
                return this.leftUpRight;
            case WallConnections.All:
                return this.allSides;
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets the sprite of a wall without neighbours.
    /// </summary>
    /// <param name="orientation">The orientation of the wall.</param>
    /// <returns>The sprite for the orientation.</returns>
    private Sprite GetIsolatedSprite(WallOrientation orientation)
    {
        switch (orientation)
        {
            case WallOrientation.Up:
                return this.isolatedUp;
            case WallOrientation.Right:
                return this.isolatedRight;
            case WallOrientation.Left:
                return this.isolatedLeft;
            default:
                return this.isolatedDown;
        }
    }
}
