using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dynamically switches between desktop and mobile input handlers based on the latest input type.
/// </summary>
public class BuilderInputSelector : MonoBehaviour
{
    private static BuilderInputSelector instance;

    [SerializeField]
    private BuilderDesktopInputHandler desktopInputHandler;
    [SerializeField]
    private BuilderMobileInputHandler mobileInputHandler;

    private ActiveInputType activeInput = ActiveInputType.None;

    private enum ActiveInputType
    {
        None,
        Desktop,
        Mobile,
    }

    /// <summary>
    /// Gets the instance of the BuilderInputSelector.
    /// </summary>
    public static BuilderInputSelector Instance
    {
        get => instance;
        private set => instance = value;
    }

    /// <summary>
    /// Gets a value indicating whether we are using desktop input currently.
    /// </summary>
    public bool IsUsingDesktop => this.desktopInputHandler.enabled;

    private void Awake()
    {
        // Start with both disabled
        Instance = this;
        this.desktopInputHandler.enabled = false;
        this.mobileInputHandler.enabled = false;
    }

    private void Update()
    {
        // Check for touch input first
        if (Touchscreen.current != null && Touchscreen.current.touches.Any(t => t.isInProgress))
        {
            if (this.activeInput != ActiveInputType.Mobile)
            {
                this.EnableMobileInput();
            }

            return;
        }

        // Check for desktop input
        if ((Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)) ||
            (Keyboard.current != null && Keyboard.current.anyKey.isPressed))
        {
            if (this.activeInput != ActiveInputType.Desktop)
            {
                this.EnableDesktopInput();
            }
        }
    }

    private void EnableMobileInput()
    {
        this.activeInput = ActiveInputType.Mobile;
        this.mobileInputHandler.enabled = true;
        this.desktopInputHandler.enabled = false;
        Debug.Log("Switching to mobile input handler.");
    }

    private void EnableDesktopInput()
    {
        this.activeInput = ActiveInputType.Desktop;
        this.desktopInputHandler.enabled = true;
        this.mobileInputHandler.enabled = false;
        Debug.Log("Switching to desktop input handler.");
    }
}