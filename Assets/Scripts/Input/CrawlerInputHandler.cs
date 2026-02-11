using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// Receives player input via the New Input System
/// and switches UI based on control scheme.
/// Methods are designed to be hooked to Input Actions or UI events.
/// </summary>
public class CrawlerInputHandler : MonoBehaviour
{
    [SerializeField]
    private CrawlerPlayerHandler player;
    [SerializeField]
    private GameObject mobileUIRoot;
    [SerializeField]
    private PlayerInput playerInput;
    [SerializeField]
    private Joystick joyStick;

    private bool usingTouch = false;

    /// <summary>
    /// Gets a value indicating whether we are using touch screen.
    /// </summary>
    public bool UsingTouch => this.usingTouch;

    /// <summary>
    /// Called automatically by PlayerInput when control scheme changes.
    /// </summary>
    /// <param name="input">The player input object.</param>
    public void OnControlsChanged(PlayerInput input)
    {
        this.UpdateControlScheme(input.currentControlScheme);
    }

    /// <summary>
    /// Called when the player input movement is changed.
    /// </summary>
    /// <param name="context">Input context.</param>
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!context.performed && !context.canceled)
        {
            return;
        }

        Vector2 move = context.ReadValue<Vector2>();
        this.player.SetMovement(move);
    }

    /// <summary>
    /// Called when the player interact button (keyboard) or the interaction object is pressed (mobile).
    /// </summary>
    /// <param name="context">Input context.</param>
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        Vector2 interaction = new Vector2(0, 0);

        // TODO: What does the player interact with?
        this.player.Interact(interaction);
    }

    /// <summary>
    /// Called when the mobile attack button is clicked or after the player input attack action is confirmed.
    /// </summary>
    public void OnAttackButton()
    {
        this.player.Attack();
    }

    /// <summary>
    /// Called when the player attack button is pressed.
    /// </summary>
    /// <param name="context">Input context.</param>
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        this.OnAttackButton();
    }

    /// <summary>
    /// Called when the mobile tool button is clicked or after the player input use tool action is confirmed.
    /// </summary>
    public void OnUseToolButton()
    {
        this.player.UseTool();
    }

    /// <summary>
    /// Called when the player tool button is pressed.
    /// </summary>
    /// <param name="context">Input context.</param>
    public void OnUseTool(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        this.OnUseToolButton();
    }

    /// <summary>
    /// Called when the mobile inventory button is clicked or after the player input use tool action is confirmed.
    /// </summary>
    public void OnOpenInventoryButton()
    {
        this.player.OpenInventory();
    }

    /// <summary>
    /// Called when the player inventory button is pressed.
    /// </summary>
    /// <param name="context">Input context.</param>
    public void OnOpenInventory(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        this.OnOpenInventoryButton();
    }

    private void Start()
    {
        this.UpdateControlScheme(this.playerInput.currentControlScheme);
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        // Detect first touch input to switch UI dynamically
        if (!this.usingTouch && Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            this.UpdateControlScheme("Touch");
        }

        // Add mobile movement support.
        if (this.usingTouch)
        {
            this.player.SetMovement(this.joyStick.Direction);
        }
    }

    private void UpdateControlScheme(string scheme)
    {
        bool isTouchScheme = scheme == "Touch";

        this.usingTouch = isTouchScheme;
        this.SetMobileUI(this.usingTouch);
    }

    private void SetMobileUI(bool show)
    {
        if (this.mobileUIRoot != null)
        {
            this.mobileUIRoot.SetActive(show);
        }
    }
}
