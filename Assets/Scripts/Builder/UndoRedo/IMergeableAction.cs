/// <summary>
/// An undoable action that can absorb a directly following action,
/// so that many small changes (for example a slider drag) become one entry.
/// </summary>
public interface IMergeableAction
{
    /// <summary>
    /// Tries to merge the next registered action into this one.
    /// </summary>
    /// <param name="next">The action that was registered after this one.</param>
    /// <returns>True when the action was absorbed and should not be pushed separately.</returns>
    bool TryMerge(IUndoableAction next);
}
