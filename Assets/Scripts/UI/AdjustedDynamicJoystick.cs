using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Our own implementation of the Joystick class.
/// This version doesnt disappear, but instead slowly moves back to the original joystick location.
/// </summary>
public class AdjustedDynamicJoystick : Joystick
{

    [SerializeField]
    private float returnSpeed = 15f;

    private Vector2 startPosition;
    private bool returningToStart = false;

    [SerializeField] private float moveThreshold = 1;

    /// <summary>
    /// Gets or sets the threshold of the joystick.
    /// </summary>
    public float MoveThreshold
    {
        get { return this.moveThreshold; }
        set { this.moveThreshold = Mathf.Abs(value); }
    }

    /// <summary>
    /// Called when touching the joystick.
    /// </summary>
    /// <param name="eventData">The pointer data.</param>
    public override void OnPointerDown(PointerEventData eventData)
    {
        this.returningToStart = false;

        this.background.anchoredPosition = this.ScreenPointToAnchoredPosition(eventData.position);
        this.background.gameObject.SetActive(true);

        base.OnPointerDown(eventData);
    }

    /// <summary>
    /// Called when letting go of the joystick.
    /// </summary>
    /// <param name="eventData">The pointer data.</param>
    public override void OnPointerUp(PointerEventData eventData)
    {
        this.returningToStart = true;

        base.OnPointerUp(eventData);
    }

    /// <summary>
    /// Handles the input.
    /// </summary>
    ///<param name="magnitude">The magnitude of the drag movement. </param>
    ///<param name="normalised">The normalized direction of the joystick.</param>
    ///<param name="radius">The radius of the joystick.</param>
    ///<param name="cam">The camera to which the joystick is attached.</param>
    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (magnitude > this.moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - this.moveThreshold) * radius;
            this.background.anchoredPosition += difference;
        }

        base.HandleInput(magnitude, normalised, radius, cam);
    }

    /// <summary>
    /// Sets the move threshold.
    /// </summary>
    protected override void Start()
    {
        this.MoveThreshold = this.moveThreshold;
        base.Start();
        this.startPosition = this.background.anchoredPosition;
    }

    private void Update()
    {
        if (this.returningToStart)
        {
            this.background.anchoredPosition = Vector2.Lerp(
                this.background.anchoredPosition,
                this.startPosition,
                this.returnSpeed * Time.deltaTime
            );

            if (Vector2.Distance(this.background.anchoredPosition, this.startPosition) < 0.1f)
            {
                this.background.anchoredPosition = this.startPosition;
                this.returningToStart = false;
            }
        }
    }
}