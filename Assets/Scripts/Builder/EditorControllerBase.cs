using UnityEditor.Actions;
using UnityEngine;

/// <summary>
/// Base class for all editor controllers (tiles and entities).
/// Handles shared input state, pointer tracking, and tool execution flow.
/// </summary>
public abstract class EditorControllerBase : MonoBehaviour
{
    private const float DRAG_THRESHOLD = 0.1f;

    private Texture2D whiteTexture;
    private Camera cam;

    private Vector3 currentPos;
    private bool primaryHolding;
    private bool secondaryHolding;

    private bool isDragging;
    private bool hasDragged;

    private Vector3 dragStart;
    private Vector3 dragEnd;

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
    /// Gets a value indicating whether the player is currently dragging.
    /// </summary>
    protected bool IsDragging => this.isDragging;

    /// <summary>
    /// Gets the start position of the selection drag.
    /// </summary>
    protected Vector3 DragStart => this.dragStart;

    /// <summary>
    /// Gets the end position of the selection drag.
    /// </summary>
    protected Vector3 DragEnd => this.dragEnd;

    /// <summary>
    /// Gets the current world position of the pointer.
    /// </summary>
    protected Vector3 CurrentPos => this.currentPos;

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
            this.isDragging = true;
            this.hasDragged = false;
            this.dragStart = this.CurrentPos;
            this.dragEnd = this.CurrentPos;
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
            this.isDragging = false;

            if (this.hasDragged)
            {
                this.OnBoxSelect(this.dragStart, this.dragEnd);
            }
            else
            {
                this.OnClickSelect(this.CurrentPos);
            }

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
    public virtual void OnDelete()
    {
        // TODO: Add implementations.
    }

    /// <summary>
    /// Finds the camera.
    /// </summary>
    protected virtual void Awake()
    {
        this.cam = Camera.main;
    }

    /// <summary>
    /// Unity update loop. Applies tool continuously while input is held.
    /// </summary>
    protected virtual void Update()
    {
        if (this.CurrentTool == EditorTool.Selection && this.isDragging)
        {
            this.dragEnd = this.CurrentPos;

            if (!this.hasDragged &&
                Vector3.Distance(this.dragStart, this.dragEnd) > DRAG_THRESHOLD)
            {
                this.hasDragged = true;
            }
        }

        if (this.primaryHolding || this.secondaryHolding)
        {
            this.ApplyTool();
        }
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
    /// Called when the player clicks to select.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected abstract void OnClickSelect(Vector3 position);

    /// <summary>
    /// Gets the world rectangle of a selection drag.
    /// </summary>
    /// <param name="a">Position 1.</param>
    /// <param name="b">Position 2.</param>
    /// <returns>The rectangle of the drag.</returns>
    protected Rect GetWorldRect(Vector3 a, Vector3 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);

        return new Rect(min, max - min);
    }

    /// <summary>
    /// Called when the player drags to select.
    /// </summary>
    /// <param name="start">The start position of the drag selection.</param>
    /// <param name="end">The end position of the drag selection.</param>
    protected abstract void OnBoxSelect(Vector3 start, Vector3 end);

    private void OnGUI()
    {
        if (this.CurrentTool != EditorTool.Selection || !this.isDragging)
        {
            return;
        }

        Rect rect = this.GetScreenRect(this.dragStart, this.dragEnd);

        this.DrawScreenRect(rect, new Color(0f, 0.5f, 1f, 0.15f));
        this.DrawScreenRectBorder(rect, 2f, Color.cyan);
    }

    private Rect GetScreenRect(Vector3 worldA, Vector3 worldB)
    {
        Vector2 a = this.cam.WorldToScreenPoint(worldA);
        Vector2 b = this.cam.WorldToScreenPoint(worldB);

        a.y = Screen.height - a.y;
        b.y = Screen.height - b.y;

        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);

        return new Rect(min, max - min);
    }

    private void DrawScreenRect(Rect rect, Color color)
    {
        if (this.whiteTexture == null)
        {
            this.whiteTexture = Texture2D.whiteTexture;
        }

        GUI.color = color;
        GUI.DrawTexture(rect, this.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawScreenRectBorder(Rect rect, float thickness, Color color, float gap = 6f)
    {
        // Top
        this.DrawDashedLine(
            new Vector2(rect.xMin, rect.yMin),
            new Vector2(rect.xMax, rect.yMin),
            thickness,
            gap,
            color);

        // Bottom
        this.DrawDashedLine(
            new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax, rect.yMax),
            thickness,
            gap,
            color);

        // Left
        this.DrawDashedLine(
            new Vector2(rect.xMin, rect.yMin),
            new Vector2(rect.xMin, rect.yMax),
            thickness,
            gap,
            color);

        // Right
        this.DrawDashedLine(
            new Vector2(rect.xMax, rect.yMin),
            new Vector2(rect.xMax, rect.yMax),
            thickness,
            gap,
            color);
    }

    private void DrawDashedLine(Vector2 start, Vector2 end, float thickness, float gap, Color color)
    {
        float distance = Vector2.Distance(start, end);
        Vector2 direction = (end - start).normalized;

        float step = gap + thickness;

        for (float i = 0; i < distance; i += step)
        {
            Vector2 segmentStart = start + (direction * i);
            Vector2 segmentEnd = start + (direction * Mathf.Min(i + thickness, distance));

            Rect segment = this.GetRectFromPoints(segmentStart, segmentEnd, thickness);
            this.DrawScreenRect(segment, color);
        }
    }

    private Rect GetRectFromPoints(Vector2 a, Vector2 b, float thickness)
    {
        Vector2 delta = b - a;
        float length = delta.magnitude;

        if (length < 0.001f)
        {
            return new Rect(a.x, a.y, thickness, thickness);
        }

        Vector2 dir = delta / length;
        Vector2 perp = new Vector2(-dir.y, dir.x) * thickness * 0.5f;

        Vector2 p1 = a + perp;
        Vector2 p2 = a - perp;

        Vector2 p3 = b + perp;

        Vector2 min = Vector2.Min(p1, Vector2.Min(p2, p3));
        Vector2 max = Vector2.Max(p1, Vector2.Max(p2, p3));

        return new Rect(min, max - min);
    }
}