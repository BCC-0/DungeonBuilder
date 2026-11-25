using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared drag mechanics for selection managers.
/// </summary>
public abstract class SelectionManagerBase : MonoBehaviour
{
    /// <summary>
    /// Minimum pointer movement (in world units) from the drag start before
    /// a primary-input hold is treated as a box-select drag rather than a click.
    /// </summary>
    protected const float DragThreshold = 0.1f;

    /// <summary>
    /// Fallback for <see cref="maxSelectionSize"/>, used when the serialized
    /// value was never set.
    /// </summary>
    private const int DefaultMaxSelectionSize = 30;

    /// <summary>
    /// Gap between the selection and the button panel, in canvas units (the local space of
    /// the selection box parent). Higher values push the panel further away from the selection.
    /// Independent of screen size and of the panel's own height.
    /// </summary>
    [SerializeField]
    private float buttonGap = 40f;

    /// <summary>
    /// Gap between the pointer and the selection menu when it is opened, in canvas units.
    /// </summary>
    [SerializeField]
    private float menuPointerOffset = 24f;

    /// <summary>
    /// Alpha of the menu buttons when the action they perform cannot be used, for
    /// example when nothing is selected.
    /// </summary>
    [SerializeField]
    private float disabledButtonAlpha = 0.4f;

    /// <summary>
    /// Largest number of cells a selection may span on one axis. Resizing and
    /// selecting are both held to this size, so the preview and the clipboard
    /// work stay small enough to remain responsive.
    /// </summary>
    [SerializeField]
    private int maxSelectionSize = DefaultMaxSelectionSize;

    /// <summary>
    /// The menu button that pastes. It follows the clipboard instead of the selection.
    /// When left empty, the button that calls <see cref="MapEditorManager.OnPasteSelection"/>
    /// is found automatically.
    /// </summary>
    [SerializeField]
    private Button pasteButton;

    /// <summary>
    /// The selection box UI object.
    /// </summary>
    [SerializeField]
    private SelectionBox selectionBox;

    /// <summary>
    /// The canvas the selection box is in.
    /// </summary>
    [SerializeField]
    private RectTransform selectionBoxParent;

    /// <summary>
    /// The UI element containing buttons with actions possible after selecting.
    /// </summary>
    [SerializeField]
    private RectTransform selectionButtons;

    /// <summary>
    /// Canvas group controlling interaction with the selection buttons.
    /// </summary>
    [SerializeField]
    private CanvasGroup selectionButtonsCanvasGroup;

    /// <summary>
    /// The UI element containing buttons with actions possible when moving (mobile only).
    /// </summary>
    [SerializeField]
    private RectTransform moveButtons;

    private Camera cam;

    private SelectionResizeHandles resizeHandles;

    private bool isDragging;
    private bool isDraggingResize;
    private bool hasResizePreview;
    private ResizeHandle activeResizeHandle = ResizeHandle.None;
    private RectInt resizeSourceCells;
    private RectInt resizeTargetCells;
    private Vector2 resizeStartWorld;
    private bool hasDragged;
    private bool isMovementMode;
    private bool isMoving;
    private bool isReferenceSelectionMode;
    private Grid grid;

    private Vector2 dragStart;
    private Vector2 dragEnd;
    private Vector2 currentPos;

    /// <summary>
    /// Gets a value indicating whether a selection drag is currently in progress.
    /// </summary>
    public bool IsDragging => this.isDragging;

    /// <summary>
    /// Gets a value indicating whether reference selection mode is active.
    /// </summary>
    public bool IsReferenceSelectionMode => this.isReferenceSelectionMode;

    /// <summary>
    /// Gets a value indicating whether the selection is currently being moved.
    /// </summary>
    public bool IsInMovementMode => this.isMovementMode;

    /// <summary>
    /// Gets a value indicating whether a resize handle is currently being dragged.
    /// </summary>
    public bool IsResizing => this.isDraggingResize;

    /// <summary>
    /// Gets a value indicating whether a resize is previewed and waits to be
    /// confirmed or cancelled, like a move does.
    /// </summary>
    public bool IsResizePreviewOpen => this.hasResizePreview;

    /// <summary>
    /// Gets a value indicating whether this manager is busy with work that is spread
    /// over several frames, such as pasting many entities.
    /// </summary>
    public virtual bool IsBusy => false;

    /// <summary>
    /// Gets a value indicating whether the selection menu is currently open.
    /// </summary>
    public bool IsSelectionMenuOpen => this.selectionButtons.gameObject.activeSelf;

    /// <summary>
    /// Gets or sets the current position of the used pointer.
    /// </summary>
    public Vector2 CurrentPos
    {
        get => this.currentPos;
        set => this.currentPos = value;
    }

    /// <summary>
    /// Gets the largest number of cells a selection may span on one axis.
    /// </summary>
    protected int MaxSelectionSize =>
        this.maxSelectionSize > 0 ? this.maxSelectionSize : DefaultMaxSelectionSize;

    /// <summary>
    /// Gets the largest number of cells a selection may hold.
    /// </summary>
    protected int MaxSelectedCells =>
        this.MaxSelectionSize * this.MaxSelectionSize;

    /// <summary>
    /// Gets or sets a value indicating whether we are currently in movement mode.
    /// </summary>
    protected bool IsMovementMode
    {
        get => this.isMovementMode;
        set => this.isMovementMode = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether we are currently moving the selected items.
    /// </summary>
    protected bool IsMoving
    {
        get => this.isMoving;
        set => this.isMoving = value;
    }

    /// <summary>
    /// Gets or sets the grid used for moving.
    /// </summary>
    protected Grid Grid
    {
        get => this.grid;
        set => this.grid = value;
    }

    /// <summary>
    /// Gets a value indicating whether this manager currently has something selected.
    /// </summary>
    protected abstract bool HasSelection { get; }

    /// <summary>
    /// Gets a value indicating whether the selection can be acted on by the menu.
    /// Managers can refuse here for selections that only exist as references.
    /// </summary>
    protected virtual bool CanUseSelectionActions => this.HasSelection;

    /// <summary>
    /// Begins picking entities for a reference field. The current selection is
    /// left untouched, clicks and box-selects are applied to the field instead.
    /// </summary>
    public void BeginReferenceSelection()
    {
        if (this.IsMovementMode)
        {
            return;
        }

        this.isReferenceSelectionMode = true;
        this.isDragging = false;
        this.hasDragged = false;

        this.DisableSelectionButtons();
        this.DisableMoveButtons();
        this.selectionBox.StopSelection();

        this.OnReferenceSelectionStarted();
    }

    /// <summary>
    /// Finishes reference selection ("Done" button). Everything that was picked
    /// has already been applied to the field.
    /// </summary>
    public void ConfirmReferenceSelection()
    {
        this.EndReferenceSelection();
    }

    /// <summary>
    /// Stops reference selection ("Cancel" button). Picks that were already
    /// applied stay applied.
    /// </summary>
    public void CancelReferenceSelection()
    {
        this.EndReferenceSelection();
    }

    /// <summary>
    /// Resets the selection manager when switching away from the selection tool.
    /// Cancels any active movement, clears the selection, and hides all buttons.
    /// </summary>
    public void ResetSelectionTool()
    {
        this.CancelResizeDrag();
        this.CancelResize();

        if (this.isReferenceSelectionMode)
        {
            this.CancelReferenceSelection();
        }

        if (this.IsMovementMode)
        {
            this.CancelMovingSelected();
        }
        else
        {
            this.ClearSelection();
        }

        this.DisableSelectionButtons();
        this.DisableMoveButtons();

        this.isDragging = false;
        this.hasDragged = false;
        this.isMoving = false;

        this.selectionBox.StopSelection();
    }

    /// <summary>
    /// Begins a selection drag at the given world position.
    /// </summary>
    /// <param name="worldPos">The pointer's world position at drag start.</param>
    public void BeginDrag(Vector2 worldPos)
    {
        if (this.IsMovementMode)
        {
            if (!BuilderInputSelector.Instance.IsUsingDesktop)
            {
                this.ContinueMove(worldPos);
            }

            return;
        }

        this.isDragging = true;
        this.hasDragged = false;
        this.dragStart = worldPos;
        this.dragEnd = worldPos;

        this.selectionBox.StartSelection(this.WorldToLocalUiPoint(this.dragStart));
    }

    /// <summary>
    /// Updates an in-progress selection drag with the pointer's current world position.
    /// Call every frame while <see cref="IsDragging"/> is true.
    /// </summary>
    /// <param name="worldPos">The pointer's current world position.</param>
    public void UpdateDrag(Vector2 worldPos)
    {
        if (!this.isDragging)
        {
            return;
        }

        // The box is drawn from the drag start to this point and the same rect is
        // used for the selection, so limiting it here keeps both inside the cap.
        this.dragEnd = this.LimitDragEnd(worldPos);

        if (!this.hasDragged &&
            Vector3.Distance(this.dragStart, this.dragEnd) >= DragThreshold)
        {
            this.hasDragged = true;
        }

        this.selectionBox.SetPosition(
            this.WorldToLocalUiPoint(this.dragStart),
            this.WorldToLocalUiPoint(this.dragEnd));
    }

    /// <summary>
    /// Ends the current selection drag, resolving it into either a click-select
    /// or a box-select depending on whether the pointer moved far enough.
    /// </summary>
    public void EndDrag()
    {
        if (this.IsMovementMode)
        {
            this.isMoving = false;
        }

        if (!this.isDragging)
        {
            return;
        }

        this.isDragging = false;

        if (this.IsReferenceSelectionMode)
        {
            List<SaveableEntity> picked;

            if (this.hasDragged)
            {
                picked = this.PickEntitiesIn(this.GetWorldRect(this.dragStart, this.dragEnd))
                    .OrderBy(e => Vector2.SqrMagnitude((Vector2)e.transform.position - this.dragStart))
                    .ToList();
            }
            else
            {
                picked = this.PickEntitiesAt(this.dragEnd);
            }

            this.HandleReferencePick(picked);
        }
        else if (this.hasDragged)
        {
            this.OnBoxSelect(this.GetWorldRect(this.dragStart, this.dragEnd));
        }
        else
        {
            this.OnClickSelect(this.dragEnd);
        }

        this.selectionBox.StopSelection();
    }

    /// <summary>
    /// Gets a value indicating whether a click at the given world position would select something.
    /// </summary>
    /// <param name="worldPos">The world position to check.</param>
    /// <returns>True when something selectable is under the position.</returns>
    public bool CanSelectAt(Vector2 worldPos)
    {
        return this.HasSelectableAt(worldPos);
    }

    /// <summary>
    /// Checks whether a screen position lies on the visible selection or move buttons.
    /// Presses there are consumed even while the buttons ignore raycasts, so they do not
    /// start editing behind the panel.
    /// </summary>
    /// <param name="screenPos">The screen position to test.</param>
    /// <returns>True when the position is on the buttons.</returns>
    public bool IsPointerOverSelectionButtons(Vector2 screenPos)
    {
        return this.IsPointerOverRect(this.selectionButtons, screenPos) ||
            this.IsPointerOverRect(this.moveButtons, screenPos);
    }

    /// <summary>
    /// Starts resizing the selection when the press landed on one of its handles.
    /// Dragging only changes the previewed size; the world is not written until
    /// the resize is confirmed, so existing entities and tiles stay in place.
    /// </summary>
    /// <param name="screenPos">The screen position of the press.</param>
    /// <returns>True when a handle was grabbed and the press belongs to the resize.</returns>
    public bool BeginResize(Vector2 screenPos)
    {
        if (!this.CanResize() || this.isDraggingResize || this.hasResizePreview)
        {
            return false;
        }

        this.RefreshResizeHandleLayout();

        if (!this.resizeHandles.TryHit(screenPos, out ResizeHandle handle))
        {
            return false;
        }

        this.isDraggingResize = true;
        this.activeResizeHandle = handle;
        this.resizeSourceCells = this.GetSelectedCellRect();
        this.resizeTargetCells = this.resizeSourceCells;
        this.resizeStartWorld = this.cam.ScreenToWorldPoint(screenPos);

        this.resizeHandles.SetGrabbed(handle);
        this.DisableSelectionButtons();

        return true;
    }

    /// <summary>
    /// Updates the size that is being dragged. Only the preview follows the
    /// pointer; nothing in the map changes yet.
    /// </summary>
    /// <param name="worldPos">The current world position of the pointer.</param>
    public void UpdateResize(Vector2 worldPos)
    {
        if (!this.isDraggingResize)
        {
            return;
        }

        Vector2Int delta = this.GetCellDelta(this.resizeStartWorld, worldPos);

        // Every cell of the target costs a preview cell and, for entities, one
        // copy per entity in it, so a huge drag has to be cut down here.
        delta = SelectionResize.LimitGrowth(
            this.resizeSourceCells,
            this.activeResizeHandle,
            delta,
            this.MaxSelectionSize);

        RectInt target = SelectionResize.Resize(
            this.resizeSourceCells,
            this.activeResizeHandle,
            delta);

        if (target == this.resizeTargetCells)
        {
            return;
        }

        this.resizeTargetCells = target;

        // Show what the resize would create while the handle is dragged, so the
        // result can be judged before it is confirmed instead of seeing a box.
        this.CancelResizePreview();
        this.BeginResizePreview(this.resizeSourceCells, this.resizeTargetCells);

        this.RefreshResizeHandleLayout();
    }

    /// <summary>
    /// Ends the handle drag and writes the resized block to the world as one
    /// undo step. While the handle is dragged nothing is written, so the resize
    /// can still be judged as a preview; releasing the handle applies it, after
    /// which the selection can be resized again or undone.
    /// </summary>
    public void EndResize()
    {
        if (!this.isDraggingResize)
        {
            return;
        }

        this.isDraggingResize = false;
        this.activeResizeHandle = ResizeHandle.None;

        this.resizeHandles.SetGrabbed(ResizeHandle.None);

        if (this.resizeTargetCells == this.resizeSourceCells)
        {
            this.CancelResizePreview();
            this.RefreshResizeHandleLayout();
            return;
        }

        this.DisableMoveButtons();
        this.ApplyResizePreview();
        this.RefreshResizeHandleLayout();
    }

    /// <summary>
    /// Applies the previewed resize to the world as one undo step.
    /// </summary>
    public void ConfirmResize()
    {
        if (!this.hasResizePreview)
        {
            return;
        }

        this.hasResizePreview = false;

        this.DisableMoveButtons();
        this.ApplyResizePreview();
        this.RefreshResizeHandleLayout();
    }

    /// <summary>
    /// Drops the previewed resize. The world was not written yet, so only the
    /// preview has to be removed.
    /// </summary>
    public void CancelResize()
    {
        if (!this.hasResizePreview)
        {
            return;
        }

        this.hasResizePreview = false;

        this.DisableMoveButtons();
        this.CancelResizePreview();
        this.RefreshResizeHandleLayout();
    }

    /// <summary>
    /// Deletes all currently selected items.
    /// </summary>
    public abstract void DeleteSelected();

    /// <summary>
    /// Copies the current selection to the editor clipboard.
    /// </summary>
    public abstract void CopySelected();

    /// <summary>
    /// Copies the current selection to the editor clipboard and deletes it,
    /// as one undo step.
    /// </summary>
    public abstract void CutSelected();

    /// <summary>
    /// Pastes the editor clipboard at the pointer and starts moving the pasted
    /// items, so the position can be chosen before the paste is confirmed.
    /// </summary>
    public abstract void PasteAtPointer();

    /// <summary>
    /// Moves all selected items by following mouse/touch.
    /// </summary>
    public abstract void MoveSelected();

    /// <summary>
    /// Stops moving the selected items and places them at their current position.
    /// </summary>
    public abstract void ConfirmMovingSelected();

    /// <summary>
    /// Cancels moving the selected items and places them at their last position.
    /// </summary>
    public abstract void CancelMovingSelected();

    /// <summary>
    /// Clears the current selection and refreshes the visualizer.
    /// </summary>
    protected abstract void ClearSelection();

    /// <summary>
    /// Rebaselines an in-progress move so that further dragging continues from
    /// the current preview position rather than jumping back to the original
    /// touch-down point. Used on mobile, where lifting and retouching mid-move
    /// is a new gesture, not a continuous drag.
    /// </summary>
    /// <param name="worldPos">The pointer's world position at the new touch-down.</param>
    protected abstract void ContinueMove(Vector2 worldPos);

    /// <summary>
    /// Gets the current world-space bounds of the active selection.
    /// </summary>
    /// <returns>The rectangle with the selection.</returns>
    protected abstract Rect GetCurrentSelectionBounds();

    /// <summary>
    /// Gets the grid cells the current selection covers.
    /// </summary>
    /// <returns>The selected cells.</returns>
    protected abstract RectInt GetSelectedCellRect();

    /// <summary>
    /// Gets the world rectangle covered by a block of cells, including the full
    /// area of the outer cells.
    /// </summary>
    /// <param name="cells">The cells to cover.</param>
    /// <returns>The world rectangle of the cells.</returns>
    protected abstract Rect GetCellsWorldRect(RectInt cells);

    /// <summary>
    /// Shows the resized block without writing to the world, so the player can
    /// confirm or cancel it like a move.
    /// </summary>
    /// <param name="source">The cells of the current selection.</param>
    /// <param name="target">The cells the selection is resized to.</param>
    protected abstract void BeginResizePreview(RectInt source, RectInt target);

    /// <summary>
    /// Writes the previewed resize to the world as one undo step.
    /// </summary>
    protected abstract void ApplyResizePreview();

    /// <summary>
    /// Removes the previewed resize and leaves the world as it was.
    /// </summary>
    protected abstract void CancelResizePreview();

    /// <summary>
    /// Selects whatever is at the clicked world position (entity or tile,
    /// depending on the concrete manager).
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected abstract void OnClickSelect(Vector2 position);

    /// <summary>
    /// Selects everything within the dragged rectangle (entities or tiles,
    /// depending on the concrete manager).
    /// </summary>
    /// <param name="rect">The world-space rectangle of the drag.</param>
    protected abstract void OnBoxSelect(Rect rect);

    /// <summary>
    /// Finds the entities that would be picked by a click at a position.
    /// Used for reference selection, where the selection itself must not change.
    /// Managers that do not select entities return nothing.
    /// </summary>
    /// <param name="position">The clicked world position.</param>
    /// <returns>The entities at that position.</returns>
    protected virtual List<SaveableEntity> PickEntitiesAt(Vector2 position)
    {
        return new List<SaveableEntity>();
    }

    /// <summary>
    /// Finds the entities that would be picked by a box-select.
    /// Used for reference selection, where the selection itself must not change.
    /// Managers that do not select entities return nothing.
    /// </summary>
    /// <param name="rect">The world-space rectangle of the box-select.</param>
    /// <returns>The entities inside the rectangle.</returns>
    protected virtual List<SaveableEntity> PickEntitiesIn(Rect rect)
    {
        return new List<SaveableEntity>();
    }

    /// <summary>
    /// Gets a value indicating whether something is under the given world position.
    /// Managers that cannot select anything return false.
    /// </summary>
    /// <param name="position">The world position to check.</param>
    /// <returns>True when something selectable is under the position.</returns>
    protected virtual bool HasSelectableAt(Vector2 position)
    {
        return false;
    }

    /// <summary>
    /// Called when reference selection starts.
    /// </summary>
    protected virtual void OnReferenceSelectionStarted()
    {
    }

    /// <summary>
    /// Called after reference selection ended, so the manager can bring back
    /// the buttons that were hidden during it.
    /// </summary>
    protected virtual void OnReferenceSelectionEnded()
    {
    }

    /// <summary>
    /// Finds the camera used to convert world positions to UI positions.
    /// </summary>
    protected virtual void Awake()
    {
        this.cam = Camera.main;
        this.grid = FindAnyObjectByType<Grid>();

        this.CreateResizeHandles();

        this.DisableSelectionButtons();
        this.DisableMoveButtons();
    }

    /// <summary>
    /// Shows the selection menu at the given world position. The menu is opened by a
    /// right click or a double tap instead of appearing whenever something is selected,
    /// so it never sits in the way of the selection itself.
    /// </summary>
    /// <param name="worldPos">The world position the menu is opened at.</param>
    public void ShowSelectionMenu(Vector2 worldPos)
    {
        if (this.IsMovementMode ||
            this.IsReferenceSelectionMode ||
            this.isDraggingResize ||
            this.hasResizePreview)
        {
            return;
        }

        this.selectionButtonsCanvasGroup.interactable = true;
        this.selectionButtonsCanvasGroup.blocksRaycasts = true;

        this.PositionButtonsAtPoint(
            this.selectionButtons,
            this.WorldToLocalUiPoint(worldPos));

        BuilderClipboard.Changed -= this.OnClipboardChanged;
        BuilderClipboard.Changed += this.OnClipboardChanged;

        this.UpdateMenuButtons();

        this.selectionButtons.gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the selection menu.
    /// </summary>
    public void HideSelectionMenu()
    {
        this.DisableSelectionButtons();
    }

    /// <summary>
    /// Disables the selection buttons.
    /// </summary>
    protected void DisableSelectionButtons()
    {
        BuilderClipboard.Changed -= this.OnClipboardChanged;

        this.selectionButtons.gameObject.SetActive(false);
    }

    /// <summary>
    /// Enables the move buttons.
    /// </summary>
    /// <param name="selectionRect">The rect of all selected objects.</param>
    protected void EnableMoveButtons(Rect selectionRect)
    {
        if (BuilderInputSelector.Instance.IsUsingDesktop)
        {
            return;
        }

        this.PositionButtonsAboveRect(this.moveButtons, selectionRect);
        this.moveButtons.gameObject.SetActive(true);
    }

    /// <summary>
    /// Disables the move buttons.
    /// </summary>
    protected void DisableMoveButtons()
    {
        this.moveButtons.gameObject.SetActive(false);
    }

    /// <summary>
    /// Computes the world-space bounding rectangle containing a set of points.
    /// </summary>
    /// <param name="points">The points to bound. Must contain at least one point.</param>
    /// <returns>The bounding rectangle.</returns>
    protected Rect GetBoundingRect(IEnumerable<Vector2> points)
    {
        using IEnumerator<Vector2> e = points.GetEnumerator();
        e.MoveNext();
        Vector2 min = e.Current;
        Vector2 max = min;

        while (e.MoveNext())
        {
            min = Vector2.Min(min, e.Current);
            max = Vector2.Max(max, e.Current);
        }

        return new Rect(min, max - min);
    }

    /// <summary>
    /// Applies picked entities to the reference field being edited.
    /// Ends reference selection when the field is satisfied (single references),
    /// and stays open otherwise (lists) so more can be added.
    /// </summary>
    /// <param name="picked">The entities that were clicked or boxed.</param>
    private void HandleReferencePick(List<SaveableEntity> picked)
    {
        if (picked == null || picked.Count == 0)
        {
            return;
        }

        RuntimePropertyEditor editor = RuntimePropertyEditor.Instance;

        if (editor == null || editor.TryApplyReferences(picked))
        {
            this.EndReferenceSelection();
        }
    }

    /// <summary>
    /// Leaves reference selection mode.
    /// </summary>
    private void EndReferenceSelection()
    {
        if (!this.isReferenceSelectionMode)
        {
            return;
        }

        this.isReferenceSelectionMode = false;
        this.isDragging = false;
        this.hasDragged = false;

        this.selectionBox.StopSelection();

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.CancelReferenceSelection();
        }

        this.OnReferenceSelectionEnded();
    }

    /// <summary>
    /// Gets the world rectangle of a selection drag.
    /// </summary>
    /// <param name="a">Position 1.</param>
    /// <param name="b">Position 2.</param>
    /// <returns>The rectangle of the drag.</returns>
    private Rect GetWorldRect(Vector2 a, Vector2 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        return new Rect(min, max - min);
    }

    /// <summary>
    /// Transforms a point to a UI point for the selection box.
    /// </summary>
    /// <param name="worldPos">The world position we are currently selecting at.</param>
    /// <returns>The UI point.</returns>
    private Vector2 WorldToLocalUiPoint(Vector2 worldPos)
    {
        return this.ScreenToLocalUiPoint(this.cam.WorldToScreenPoint(worldPos));
    }

    /// <summary>
    /// Transforms a screen point to the local space of the selection box parent,
    /// which is also the space the resize handles live in.
    /// </summary>
    /// <param name="screenPos">The screen position to transform.</param>
    /// <returns>The local point.</returns>
    private Vector2 ScreenToLocalUiPoint(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            this.selectionBoxParent,
            screenPos,
            this.cam,
            out Vector2 localPoint);

        return localPoint;
    }

    /// <summary>
    /// Gets the visible screen area expressed in the local space of the selection box parent.
    /// </summary>
    /// <returns>The visible area.</returns>
    private Rect GetVisibleUiRect()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            this.selectionBoxParent,
            Vector2.zero,
            this.cam,
            out Vector2 min);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            this.selectionBoxParent,
            new Vector2(Screen.width, Screen.height),
            this.cam,
            out Vector2 max);

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    /// <summary>
    /// Positions a button panel centered above the given world-space rect, keeping it fully on screen.
    /// If there is no room above, the panel is placed below the rect. If neither fits,
    /// the panel is clamped to the nearest visible position. The gap to the rect is a
    /// fixed distance in canvas units.
    /// </summary>
    /// <param name="buttons">The button panel's RectTransform to position.</param>
    /// <param name="worldRect">The world-space rect (e.g. selection bounds) to anchor to.</param>
    private void PositionButtonsAboveRect(
        RectTransform buttons,
        Rect worldRect)
    {
        Vector2 uiMin = this.WorldToLocalUiPoint(worldRect.min);
        Vector2 uiMax = this.WorldToLocalUiPoint(worldRect.max);
        Rect bounds = this.GetVisibleUiRect();

        Vector3 relativeScale = new Vector3(
            buttons.lossyScale.x / this.selectionBoxParent.lossyScale.x,
            buttons.lossyScale.y / this.selectionBoxParent.lossyScale.y,
            1f);
        Vector2 size = Vector2.Scale(buttons.rect.size, relativeScale);

        float gap = this.buttonGap;
        float minBottom = bounds.yMin;
        float maxBottom = Mathf.Max(minBottom, bounds.yMax - size.y);

        float bottom = uiMax.y + gap;

        if (bottom > maxBottom)
        {
            float below = uiMin.y - gap - size.y;
            bottom = below >= minBottom ? below : maxBottom;
        }

        bottom = Mathf.Clamp(bottom, minBottom, maxBottom);

        float minLeft = bounds.xMin;
        float maxLeft = Mathf.Max(minLeft, bounds.xMax - size.x);
        float left = Mathf.Clamp(
            ((uiMin.x + uiMax.x) * 0.5f) - (size.x * 0.5f),
            minLeft,
            maxLeft);

        Vector3 pivotWorld = this.selectionBoxParent.TransformPoint(new Vector3(
            left + (buttons.pivot.x * size.x),
            bottom + (buttons.pivot.y * size.y),
            0f));

        pivotWorld.z = buttons.position.z;
        buttons.position = pivotWorld;
    }

    /// <summary>
    /// Keeps the move buttons above the selection while it is being moved. The
    /// selection menu itself stays where it was opened.
    /// </summary>
    private void LateUpdate()
    {
        this.RefreshResizeHandleLayout();

        if (this.hasResizePreview && this.moveButtons.gameObject.activeSelf)
        {
            this.PositionButtonsAboveRect(
                this.moveButtons,
                this.GetCellsWorldRect(this.resizeTargetCells));
        }

        if (!this.HasSelection || !this.IsMovementMode)
        {
            return;
        }

        if (this.moveButtons.gameObject.activeSelf)
        {
            this.PositionButtonsAboveRect(
                this.moveButtons,
                this.GetCurrentSelectionBounds());
        }
    }

    /// <summary>
    /// Enables the menu buttons that can be used right now. Actions on the selection
    /// need a selection, while paste only needs something on the clipboard.
    /// </summary>
    private void UpdateMenuButtons()
    {
        bool hasSelection = this.CanUseSelectionActions;
        bool hasClipboard = BuilderClipboard.HasContent;

        if (this.pasteButton == null)
        {
            this.pasteButton = this.FindPasteButton();
        }

        foreach (Button button in
                 this.selectionButtons.GetComponentsInChildren<Button>(true))
        {
            this.SetButtonEnabled(
                button,
                button == this.pasteButton ? hasClipboard : hasSelection);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the selection may be resized right now.
    /// </summary>
    private bool CanResize()
    {
        return this.HasSelection &&
            this.CanUseSelectionActions &&
            !this.IsMovementMode &&
            !this.isReferenceSelectionMode &&
            !this.IsBusy;
    }

    /// <summary>
    /// Creates the resize handles in code, so no sprite or prefab is needed.
    /// </summary>
    private void CreateResizeHandles()
    {
        if (this.selectionBoxParent == null)
        {
            return;
        }

        GameObject handleObject = new ("Selection Resize Handles");

        handleObject.transform.SetParent(this.selectionBoxParent, false);

        this.resizeHandles = handleObject.AddComponent<SelectionResizeHandles>();

        Color outlineColor = this.selectionBox != null
            ? this.selectionBox.OutlineColor
            : Color.white;

        this.resizeHandles.Initialize(
            this.selectionBoxParent,
            this.cam,
            outlineColor);
    }

    /// <summary>
    /// Moves the handles onto the selection, or hides them when there is nothing
    /// to resize. While a handle is dragged the handles follow the previewed size.
    /// </summary>
    private void RefreshResizeHandleLayout()
    {
        if (this.resizeHandles == null)
        {
            return;
        }

        RectInt cells = this.isDraggingResize
            ? this.resizeTargetCells
            : this.GetSelectedCellRect();

        bool visible =
            this.CanResize() &&
            !this.hasResizePreview &&
            cells.width > 0 &&
            cells.height > 0;

        this.resizeHandles.SetVisible(visible);

        if (!visible)
        {
            return;
        }

        Rect bounds = this.GetCellsWorldRect(cells);

        Vector2 uiMin = this.WorldToLocalUiPoint(bounds.min);
        Vector2 uiMax = this.WorldToLocalUiPoint(bounds.max);

        this.resizeHandles.Layout(uiMin, uiMax, this.GetScreenPixelsToLocal());
    }

    /// <summary>
    /// Drops the handle drag without previewing anything.
    /// </summary>
    private void CancelResizeDrag()
    {
        if (!this.isDraggingResize)
        {
            return;
        }

        this.isDraggingResize = false;
        this.activeResizeHandle = ResizeHandle.None;

        if (this.resizeHandles != null)
        {
            this.resizeHandles.SetGrabbed(ResizeHandle.None);
        }

        this.CancelResizePreview();
    }

    /// <summary>
    /// Gets how many local UI units one screen pixel is, so the handles keep the
    /// same size on screen whatever the resolution.
    /// </summary>
    /// <returns>The conversion factor.</returns>
    private float GetScreenPixelsToLocal()
    {
        Canvas canvas = this.selectionBoxParent.GetComponentInParent<Canvas>();

        float scaleFactor = canvas != null && canvas.rootCanvas != null
            ? canvas.rootCanvas.scaleFactor
            : 1f;

        return 1f / Mathf.Max(0.01f, scaleFactor);
    }

    /// <summary>
    /// Gets the whole-cell distance between two world positions.
    /// </summary>
    /// <param name="from">Where the drag started.</param>
    /// <param name="to">Where the pointer is now.</param>
    /// <returns>The distance in grid cells.</returns>
    private Vector2Int GetCellDelta(Vector2 from, Vector2 to)
    {
        if (this.grid == null)
        {
            return Vector2Int.zero;
        }

        Vector3Int fromCell = this.grid.WorldToCell((Vector3)from);
        Vector3Int toCell = this.grid.WorldToCell((Vector3)to);

        return (Vector2Int)(toCell - fromCell);
    }

    /// <summary>
    /// Limits the end of a drag so its selection box cannot span more cells than a
    /// selection may hold, no matter how far the pointer travels.
    /// </summary>
    /// <param name="worldPos">The pointer's current world position.</param>
    /// <returns>The drag end, kept inside the allowed cells.</returns>
    private Vector2 LimitDragEnd(Vector2 worldPos)
    {
        if (this.grid == null)
        {
            return worldPos;
        }

        Vector3Int startCell = this.grid.WorldToCell((Vector3)this.dragStart);
        Vector3Int endCell = this.grid.WorldToCell((Vector3)worldPos);

        int reach = this.MaxSelectionSize - 1;

        Vector3Int limitedCell = new Vector3Int(
            Mathf.Clamp(endCell.x, startCell.x - reach, startCell.x + reach),
            Mathf.Clamp(endCell.y, startCell.y - reach, startCell.y + reach),
            endCell.z);

        if (limitedCell == endCell)
        {
            return worldPos;
        }

        Vector3 center = this.grid.GetCellCenterWorld(limitedCell);

        return new Vector2(
            limitedCell.x == endCell.x ? worldPos.x : center.x,
            limitedCell.y == endCell.y ? worldPos.y : center.y);
    }

    /// <summary>
    /// Fades a single menu button and stops it from being clicked when it is disabled.
    /// </summary>
    /// <param name="button">The button to update.</param>
    /// <param name="enabled">Whether the button can be used.</param>
    private void SetButtonEnabled(Button button, bool enabled)
    {
        button.interactable = enabled;

        CanvasGroup group = button.GetComponent<CanvasGroup>();

        if (group == null)
        {
            group = button.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = enabled ? 1f : this.disabledButtonAlpha;
    }

    /// <summary>
    /// Finds the menu button that pastes, so it can follow the clipboard instead of
    /// the selection.
    /// </summary>
    /// <returns>The paste button, or null when the menu does not have one.</returns>
    private Button FindPasteButton()
    {
        if (this.pasteButton != null)
        {
            return this.pasteButton;
        }

        foreach (Button button in
                 this.selectionButtons.GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) ==
                    nameof(MapEditorManager.OnPasteSelection))
                {
                    return button;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Refreshes the menu buttons when the clipboard changes while the menu is open.
    /// </summary>
    private void OnClipboardChanged()
    {
        if (this.selectionButtons.gameObject.activeSelf)
        {
            this.UpdateMenuButtons();
        }
    }

    /// <summary>
    /// Positions a button panel at a UI point, keeping it fully on screen. Used for the
    /// selection menu, which opens where the player right clicked or double tapped.
    /// </summary>
    /// <param name="buttons">The button panel's RectTransform to position.</param>
    /// <param name="uiPoint">The UI-space point to place the panel at.</param>
    private void PositionButtonsAtPoint(
        RectTransform buttons,
        Vector2 uiPoint)
    {
        Rect bounds = this.GetVisibleUiRect();

        Vector3 relativeScale = new Vector3(
            buttons.lossyScale.x / this.selectionBoxParent.lossyScale.x,
            buttons.lossyScale.y / this.selectionBoxParent.lossyScale.y,
            1f);
        Vector2 size = Vector2.Scale(buttons.rect.size, relativeScale);

        float minLeft = bounds.xMin;
        float maxLeft = Mathf.Max(minLeft, bounds.xMax - size.x);
        float left = Mathf.Clamp(uiPoint.x, minLeft, maxLeft);

        float minBottom = bounds.yMin;
        float maxBottom = Mathf.Max(minBottom, bounds.yMax - size.y);
        float bottom = Mathf.Clamp(
            uiPoint.y + this.menuPointerOffset,
            minBottom,
            maxBottom);

        Vector3 pivotWorld = this.selectionBoxParent.TransformPoint(new Vector3(
            left + (buttons.pivot.x * size.x),
            bottom + (buttons.pivot.y * size.y),
            0f));

        pivotWorld.z = buttons.position.z;
        buttons.position = pivotWorld;
    }

    /// <summary>
    /// Checks whether a screen position lies on a UI rect, including while a canvas group
    /// on it is blocking raycasts.
    /// </summary>
    /// <param name="rect">The rect to test.</param>
    /// <param name="screenPos">The screen position to test.</param>
    /// <returns>True when the position is inside the rect.</returns>
    private bool IsPointerOverRect(RectTransform rect, Vector2 screenPos)
    {
        if (!rect.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas canvas = rect.GetComponentInParent<Canvas>();

        Camera uiCam =
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, uiCam);
    }
}