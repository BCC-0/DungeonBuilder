using System;

/// <summary>
/// The direction a wall faces. It is used for the sprite when a wall has no
/// neighbours to connect to.
/// </summary>
public enum WallOrientation
{
    Up = 0,
    Right = 1,
    Down = 2,
    Left = 3,
}

/// <summary>
/// The sides of a wall that connect to another wall. The values are the
/// 4-bit mask used to pick a sprite.
/// </summary>
[Flags]
public enum WallConnections
{
    None = 0,
    Up = 1,
    Right = 2,
    Down = 4,
    Left = 8,
    All = Up | Right | Down | Left,
}

/// <summary>
/// The diagonal neighbours of a wall, in clockwise order so they rotate
/// the same way as WallConnections.
/// </summary>
[Flags]
public enum WallCorners
{
    None = 0,
    UpRight = 1,
    DownRight = 2,
    DownLeft = 4,
    UpLeft = 8,
}
