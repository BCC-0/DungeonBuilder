using UnityEngine;

/// <summary>
/// The overlay sprites shared by all walls. There are 16 sprites, one for each
/// combination of connections, drawn for orientation Up and stored at the
/// flag value of WallConnections. Other orientations rotate the overlay.
/// Orientations are ordered Up, Right, Down, Left.
/// </summary>
[CreateAssetMenu(fileName = "WallSpriteSet", menuName = "Dungeon/Wall Sprite Set")]
public class WallSpriteSet : ScriptableObject
{
    private const string ResourcePath = "WallSpriteSet";
    private const int SideCount = 4;
    private const int ConnectionCount = 16;

    private static WallSpriteSet instance;
    private static bool warned;

    [SerializeField]
    private Sprite[] overlays = new Sprite[ConnectionCount];

    /// <summary>
    /// Gets the shared sprite set, loaded from the Resources folder. The
    /// asset is found by its name first, and otherwise any WallSpriteSet
    /// asset in a Resources folder is used.
    /// </summary>
    public static WallSpriteSet Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<WallSpriteSet>(ResourcePath);
            }

            if (instance == null)
            {
                WallSpriteSet[] all =
                    Resources.LoadAll<WallSpriteSet>(string.Empty);

                if (all.Length > 0)
                {
                    instance = all[0];
                }
            }

            if (instance == null &&
                !warned)
            {
                warned = true;

                Debug.LogError(
                    "No WallSpriteSet asset found. It must be a " +
                    "WallSpriteSet (Create > Dungeon > Wall Sprite Set) " +
                    "inside a folder named 'Resources'.");
            }

            return instance;
        }
    }

    /// <summary>
    /// Gets the rotation to apply to the overlay for an orientation.
    /// </summary>
    /// <param name="orientation">The orientation of the wall.</param>
    /// <returns>The clockwise rotation for the orientation.</returns>
    public static Quaternion GetRotation(WallOrientation orientation)
    {
        return Quaternion.Euler(0f, 0f, -90f * (int)orientation);
    }

    /// <summary>
    /// Gets the overlay sprite for a wall. The sprites are drawn for
    /// orientation Up, so the connections are rotated back into that frame.
    /// </summary>
    /// <param name="connections">The sides that connect to another wall.</param>
    /// <param name="orientation">The orientation of the wall.</param>
    /// <returns>The sprite, or null when the set has no sprite for this case.</returns>
    public Sprite GetOverlay(
        WallConnections connections,
        WallOrientation orientation)
    {
        int steps = (int)orientation;
        int mask = (int)connections & (ConnectionCount - 1);

        int index =
            ((mask >> steps) | (mask << (SideCount - steps))) &
            (ConnectionCount - 1);

        if (this.overlays == null ||
            index >= this.overlays.Length)
        {
            return null;
        }

        return this.overlays[index];
    }
}