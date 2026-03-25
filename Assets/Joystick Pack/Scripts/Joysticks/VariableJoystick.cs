using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VariableJoystick : Joystick
{
    public float MoveThreshold { get { return this.moveThreshold; } set { this.moveThreshold = Mathf.Abs(value); } }

    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;

    private Vector2 fixedPosition = Vector2.zero;

    public void SetMode(JoystickType joystickType)
    {
        this.joystickType = joystickType;
        if(joystickType == JoystickType.Fixed)
        {
            this.background.anchoredPosition = this.fixedPosition;
            this.background.gameObject.SetActive(true);
        }
        else
            this.background.gameObject.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        this.fixedPosition = this.background.anchoredPosition;
        this.SetMode(this.joystickType);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if(this.joystickType != JoystickType.Fixed)
        {
            this.background.anchoredPosition = this.ScreenPointToAnchoredPosition(eventData.position);
            this.background.gameObject.SetActive(true);
        }
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if(this.joystickType != JoystickType.Fixed)
            this.background.gameObject.SetActive(false);

        base.OnPointerUp(eventData);
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (this.joystickType == JoystickType.Dynamic && magnitude > this.moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - this.moveThreshold) * radius;
            this.background.anchoredPosition += difference;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }
}

public enum JoystickType { Fixed, Floating, Dynamic }