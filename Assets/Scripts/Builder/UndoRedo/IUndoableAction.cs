using UnityEngine;

/// <summary>
/// Represents an editor operation that can be undone and redone.
/// </summary>
public interface IUndoableAction
{
    /// <summary>
    /// Reverts the operation.
    /// </summary>
    void Undo();

    /// <summary>
    /// Reapplies the operation.
    /// </summary>
    void Redo();
}
