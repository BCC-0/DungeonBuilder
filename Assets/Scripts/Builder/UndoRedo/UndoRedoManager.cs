using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager for editor undo and redo operations.
/// </summary>
public class UndoRedoManager : MonoBehaviour
{
    private readonly Stack<IUndoableAction> undoStack = new ();
    private readonly Stack<IUndoableAction> redoStack = new ();

    private readonly List<IUndoableAction> actionLog = new ();

    private CompositeAction openTransaction;
    private int transactionDepth;
    private IUndoableAction lastRegistered;

    /// <summary>
    /// An action called when history is changed.
    /// </summary>
    public event Action HistoryChanged;

    /// <summary>
    /// Gets a value indicating whether we can currently undo.
    /// </summary>
    public bool CanUndo => this.undoStack.Count > 0;

    /// <summary>
    /// Gets a value indicating whether we can currently redo.
    /// </summary>
    public bool CanRedo => this.redoStack.Count > 0;

    /// <summary>
    /// Gets a value indicating whether a transaction is currently open.
    /// </summary>
    public bool IsInTransaction => this.openTransaction != null;

    /// <summary>
    /// Gets the permanent action log.
    /// </summary>
    public IReadOnlyList<IUndoableAction> ActionLog => this.actionLog;

    /// <summary>
    /// Starts grouping registered actions into one undo step.
    /// Calls can be nested, only the outermost EndTransaction commits.
    /// </summary>
    public void BeginTransaction()
    {
        this.openTransaction ??= new CompositeAction();
        this.transactionDepth++;
    }

    /// <summary>
    /// Ends a transaction. When the outermost transaction ends, everything
    /// registered in between is pushed as one undo step (nothing if it is empty).
    /// </summary>
    public void EndTransaction()
    {
        if (this.openTransaction == null)
        {
            return;
        }

        this.transactionDepth--;

        if (this.transactionDepth > 0)
        {
            return;
        }

        this.CommitTransaction();
    }

    /// <summary>
    /// Reverts everything done in the open transaction and discards it.
    /// </summary>
    public void CancelTransaction()
    {
        if (this.openTransaction == null)
        {
            return;
        }

        CompositeAction cancelled = this.openTransaction;
        this.openTransaction = null;
        this.transactionDepth = 0;

        cancelled.Undo();
    }

    /// <summary>
    /// Registers a completed operation.
    /// The operation should already have been applied.
    /// </summary>
    /// <param name="action">The action to register.</param>
    public void RegisterAction(IUndoableAction action)
    {
        if (action == null)
        {
            Debug.LogWarning("Cannot register a null undo action.");
            return;
        }

        if (this.openTransaction != null)
        {
            this.openTransaction.Add(action);
            return;
        }

        if (this.lastRegistered is IMergeableAction mergeable &&
            this.undoStack.Count > 0 &&
            ReferenceEquals(this.undoStack.Peek(), this.lastRegistered) &&
            mergeable.TryMerge(action))
        {
            this.redoStack.Clear();
            this.HistoryChanged?.Invoke();
            return;
        }

        this.undoStack.Push(action);
        this.lastRegistered = action;

        this.redoStack.Clear();

        this.actionLog.Add(action);

        this.HistoryChanged?.Invoke();
    }

    /// <summary>
    /// Undoes the most recent operation.
    /// </summary>
    public void Undo()
    {
        this.CommitOpenTransaction();

        if (!this.CanUndo)
        {
            return;
        }

        IUndoableAction action = this.undoStack.Pop();
        this.lastRegistered = null;

        try
        {
            action.Undo();
            this.redoStack.Push(action);
        }
        catch (Exception exception)
        {
            this.undoStack.Push(action);
            Debug.LogException(exception);
        }

        this.HistoryChanged?.Invoke();
    }

    /// <summary>
    /// Redoes the most recently undone operation.
    /// </summary>
    public void Redo()
    {
        this.CommitOpenTransaction();

        if (!this.CanRedo)
        {
            return;
        }

        IUndoableAction action = this.redoStack.Pop();
        this.lastRegistered = null;

        try
        {
            action.Redo();
            this.undoStack.Push(action);
        }
        catch (Exception exception)
        {
            this.redoStack.Push(action);
            Debug.LogException(exception);
        }

        this.HistoryChanged?.Invoke();
    }

    /// <summary>
    /// Clears undo and redo history.
    /// Does not clear the permanent action log.
    /// </summary>
    public void ClearHistory()
    {
        this.openTransaction = null;
        this.transactionDepth = 0;
        this.lastRegistered = null;

        this.undoStack.Clear();
        this.redoStack.Clear();

        this.HistoryChanged?.Invoke();
    }

    /// <summary>
    /// Clears all history, including the permanent action log.
    /// </summary>
    public void ClearAll()
    {
        this.ClearHistory();
        this.actionLog.Clear();

        this.HistoryChanged?.Invoke();
    }

    private void CommitOpenTransaction()
    {
        if (this.openTransaction != null)
        {
            this.transactionDepth = 0;
            this.CommitTransaction();
        }
    }

    private void CommitTransaction()
    {
        CompositeAction done = this.openTransaction;
        this.openTransaction = null;
        this.transactionDepth = 0;

        if (done != null && !done.IsEmpty)
        {
            this.RegisterAction(done);
        }
    }
}
