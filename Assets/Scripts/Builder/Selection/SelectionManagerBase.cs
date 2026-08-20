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
    /// The selection box UI object.
    /// </summary>
    [SerializeField]
    private SelectionBox selectionBox;

    /// <summary>
    /// The canvas the selection box is in.
    /// </summary>
    [SerializeField]
    private RectTransform selectionBoxParent;

    private Camera cam;

    private bool isDragging;
    private bool hasDragged;

    private Vector3 dragStart;
    private Vector3 dragEnd;

    /// <summary>
    /// Gets a value indicating whether a selection drag is currently in progress.
    /// </summary>
    public bool IsDragging => this.isDragging;

    /// <summary>
    /// Begins a selection drag at the given world position.
    /// </summary>
    /// <param name="worldPos">The pointer's world position at drag start.</param>
    public void BeginDrag(Vector2 worldPos)
    {
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
    /// Finds the camera used to convert world positions to UI positions.
    /// </summary>
    protected virtual void Awake()
    {
        this.cam = Camera.main;
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
    /// Finds the camera used to convert world positions to UI positions.
    /// </summary>
    protected virtual void Awake()
    {
        this.cam = Camera.main;
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
}
