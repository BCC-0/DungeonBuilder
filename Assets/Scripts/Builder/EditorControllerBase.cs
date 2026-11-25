using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base class for all editor controllers (tiles and entities).
/// Handles shared input state, pointer tracking, and tool execution flow.
/// </summary>
public abstract class EditorControllerBase : MonoBehaviour
{
    /// <summary>
    /// The tilemap we can edit tiles on.
    /// </summary>
    [SerializeField]
    private SaveableTilemap saveableTilemap;

    private SelectionManagerBase selectionManager;
    private Vector2 currentPos;
    private bool primaryHolding;
    private bool secondaryHolding;
    private bool strokeActive;

    /// <summary>
    /// The different possible actions when editing entities/tiles.
    /// </summary>
    public enum EditorAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        None,

        /// <summary>
        /// Paint action.
        /// </summary>
        Paint,

        /// <summary>
        /// Erase action.
        /// </summary>
        Erase,
    }

    /// <summary>
    /// Gets the tilemap used by this editor controller.
    /// </summary>
    public SaveableTilemap Tilemap => this.saveableTilemap;

    /// <summary>
    /// Gets a value indicating whether the selection of this controller is currently being moved.
    /// </summary>
    public bool IsMovingSelection =>
        this.selectionManager.IsInMovementMode ||
        this.selectionManager.IsResizePreviewOpen;

    /// <summary>
    /// Gets a value indicating whether a resize handle of this controller is being dragged.
    /// </summary>
    public bool IsSelectionResizing => this.selectionManager.IsResizing;

    /// <summary>
    /// Gets a value indicating whether the selection of this controller is busy with
    /// work spread over several frames, such as pasting many entities.
    /// </summary>
    public bool IsBusy => this.selectionManager.IsBusy;

    /// <summary>
    /// Gets the shared tilemap used for tile painting.
    /// </summary>
    protected SaveableTilemap SaveableTilemap => this.saveableTilemap;

    /// <summary>
    /// Gets or sets the selection manager handling click/drag/box selection.
    /// </summary>
    protected SelectionManagerBase SelectionManager { get => this.selectionManager; set => this.selectionManager = value; }

    /// <summary>
    /// Gets the current world position of the pointer.
    /// </summary>
    protected Vector2 CurrentPos => this.currentPos;

    /// <summary>
    /// Gets a value indicating whether the primary input is held.
    /// </summary>
    protected bool PrimaryHolding => this.primaryHolding;

    /// <summary>
    /// Gets a value indicating whether the secondary input is held.
    /// </summary>
    protected bool SecondaryHolding => this.secondaryHolding;

    /// <summary>
    /// Gets the currently active editor tool.
    /// </summary>
    protected EditorTool CurrentTool { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a resize drag or a resize preview owns the
    /// selection, so other selection actions must leave it alone.
    /// </summary>
    private bool IsResizeBusy =>
        this.selectionManager.IsResizing ||
        this.selectionManager.IsResizePreviewOpen;

    /// <summary>
    /// Updates the pointer world position.
    /// </summary>
    /// <param name="worldPos">The world position of the pointer.</param>
    public void OnPointerMoved(Vector3 worldPos)
    {
        this.currentPos = worldPos;
    }

    /// <summary>
    /// Sets the active editor tool for this controller.
    /// </summary>
    /// <param name="tool">The tool to activate.</param>
    public void SetTool(EditorTool tool)
    {
        // Changing tool always finishes the stroke that is in progress.
        this.EndStroke();

        this.CurrentTool = tool;
    }

    /// <summary>
    /// Called when the primary input is pressed.
    /// </summary>
    public virtual void OnPrimaryDown()
    {
        if (this.CurrentTool == EditorTool.Selection)
        {
            this.selectionManager.BeginDrag(this.CurrentPos);
            return;
        }

        if (this.CurrentTool == EditorTool.Pick)
        {
            this.OnPick();
            return;
        }

        this.primaryHolding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Called when the primary input is released.
    /// </summary>
    public virtual void OnPrimaryUp()
    {
        if (this.CurrentTool == EditorTool.Selection)
        {
            this.selectionManager.EndDrag();
            return;
        }

        this.primaryHolding = false;
        this.EndStrokeIfReleased();
    }

    /// <summary>
    /// Called when the secondary input is pressed.
    /// </summary>
    public void OnSecondaryDown()
    {
        this.secondaryHolding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Called when the secondary input is released.
    /// </summary>
    public void OnSecondaryUp()
    {
        this.secondaryHolding = false;
        this.EndStrokeIfReleased();
    }

    /// <summary>
    /// Deletes all selected entities or tiles.
    /// </summary>
    public void OnDeleteSelection()
    {
        if (this.IsResizeBusy)
        {
            return;
        }

        this.selectionManager.DeleteSelected();
    }

    /// <summary>
    /// Copies the currently selected items to the clipboard.
    /// </summary>
    public void OnCopySelection()
    {
        if (this.IsResizeBusy)
        {
            return;
        }

        this.selectionManager.CopySelected();
    }

    /// <summary>
    /// Cuts the currently selected items to the clipboard.
    /// </summary>
    public void OnCutSelection()
    {
        if (this.IsResizeBusy)
        {
            return;
        }

        this.selectionManager.CutSelected();
    }

    /// <summary>
    /// Pastes the clipboard at the pointer and starts moving the pasted items.
    /// </summary>
    public void OnPaste()
    {
        if (this.IsResizeBusy)
        {
            return;
        }

        this.selectionManager.PasteAtPointer();
    }

    /// <summary>
    /// Starts moving the selection.
    /// </summary>
    public void OnMoveSelection()
    {
        if (this.IsResizeBusy)
        {
            return;
        }

        this.selectionManager.MoveSelected();
    }

    /// <summary>
    /// Confirms moving the items to the current shown position.
    /// </summary>
    public void OnConfirmMove()
    {
        if (this.selectionManager.IsResizePreviewOpen)
        {
            this.selectionManager.ConfirmResize();
            return;
        }

        this.selectionManager.ConfirmMovingSelected();
    }

    /// <summary>
    /// Cancels moving the items and returns them to their last position.
    /// </summary>
    public void OnCancelMove()
    {
        if (this.selectionManager.IsResizePreviewOpen)
        {
            this.selectionManager.CancelResize();
            return;
        }

        this.selectionManager.CancelMovingSelected();
    }

    /// <summary>
    /// Starts resizing the selection when a resize handle was pressed.
    /// </summary>
    /// <param name="screenPos">The screen position of the press.</param>
    /// <returns>True when a handle was grabbed and the press belongs to the resize.</returns>
    public bool BeginSelectionResize(Vector2 screenPos)
    {
        return this.selectionManager.BeginResize(screenPos);
    }

    /// <summary>
    /// Ends a resize handle drag and applies the new size as one undo step.
    /// </summary>
    public void EndSelectionResize()
    {
        this.selectionManager.EndResize();
    }

    /// <summary>
    /// Clears any current selection made by this controller.
    /// </summary>
    public void ResetSelectionTool()
    {
        this.selectionManager.ResetSelectionTool();
    }

    /// <summary>
    /// Gets a value indicating whether an entity or tile can be selected at the given world position.
    /// </summary>
    /// <param name="worldPos">The world position to check.</param>
    /// <returns>True when a click at the position would select something.</returns>
    public bool CanSelectAt(Vector2 worldPos)
    {
        return this.selectionManager.CanSelectAt(worldPos);
    }

    /// <summary>
    /// Selects the entity or tile at the given world position, as if it was clicked.
    /// </summary>
    /// <param name="worldPos">The world position that was clicked.</param>
    public void SelectAt(Vector2 worldPos)
    {
        this.selectionManager.CurrentPos = worldPos;
        this.selectionManager.BeginDrag(worldPos);
        this.selectionManager.EndDrag();
    }

    /// <summary>
    /// Checks whether a screen position lies on the selection buttons, so a press
    /// there is not treated as world input.
    /// </summary>
    /// <param name="screenPos">The screen position to test.</param>
    /// <returns>True when the position is on the selection buttons.</returns>
    public bool IsPointerOverSelectionButtons(Vector2 screenPos)
    {
        return this.selectionManager.IsPointerOverSelectionButtons(screenPos);
    }

    /// <summary>
    /// Gets a value indicating whether the selection menu is open.
    /// </summary>
    public bool IsSelectionMenuOpen => this.selectionManager.IsSelectionMenuOpen;

    /// <summary>
    /// Opens the selection menu at the given world position.
    /// </summary>
    /// <param name="worldPos">The world position the menu is opened at.</param>
    public void OnSelectionMenuRequested(Vector2 worldPos)
    {
        this.selectionManager.ShowSelectionMenu(worldPos);
    }

    /// <summary>
    /// Closes the selection menu.
    /// </summary>
    public void OnSelectionMenuDismissed()
    {
        this.selectionManager.HideSelectionMenu();
    }

    /// <summary>
    /// Applies the current tool by resolving it into an editor action.
    /// </summary>
    protected void ApplyTool()
    {
        this.EnsureStroke();
        this.OnApplyTool(this.CurrentTool);
    }

    /// <summary>
    /// Converts tool + input state into concrete editor behavior.
    /// </summary>
    /// <param name="tool">The active editor tool.</param>
    protected abstract void OnApplyTool(EditorTool tool);

    /// <summary>
    /// Called when a brush or eraser stroke starts, before the first change.
    /// Everything changed until <see cref="OnStrokeEnd"/> is one undo step.
    /// </summary>
    protected virtual void OnStrokeBegin()
    {
    }

    /// <summary>
    /// Called when the stroke ends (input released, or the tool changed).
    /// </summary>
    protected virtual void OnStrokeEnd()
    {
    }

    /// <summary>
    /// Called when the pick tool is used. Picks what is under the pointer into the toolbar.
    /// </summary>
    protected virtual void OnPick()
    {
    }

    /// <summary>
    /// Converts the current tool and input state into a high-level action.
    /// </summary>
    /// <param name="tool">The active tool.</param>
    /// <returns>The resolved editor action.</returns>
    protected EditorAction GetAction(EditorTool tool)
    {
        return tool switch
        {
            EditorTool.Brush when this.PrimaryHolding => EditorAction.Paint,
            EditorTool.Brush when this.SecondaryHolding => EditorAction.Erase,

            EditorTool.Eraser when this.PrimaryHolding || this.SecondaryHolding
                => EditorAction.Erase,

            _ => EditorAction.None
        };
    }

    /// <summary>
    /// Makes sure a stroke is not left open when this controller is disabled.
    /// </summary>
    private void OnDisable()
    {
        this.primaryHolding = false;
        this.secondaryHolding = false;
        this.EndStroke();
    }

    /// <summary>
    /// Applies tool continuously while input is held,
    /// and drives the active selection drag (if any).
    /// </summary>
    private void Update()
    {
        this.selectionManager.CurrentPos = this.currentPos;

        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            if (BuilderInputSelector.Instance.IsUsingDesktop &&
                this.CurrentTool == EditorTool.Selection &&
                this.selectionManager.IsDragging &&
                !mouse.leftButton.isPressed)
            {
                this.OnPrimaryUp();
            }

            if (BuilderInputSelector.Instance.IsUsingDesktop &&
                this.selectionManager.IsResizing &&
                !mouse.leftButton.isPressed)
            {
                this.selectionManager.EndResize();
            }

            if (this.primaryHolding && !mouse.leftButton.isPressed)
            {
                this.primaryHolding = false;
            }

            if (this.secondaryHolding && !mouse.rightButton.isPressed)
            {
                this.secondaryHolding = false;
            }
        }

        // The input can also be released here, without OnPrimaryUp being called.
        this.EndStrokeIfReleased();

        if (this.CurrentTool == EditorTool.Selection && this.selectionManager.IsDragging)
        {
            this.selectionManager.UpdateDrag(this.CurrentPos);
        }

        if (this.selectionManager.IsResizing)
        {
            this.selectionManager.UpdateResize(this.CurrentPos);
        }

        if (this.primaryHolding || this.secondaryHolding)
        {
            this.ApplyTool();
        }
    }

    private void EnsureStroke()
    {
        if (this.strokeActive ||
            (this.CurrentTool != EditorTool.Brush &&
             this.CurrentTool != EditorTool.Eraser))
        {
            return;
        }

        this.strokeActive = true;
        this.OnStrokeBegin();
    }

    private void EndStrokeIfReleased()
    {
        if (!this.primaryHolding && !this.secondaryHolding)
        {
            this.EndStroke();
        }
    }

    private void EndStroke()
    {
        if (!this.strokeActive)
        {
            return;
        }

        this.strokeActive = false;
        this.OnStrokeEnd();
    }
}
