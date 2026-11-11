using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    /// Time in seconds after the selection buttons appear during which presses are ignored.
    /// </summary>
    private const float ButtonPressDelay = 0.1f;

    /// <summary>
    /// Gap between the selection and the button panel, in canvas units (the local space of
    /// the selection box parent). Higher values push the panel further away from the selection.
    /// Independent of screen size and of the panel's own height.
    /// </summary>
    [SerializeField]
    private float buttonGap = 40f;

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

    private bool isDragging;
    private bool hasDragged;
    private bool isMovementMode;
    private bool isMoving;
    private bool isReferenceSelectionMode;
    private Grid grid;

    private float buttonsBlockedUntil;

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
    /// Gets or sets the current position of the used pointer.
    /// </summary>
    public Vector2 CurrentPos
    {
        get => this.currentPos;
        set => this.currentPos = value;
    }

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

        this.dragEnd = worldPos;

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
    /// Deletes all currently selected items.
    /// </summary>
    public abstract void DeleteSelected();

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
        this.DisableSelectionButtons();
        this.DisableMoveButtons();
    }

    /// <summary>
    /// Enables the selection buttons. When they were hidden before, presses on them
    /// are ignored for a short time.
    /// </summary>
    /// <param name="selectionRect">The rect of all selected objects.</param>
    protected void EnableSelectionButtons(Rect selectionRect)
    {
        if (!this.selectionButtons.gameObject.activeSelf)
        {
            this.buttonsBlockedUntil = Time.unscaledTime + ButtonPressDelay;
            this.selectionButtonsCanvasGroup.interactable = false;
            this.selectionButtonsCanvasGroup.blocksRaycasts = false;
        }

        this.PositionButtonsAboveRect(this.selectionButtons, selectionRect);
        this.selectionButtons.gameObject.SetActive(true);
    }

    /// <summary>
    /// Disables the selection buttons.
    /// </summary>
    protected void DisableSelectionButtons()
    {
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
        Vector2 screenPoint = this.cam.WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            this.selectionBoxParent,
            screenPoint,
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
    /// Updates the position of the UI buttons when something is selected.
    /// </summary>
    private void LateUpdate()
    {
        if (!this.selectionButtonsCanvasGroup.blocksRaycasts &&
            this.selectionButtons.gameObject.activeSelf &&
            Time.unscaledTime >= this.buttonsBlockedUntil)
        {
            this.selectionButtonsCanvasGroup.interactable = true;
            this.selectionButtonsCanvasGroup.blocksRaycasts = true;
        }

        if (!this.HasSelection)
        {
            return;
        }

        if (this.IsMovementMode)
        {
            if (this.moveButtons.gameObject.activeSelf)
            {
                this.PositionButtonsAboveRect(
                    this.moveButtons,
                    this.GetCurrentSelectionBounds());
            }

            return;
        }

        if (this.selectionButtons.gameObject.activeSelf)
        {
            this.PositionButtonsAboveRect(
                this.selectionButtons,
                this.GetCurrentSelectionBounds());
        }
    }
}