using System;

/// <summary>
/// The direction a wall faces. It is used for the sprite when a wall has no
/// neighbours to connect to.
/// </summary>
public enum WallOrientation
{
    /// <summary>
    /// The Up orientation of a wall.
    /// </summary>
    Up = 0,

    /// <summary>
    /// The Right orientation of a wall.
    /// </summary>
    Right = 1,

    /// <summary>
    /// The Down orientation of a wall.
    /// </summary>
    Down = 2,

    /// <summary>
    /// The Left orientation of a wall.
    /// </summary>
    Left = 3,
}

/// <summary>
/// The sides of a wall that connect to another wall. The values are the
/// 4-bit mask used to pick a sprite.
/// </summary>
[Flags]
public enum WallConnections
{
    /// <summary>
    /// This wall has no connections.
    /// </summary>
    None = 0,

    /// <summary>
    /// This wall has an up connection.
    /// </summary>
    Up = 1,

    /// <summary>
    /// This wall has a right connection.
    /// </summary>
    Right = 2,

    /// <summary>
    /// This wall has a down connection.
    /// </summary>
    Down = 4,

    /// <summary>
    /// This wall has a left connection.
    /// </summary>
    Left = 8,

    /// <summary>
    /// This wall has all connections.
    /// </summary>
    All = Up | Right | Down | Left,
}

/// <summary>
/// The diagonal neighbours of a wall, in clockwise order so they rotate
/// the same way as WallConnections.
/// </summary>
[Flags]
public enum WallCorners
{
    /// <summary>
    /// This wall is attached to no corners.
    /// </summary>
    None = 0,

    /// <summary>
    /// This wall is attached to a corner up-right.
    /// </summary>
    UpRight = 1,

    /// <summary>
    /// This wall is attached to a corner down-right.
    /// </summary>
    DownRight = 2,

    /// <summary>
    /// This wall is attached to a corner down-left.
    /// </summary>
    DownLeft = 4,

    /// <summary>
    /// This wall is attached to a corner up-left.
    /// </summary>
    UpLeft = 8,
}
