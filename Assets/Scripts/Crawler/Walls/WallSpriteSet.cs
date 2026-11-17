using System;
using UnityEngine;

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
    /// Picks the overlay sprite and rotation. Connections always win; the
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