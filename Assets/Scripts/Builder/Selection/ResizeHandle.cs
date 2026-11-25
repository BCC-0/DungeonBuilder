/// <summary>
/// The grab points of the selection resize box. A side handle moves one edge,
/// a corner moves two and the centre moves all four around the block's middle.
/// </summary>
public enum ResizeHandle
{
    /// <summary>
    /// No handle is being used.
    /// </summary>
    None,

    /// <summary>
    /// The handle in the middle of the left edge.
    /// </summary>
    Left,

    /// <summary>
    /// The handle in the middle of the right edge.
    /// </summary>
    Right,

    /// <summary>
    /// The handle in the middle of the top edge.
    /// </summary>
    Top,

    /// <summary>
    /// The handle in the middle of the bottom edge.
    /// </summary>
    Bottom,

    /// <summary>
    /// The handle in the top left corner.
    /// </summary>
    TopLeft,

    /// <summary>
    /// The handle in the top right corner.
    /// </summary>
    TopRight,

    /// <summary>
    /// The handle in the bottom left corner.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// The handle in the bottom right corner.
    /// </summary>
    BottomRight,

    /// <summary>
    /// The handle in the middle of the selection, which resizes in every direction.
    /// </summary>
    Center,
}
