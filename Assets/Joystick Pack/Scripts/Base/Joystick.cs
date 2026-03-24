using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float Horizontal { get { return (this.snapX) ? this.SnapFloat(this.input.x, AxisOptions.Horizontal) : this.input.x; } }
    public float Vertical { get { return (this.snapY) ? this.SnapFloat(this.input.y, AxisOptions.Vertical) : this.input.y; } }
    public Vector2 Direction { get { return new Vector2(this.Horizontal, this.Vertical); } }

    public float HandleRange
    {
        get { return this.handleRange; }
        set { this.handleRange = Mathf.Abs(value); }
    }

    public float DeadZone
    {
        get { return this.deadZone; }
        set { this.deadZone = Mathf.Abs(value); }
    }

    public AxisOptions AxisOptions { get { return this.AxisOptions; } set { this.axisOptions = value; } }
    public bool SnapX { get { return this.snapX; } set { this.snapX = value; } }
    public bool SnapY { get { return this.snapY; } set { this.snapY = value; } }

    [SerializeField] private float handleRange = 1;
    [SerializeField] private float deadZone = 0;
    [SerializeField] private AxisOptions axisOptions = AxisOptions.Both;
    [SerializeField] private bool snapX = false;
    [SerializeField] private bool snapY = false;

    [SerializeField] protected RectTransform background = null;
    [SerializeField] private RectTransform handle = null;
    private RectTransform baseRect = null;

    private Canvas canvas;
    private Camera cam;

    private Vector2 input = Vector2.zero;

    protected virtual void Start()
    {
        this.HandleRange = this.handleRange;
        this.DeadZone = this.deadZone;
        this.baseRect = this.GetComponent<RectTransform>();
        this.canvas = this.GetComponentInParent<Canvas>();
        if (this.canvas == null)
            Debug.LogError("The Joystick is not placed inside a canvas");

        Vector2 center = new Vector2(0.5f, 0.5f);
        this.background.pivot = center;
        this.handle.anchorMin = center;
        this.handle.anchorMax = center;
        this.handle.pivot = center;
        this.handle.anchoredPosition = Vector2.zero;
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        this.OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        this.cam = null;
        if (this.canvas.renderMode == RenderMode.ScreenSpaceCamera)
            this.cam = this.canvas.worldCamera;

        Vector2 position = RectTransformUtility.WorldToScreenPoint(this.cam, this.background.position);
        Vector2 radius = this.background.sizeDelta / 2;
        this.input = (eventData.position - position) / (radius * this.canvas.scaleFactor);
        this.FormatInput();
        this.HandleInput(this.input.magnitude, this.input.normalized, radius, this.cam);
        this.handle.anchoredPosition = this.input * radius * this.handleRange;
    }

    protected virtual void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (magnitude > this.deadZone)
        {
            if (magnitude > 1)
                this.input = normalised;
        }
        else
            this.input = Vector2.zero;
    }

    private void FormatInput()
    {
        if (this.axisOptions == AxisOptions.Horizontal)
            this.input = new Vector2(this.input.x, 0f);
        else if (this.axisOptions == AxisOptions.Vertical)
            this.input = new Vector2(0f, this.input.y);
    }

    private float SnapFloat(float value, AxisOptions snapAxis)
    {
        if (value == 0)
            return value;

        if (this.axisOptions == AxisOptions.Both)
        {
            float angle = Vector2.Angle(this.input, Vector2.up);
            if (snapAxis == AxisOptions.Horizontal)
            {
                if (angle < 22.5f || angle > 157.5f)
                    return 0;
                else
                    return (value > 0) ? 1 : -1;
            }
            else if (snapAxis == AxisOptions.Vertical)
            {
                if (angle > 67.5f && angle < 112.5f)
                    return 0;
                else
                    return (value > 0) ? 1 : -1;
            }
            return value;
        }
        else
        {
            if (value > 0)
                return 1;
            if (value < 0)
                return -1;
        }
        return 0;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        this.input = Vector2.zero;
        this.handle.anchoredPosition = Vector2.zero;
    }

    protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
    {
        Vector2 localPoint = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this.baseRect, screenPosition, this.cam, out localPoint))
        {
            Vector2 pivotOffset = this.baseRect.pivot * this.baseRect.sizeDelta;
            return localPoint - (this.background.anchorMax * this.baseRect.sizeDelta) + pivotOffset;
        }
        return Vector2.zero;
    }
}

public enum AxisOptions { Both, Horizontal, Vertical }