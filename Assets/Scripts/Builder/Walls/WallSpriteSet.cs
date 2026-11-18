
using System;
using UnityEngine;

/// <summary>
/// The kind of corner a wall forms with its neighbours.
/// </summary>
public enum WallCornerKind
{
    /// <summary>
    /// The wall is not a single corner.
    /// </summary>
    None = 0,

    /// <summary>
    /// Two adjacent sides connect and the diagonal between them is a wall too,
    /// so the outline turns around a corner of a solid mass.
    /// </summary>
    Outer = 1,

    /// <summary>
    /// The outline turns into a notch. Either two adjacent sides connect and
    /// the diagonal between them is empty, or all four sides connect and
    /// exactly one diagonal is empty.
    /// </summary>
    Inner = 2,
}

[CreateAssetMenu(fileName = "WallSpriteSet", menuName = "Dungeon/Wall Sprite Set")]
public class WallSpriteSet : ScriptableObject
{
    private const string ResourcePath = "WallSpriteSet";
    private const int SideCount = 4;
    private const int FullMask = 15;

    private static WallSpriteSet instance;
    private static bool warned;

    [Tooltip("Used when there are no neighbours. Rotated by the wall orientation.")]
    [SerializeField]
    private Sprite isolated;

    [SerializeField]
    private WallShape[] shapes = Array.Empty<WallShape>();

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
                WallSpriteSet[] all = Resources.LoadAll<WallSpriteSet>(string.Empty);

                if (all.Length > 0)
                {
                    instance = all[0];
                }
            }

            if (instance == null && !warned)
            {
                warned = true;
                Debug.LogError("No WallSpriteSet asset found in a Resources folder.");
            }

            return instance;
        }
    }

    /// <summary>
    /// Works out whether the neighbours form a single corner and, if so, the
    /// rotation for the corner sprite. Three situations count:
    /// two adjacent sides connect and the diagonal between them is a wall
    /// (outer corner); two adjacent sides connect and that diagonal is empty
    /// (inner corner); all four sides connect and exactly one diagonal is
    /// empty (inner corner). The caller chooses the matching outer or inner
    /// corner sprite. Both sprites use the rotation of the two-sided corner
    /// shape in this set, without an additional rotation for inner corners.
    /// </summary>
    /// <param name="connections">The sides that connect to another wall.</param>
    /// <param name="corners">The diagonals that contain a wall.</param>
    /// <param name="orientation">The orientation of the wall.</param>
    /// <param name="kind">The kind of corner, or None when there is no corner.</param>
    /// <param name="rotation">The rotation for the corner sprite.</param>
    /// <returns>True when the neighbours form a corner and a rotation was found.</returns>
    public bool TryGetCornerRotation(
        WallConnections connections,
        WallCorners corners,
        WallOrientation orientation,
        out WallCornerKind kind,
        out Quaternion rotation)
    {
        kind = WallCornerKind.None;
        rotation = Quaternion.identity;

        int edges = (int)connections & FullMask;
        int diagonals = (int)corners & FullMask;

        if (!TryClassifyCorner(
                edges,
                diagonals,
                out int cornerBit,
                out WallCornerKind foundKind))
        {
            return false;
        }

        bool found;

        if (edges == FullMask)
        {
            // Surrounded on all sides: borrow the rotation of the two-sided
            // corner that sits around the empty diagonal. The empty-diagonal
            // shape is tried first, then the filled one.
            WallConnections cornerEdges =
                (WallConnections)((1 << cornerBit) |
                                  (1 << ((cornerBit + 1) % SideCount)));

            WallCorners cornerDiagonal = (WallCorners)(1 << cornerBit);

            found =
                this.TryGetOverlay(
                    cornerEdges,
                    WallCorners.None,
                    orientation,
                    out _,
                    out rotation) ||
                this.TryGetOverlay(
                    cornerEdges,
                    cornerDiagonal,
                    orientation,
                    out _,
                    out rotation);
        }
        else
        {
            found =
                this.TryGetOverlay(
                    connections,
                    corners,
                    orientation,
                    out _,
                    out rotation);
        }

        if (!found)
        {
            rotation = Quaternion.identity;
            return false;
        }

        kind = foundKind;
        return true;
    }

    /// <summary>
    /// Picks the overlay sprite and its rotation. Connections always win; the
    /// orientation is only used when there are no neighbours.
    /// </summary>
    public bool TryGetOverlay(
        WallConnections connections,
        WallCorners corners,
        WallOrientation orientation,
        out Sprite sprite,
        out Quaternion rotation)
    {
        int edges = (int)connections & FullMask;

        if (edges == 0)
        {
            sprite = this.isolated;
            rotation = RotationFromSteps((int)orientation);
            return sprite != null;
        }

        int effective = EffectiveCorners(edges, (int)corners);

        // Pass 0: shapes that care about the diagonals.
        // Pass 1: shapes that ignore them (fallback).
        for (int pass = 0; pass < 2; pass++)
        {
            bool wantIgnore = pass == 1;

            foreach (WallShape shape in this.shapes)
            {
                if (shape == null ||
                    shape.sprite == null ||
                    shape.ignoreCorners != wantIgnore)
                {
                    continue;
                }

                int refEdges = (int)shape.edges & FullMask;
                int refCorners = EffectiveCorners(refEdges, (int)shape.corners);

                for (int steps = 0; steps < SideCount; steps++)
                {
                    if (Rotate(refEdges, steps) != edges)
                    {
                        continue;
                    }

                    if (!shape.ignoreCorners &&
                        Rotate(refCorners, steps) != effective)
                    {
                        continue;
                    }

                    sprite = shape.sprite;
                    rotation = RotationFromSteps(steps);
                    return true;
                }
            }
        }

        sprite = null;
        rotation = Quaternion.identity;
        return false;
    }

    // Corner bit i sits between edge bit i and edge bit i + 1, and the
    // reference corner (Up + Right) is bit 0.
    private static bool TryClassifyCorner(
        int edges,
        int diagonals,
        out int cornerBit,
        out WallCornerKind kind)
    {
        cornerBit = 0;
        kind = WallCornerKind.None;

        switch (edges)
        {
            case 3:  // Up + Right
                cornerBit = 0;
                break;
            case 6:  // Right + Down
                cornerBit = 1;
                break;
            case 12: // Down + Left
                cornerBit = 2;
                break;
            case 9:  // Left + Up
                cornerBit = 3;
                break;
            case FullMask:
                // Surrounded: only a single empty diagonal is a corner.
                int missing = ~diagonals & FullMask;

                for (int i = 0; i < SideCount; i++)
                {
                    if (missing == (1 << i))
                    {
                        cornerBit = i;
                        kind = WallCornerKind.Inner;
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }

        kind = (diagonals & (1 << cornerBit)) != 0
            ? WallCornerKind.Outer
            : WallCornerKind.Inner;

        return true;
    }

    // A diagonal only counts when both sides next to it are connected.
    // Corner bit i sits between edge bit i and edge bit i + 1.
    private static int EffectiveCorners(int edges, int corners)
    {
        int result = 0;

        for (int i = 0; i < SideCount; i++)
        {
            bool sideA = (edges & (1 << i)) != 0;
            bool sideB = (edges & (1 << ((i + 1) % SideCount))) != 0;

            if (sideA && sideB && (corners & (1 << i)) != 0)
            {
                result |= 1 << i;
            }
        }

        return result;
    }

    // Rotates a 4-bit mask clockwise.
    private static int Rotate(int mask, int steps)
    {
        return ((mask << steps) | (mask >> (SideCount - steps))) & FullMask;
    }

    private static Quaternion RotationFromSteps(int steps)
    {
        return Quaternion.Euler(0f, 0f, -90f * steps);
    }

    [Serializable]
    public class WallShape
    {
        public string name;

        [Tooltip("Edges that connect in the drawing.")]
        public WallConnections edges;

        [Tooltip("Diagonal walls that are present in the drawing. Only matters for diagonals between two connected edges.")]
        public WallCorners corners;

        [Tooltip("Use this sprite whatever the diagonals are, unless a more exact shape exists.")]
        public bool ignoreCorners;

        public Sprite sprite;
    }
}
