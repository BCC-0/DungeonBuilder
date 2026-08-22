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
    }

    /// <summary>
    /// Deletes all selected entities or tiles.
    /// </summary>
    public void OnDeleteSelection()
    {
        this.selectionManager.DeleteSelected();
    }

    /// <summary>
    /// Starts moving the selection.
    /// </summary>
    public void OnMoveSelection()
    {
        this.selectionManager.MoveSelected();
    }

    /// <summary>
    /// Confirms moving the items to the current shown position.
    /// </summary>
    public void OnConfirmMove()
    {
        this.selectionManager.ConfirmMovingSelected();
    }

    /// <summary>
    /// Cancels moving the items and returns them to their last position.
    /// </summary>
    public void OnCancelMove()
    {
        this.selectionManager.CancelMovingSelected();
    }

    /// <summary>
    /// Clears any current selection made by this controller.
    /// </summary>
    public void ResetSelectionTool()
    {
        this.selectionManager.ResetSelectionTool();
    }

    /// <summary>
    /// Applies the current tool by resolving it into an editor action.
    /// </summary>
    protected void ApplyTool()
    {
        this.OnApplyTool(this.CurrentTool);
    }

    /// <summary>
    /// Converts tool + input state into concrete editor behavior.
    /// </summary>
    /// <param name="tool">The active editor tool.</param>
    protected abstract void OnApplyTool(EditorTool tool);

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

            if (this.primaryHolding && !mouse.leftButton.isPressed)
            {
                this.primaryHolding = false;
            }

            if (this.secondaryHolding && !mouse.rightButton.isPressed)
            {
                this.secondaryHolding = false;
            }
        }

        if (this.CurrentTool == EditorTool.Selection && this.selectionManager.IsDragging)
        {
            this.selectionManager.UpdateDrag(this.CurrentPos);
        }

        if (this.primaryHolding || this.secondaryHolding)
        {
            this.ApplyTool();
        }
    }
}
