using System.Collections.Generic;
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
    /// Vertical padding (in UI units) between the top of a selection rect and the buttons above it.
    /// </summary>
    private const float ButtonVerticalPadding = 100f;

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
    /// The UI element containing buttons with actions possible when moving (mobile only).
    /// </summary>
    [SerializeField]
    private RectTransform moveButtons;

    private Camera cam;

    private bool isDragging;
    private bool hasDragged;
    private bool isMovingSelected;
    private Grid grid;

    private Vector2 dragStart;
    private Vector2 dragEnd;
    private Vector2 currentPos;

    /// <summary>
    /// Gets a value indicating whether a selection drag is currently in progress.
    /// </summary>
    public bool IsDragging => this.isDragging;

    /// <summary>
    /// Gets or sets the current position of the used pointer.
    /// </summary>
    public Vector2 CurrentPos
    {
        get => this.currentPos;
        set => this.currentPos = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether we are currently moving the selected items.
    /// </summary>
    protected bool IsMovingSelected
    {
        get => this.isMovingSelected;
        set => this.isMovingSelected = value;
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
    /// Begins a selection drag at the given world position.
    /// </summary>
    /// <param name="worldPos">The pointer's world position at drag start.</param>
    public void BeginDrag(Vector2 worldPos)
    {
        Debug.Log("Begin drag");
        if (this.IsMovingSelected)
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
        if (!this.isDragging)
        {
            return;
        }

        this.isDragging = false;

        if (this.hasDragged)
        {
            this.OnBoxSelect(this.GetWorldRect(this.dragStart, this.dragEnd));
            this.selectionBox.StopSelection();
        }
        else
        {
            this.OnClickSelect(this.dragEnd);
        }
    }

    /// <summary>
    /// Clears the current selection and refreshes the visualizer.
    /// </summary>
    public abstract void ClearSelection();

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
    /// Rebaselines an in-progress move so that further dragging continues from
    /// the current preview position rather than jumping back to the original
    /// touch-down point. Used on mobile, where lifting and retouching mid-move
    /// is a new gesture, not a continuous drag.
    /// </summary>
    /// <param name="worldPos">The pointer's world position at the new touch-down.</param>
    protected abstract void ContinueMove(Vector2 worldPos);

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
    /// Enables the selection buttons.
    /// </summary>
    /// <param name="selectionRect">The rect of all selected objects,</param>
    protected void EnableSelectionButtons(Rect selectionRect)
    {
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
    /// <param name="selectionRect">The rect of all selected objects,</param>
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
            this.selectionBoxParent, screenPoint, this.cam, out Vector2 localPoint);
        return localPoint;
    }

    /// <summary>
    /// Positions a button panel centered above the top edge of the given world-space rect.
    /// </summary>
    /// <param name="buttons">The button panel's RectTransform to position.</param>
    /// <param name="worldRect">The world-space rect (e.g. selection bounds) to anchor above.</param>
    private void PositionButtonsAboveRect(RectTransform buttons, Rect worldRect)
    {
        Vector2 topCenterWorld = new Vector2(worldRect.center.x, worldRect.yMax);

        Vector2 localPoint = this.WorldToLocalUiPoint(topCenterWorld);

        buttons.anchoredPosition = localPoint + new Vector2(0f, ButtonVerticalPadding);
    }
}
