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

    /// <summary>
    /// An action called when history is changed.
    /// </summary>
    public event Action HistoryChanged;

    /// <summary>
    /// Gets the instance of the undo redo manager.
    /// </summary>
    public static UndoRedoManager Instance { get; private set; }

    /// <summary>
    /// Gets a value indicating whether we can currently undo.
    /// </summary>
    public bool CanUndo => this.undoStack.Count > 0;

    /// <summary>
    /// Gets a value indicating whether we can currently redo.
    /// </summary>
    public bool CanRedo => this.redoStack.Count > 0;

    /// <summary>
    /// Gets the permanent action log.
    /// </summary>
    public IReadOnlyList<IUndoableAction> ActionLog => this.actionLog;

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

        this.undoStack.Push(action);

        // A new operation invalidates the redo history.
        this.redoStack.Clear();

        // Keep the permanent log separate from undo/redo.
        this.actionLog.Add(action);

        this.HistoryChanged?.Invoke();
    }

    /// <summary>
    /// Undoes the most recent operation.
    /// </summary>
    public void Undo()
    {
        if (!this.CanUndo)
        {
            return;
        }

        IUndoableAction action = this.undoStack.Pop();

        try
        {
            action.Undo();
            this.redoStack.Push(action);
        }
        catch (Exception exception)
        {
            // Preserve the action if undo failed.
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
        if (!this.CanRedo)
        {
            return;
        }

        IUndoableAction action = this.redoStack.Pop();

        try
        {
            action.Redo();
            this.undoStack.Push(action);
        }
        catch (Exception exception)
        {
            // Preserve the action if redo failed.
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
