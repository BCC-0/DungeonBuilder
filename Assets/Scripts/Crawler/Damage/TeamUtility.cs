using UnityEngine;

/// <summary>
/// A utility that decides which source team can damage which target team.
/// This allows making damage more complicated then using layers or any other way.
/// </summary>
public static class TeamUtility
{
    /// <summary>
    /// Determines whether a source of the given team can damage a target of the given team.
    /// </summary>
    /// <param name="source">The team of the damage source.</param>
    /// <param name="target">The team of the target.</param>
    /// <returns>Whether the target can be damaged.</returns>
    public static bool CanDamage(Team source, Team target)
    {
        return source == Team.Neutral || source != target;
    }
}