using UnityEngine;

/// <summary>
/// Grid math for resizing a selected block. It decides which edges a handle
/// moves, and which source cell fills a cell of the new size. Growing and
/// shrinking both sample the block proportionally, so its content is stretched
/// to the new size instead of being repeated.
/// </summary>
public static class SelectionResize
{
    /// <summary>
    /// Applies a handle drag to a selection, keeping at least one cell on each axis.
    /// </summary>
    /// <param name="source">The cell rectangle of the selection before the drag.</param>
    /// <param name="handle">The handle that is being dragged.</param>
    /// <param name="delta">The drag distance in whole cells.</param>
    /// <returns>The rectangle of the selection after the drag.</returns>
    public static RectInt Resize(
        RectInt source,
        ResizeHandle handle,
        Vector2Int delta)
    {
        int minX = source.xMin;
        int maxX = source.xMax - 1;
        int minY = source.yMin;
        int maxY = source.yMax - 1;

        bool movesMinX = MovesMinX(handle);
        bool movesMaxX = MovesMaxX(handle);
        bool movesMinY = MovesMinY(handle);
        bool movesMaxY = MovesMaxY(handle);

        if (handle == ResizeHandle.Center)
        {
            // The centre keeps the block in the middle, so the drag is split
            // between both edges. The smaller half goes to the min side, which
            // keeps odd changes reachable.
            int halfX = delta.x / 2;
            int halfY = delta.y / 2;

            minX -= delta.x - halfX;
            maxX += halfX;
            minY -= delta.y - halfY;
            maxY += halfY;
        }
        else
        {
            if (movesMinX)
            {
                minX += delta.x;
            }

            if (movesMaxX)
            {
                maxX += delta.x;
            }

            if (movesMinY)
            {
                minY += delta.y;
            }

            if (movesMaxY)
            {
                maxY += delta.y;
            }
        }

        // An edge may not cross the opposite one, so the selection never collapses.
        if (movesMinX || handle == ResizeHandle.Center)
        {
            minX = Mathf.Min(minX, maxX);
        }

        if (movesMaxX || handle == ResizeHandle.Center)
        {
            maxX = Mathf.Max(maxX, minX);
        }

        if (movesMinY || handle == ResizeHandle.Center)
        {
            minY = Mathf.Min(minY, maxY);
        }

        if (movesMaxY || handle == ResizeHandle.Center)
        {
            maxY = Mathf.Max(maxY, minY);
        }

        // Both clamps together can still cross the edges on a small block, so the
        // result is forced to at least one cell on each axis.
        maxX = Mathf.Max(maxX, minX);
        maxY = Mathf.Max(maxY, minY);

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    /// Cuts a drag down so the resized block does not grow beyond a maximum size
    /// on either axis. Shrinking is left alone.
    /// </summary>
    /// <param name="source">The cell rectangle of the selection before the drag.</param>
    /// <param name="handle">The handle that is being dragged.</param>
    /// <param name="delta">The drag distance in whole cells.</param>
    /// <param name="maxSize">The largest cell count either axis may reach.</param>
    /// <returns>The drag distance, limited to the growth that still fits.</returns>
    public static Vector2Int LimitGrowth(
        RectInt source,
        ResizeHandle handle,
        Vector2Int delta,
        int maxSize)
    {
        bool centred = handle == ResizeHandle.Center;

        return new Vector2Int(
            LimitGrowthOnAxis(
                source.width,
                delta.x,
                maxSize,
                MovesMinX(handle),
                MovesMaxX(handle),
                centred),
            LimitGrowthOnAxis(
                source.height,
                delta.y,
                maxSize,
                MovesMinY(handle),
                MovesMaxY(handle),
                centred));
    }

    /// <summary>
    /// Gets the source cell that fills a cell of the resized block on one axis.
    /// The block is sampled proportionally in both directions, so growing
    /// stretches the content over the new size and shrinking drops cells
    /// instead of changing it.
    /// </summary>
    /// <param name="targetIndex">The cell coordinate of the new block.</param>
    /// <param name="targetMin">The lowest cell coordinate of the new block.</param>
    /// <param name="targetSize">The cell count of the new block on this axis.</param>
    /// <param name="sourceMin">The lowest cell coordinate of the selection.</param>
    /// <param name="sourceSize">The cell count of the selection on this axis.</param>
    /// <returns>The cell coordinate of the selection that fills the cell.</returns>
    public static int SourceIndexForTarget(
        int targetIndex,
        int targetMin,
        int targetSize,
        int sourceMin,
        int sourceSize)
    {
        if (sourceSize <= 0 || targetSize <= 0)
        {
            return sourceMin;
        }

        // The middle of the cell is sampled, so a stretch is centered and the
        // content does not lean toward a single edge as the block grows.
        int sampled = (((2 * (targetIndex - targetMin)) + 1) * sourceSize) /
            (2 * targetSize);

        int clamped = Mathf.Clamp(sampled, 0, sourceSize - 1);

        return sourceMin + clamped;
    }

    /// <summary>
    /// Gets the cell a source cell maps to in the new block, so content that
    /// survives can be moved instead of being recreated.
    /// </summary>
    /// <param name="sourceIndex">The cell coordinate in the selection.</param>
    /// <param name="targetMin">The lowest cell coordinate of the new block.</param>
    /// <param name="targetSize">The cell count of the new block on this axis.</param>
    /// <param name="sourceMin">The lowest cell coordinate of the selection.</param>
    /// <param name="sourceSize">The cell count of the selection on this axis.</param>
    /// <param name="targetIndex">The cell the source cell moves to, or 0 when it disappears.</param>
    /// <returns>True when the source cell is still part of the new block.</returns>
    public static bool TryTargetIndexForSource(
        int sourceIndex,
        int targetMin,
        int targetSize,
        int sourceMin,
        int sourceSize,
        out int targetIndex)
    {
        targetIndex = sourceIndex;

        if (sourceSize <= 0 || targetSize <= 0)
        {
            return false;
        }

        int offset = sourceIndex - sourceMin;

        if (offset < 0 || offset >= sourceSize)
        {
            return false;
        }

        int candidate = targetMin + ((offset * targetSize) / sourceSize);

        for (int i = 0; i <= 1; i++)
        {
            int current = candidate + i;

            if (current - targetMin < 0 || current - targetMin >= targetSize)
            {
                continue;
            }

            if (SourceIndexForTarget(
                    current,
                    targetMin,
                    targetSize,
                    sourceMin,
                    sourceSize) == sourceIndex)
            {
                targetIndex = current;
                return true;
            }
        }

        targetIndex = sourceIndex;

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether a handle moves the lowest x edge.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <returns>True when the left edge moves.</returns>
    private static bool MovesMinX(ResizeHandle handle)
    {
        return handle == ResizeHandle.Left ||
            handle == ResizeHandle.TopLeft ||
            handle == ResizeHandle.BottomLeft;
    }

    /// <summary>
    /// Gets a value indicating whether a handle moves the highest x edge.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <returns>True when the right edge moves.</returns>
    private static bool MovesMaxX(ResizeHandle handle)
    {
        return handle == ResizeHandle.Right ||
            handle == ResizeHandle.TopRight ||
            handle == ResizeHandle.BottomRight;
    }

    /// <summary>
    /// Gets a value indicating whether a handle moves the highest y edge.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <returns>True when the top edge moves.</returns>
    private static bool MovesMaxY(ResizeHandle handle)
    {
        return handle == ResizeHandle.Top ||
            handle == ResizeHandle.TopLeft ||
            handle == ResizeHandle.TopRight;
    }

    /// <summary>
    /// Gets a value indicating whether a handle moves the lowest y edge.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <returns>True when the bottom edge moves.</returns>
    private static bool MovesMinY(ResizeHandle handle)
    {
        return handle == ResizeHandle.Bottom ||
            handle == ResizeHandle.BottomLeft ||
            handle == ResizeHandle.BottomRight;
    }

    /// <summary>
    /// Cuts the drag on one axis down to the growth that still fits.
    /// </summary>
    /// <param name="sourceSize">The cell count of the selection on this axis.</param>
    /// <param name="delta">The drag distance on this axis.</param>
    /// <param name="maxSize">The largest cell count this axis may reach.</param>
    /// <param name="movesMin">Whether the handle moves the low edge.</param>
    /// <param name="movesMax">Whether the handle moves the high edge.</param>
    /// <param name="centred">Whether the centre handle is dragged.</param>
    /// <returns>The allowed drag distance.</returns>
    private static int LimitGrowthOnAxis(
        int sourceSize,
        int delta,
        int maxSize,
        bool movesMin,
        bool movesMax,
        bool centred)
    {
        int allowed = Mathf.Max(0, maxSize - sourceSize);

        // The centre and the high edge grow with a positive drag, the low edge
        // grows with a negative one. Shrinking never needs a limit.
        if (centred || movesMax)
        {
            delta = Mathf.Min(delta, allowed);
        }

        if (!centred && movesMin)
        {
            delta = Mathf.Max(delta, -allowed);
        }

        return delta;
    }
}
