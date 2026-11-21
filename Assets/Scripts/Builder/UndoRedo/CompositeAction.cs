
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Groups several actions so they are undone and redone as a single step.
/// </summary>
public class CompositeAction : IUndoableAction
{
    private readonly List<IUndoableAction> actions = new ();

    /// <summary>
    /// Gets a value indicating whether the composite contains no actions.
    /// </summary>
    public bool IsEmpty => this.actions.Count == 0;

    /// <summary>
    /// Gets the number of grouped actions.
    /// </summary>
    public int Count => this.actions.Count;

    /// <summary>
    /// Adds an action to the group. The action should already have been applied.
    /// </summary>
    /// <param name="action">The action to add.</param>
    public void Add(IUndoableAction action)
    {
        if (action != null)
        {
            this.actions.Add(action);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        // Reverse order so the last change is reverted first.
        for (int i = this.actions.Count - 1; i >= 0; i--)
        {
            this.actions[i].Undo();
        }
    }

    /// <inheritdoc/>
    public void Redo()
    {
        for (int i = 0; i < this.actions.Count; i++)
        {
            this.actions[i].Redo();
        }
    }
}
