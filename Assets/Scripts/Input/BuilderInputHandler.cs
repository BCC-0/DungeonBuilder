using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all player input for the map editor using Unity Events.
/// </summary>
public class BuilderInputHandler : MonoBehaviour
{
    private static bool isPointerOverUI;

    [SerializeField]
    private Camera cam;
    [SerializeField]
    private MapEditorManager mapEditorManager;
    [SerializeField]
    private TileEditorController tileEditor;
    [SerializeField]
    private EntityEditorController entityEditor;
    [SerializeField]
    private CameraController cameraController;
    [SerializeField]
    private float holdThreshold = 0.2f;
    [SerializeField]
    private float mobileZoomSpeed = 0.2f;
    [SerializeField]
    private float mobilePanSpeed = 0.2f;

    private bool isMiddleMouseHeld;
    private bool isPrimaryHeld;

    private bool isTouching;
    private float touchHoldTimer;
    private float lastPinchDistance;

    /// <summary>
    /// Called when the pointer is moved.
    /// Finds the position of the pointer and communicates it to the tileEditor and the entityEditor.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPoint(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        Vector2 screen = ctx.ReadValue<Vector2>();
        Vector3 world = this.cam.ScreenToWorldPoint(screen);
        world.z = 0;

        this.tileEditor.OnPointerMoved(world);
        this.entityEditor.OnPointerMoved(world);
    }

    /// <summary>
    /// Called when the primary button is called.
    /// Action depends on position.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPrimary(InputAction.CallbackContext ctx)
    {
        if (isPointerOverUI)
        {
            return;
        }

        if (ctx.started)
        {
            this.isPrimaryHeld = true;

            if (this.mapEditorManager.CurrentLayer == EditLayer.Background)
            {
                this.tileEditor.OnPrimaryDown();
            }
            else
            {
                this.entityEditor.OnPrimaryDown();
            }
        }

        if (ctx.canceled)
        {
            this.isPrimaryHeld = false;

            if (this.mapEditorManager.CurrentLayer == EditLayer.Background)
            {
                this.tileEditor.OnPrimaryUp();
            }
            else
            {
                this.entityEditor.OnPrimaryUp();
            }
        }
    }

    /// <summary>
    /// Called when the delete key or button is pressed.
    /// Deletes the currently selected entity/entities.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnDelete(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            this.entityEditor.OnDelete();
        }
    }

    /// <summary>
    /// Called when the zoom input is performed (Mouse scroll or pinch gesture).
    /// Passes the zoom delta to the camera controller.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnZoom(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        float delta = ctx.ReadValue<float>();
        Vector2 pointerPos = Mouse.current.position.ReadValue();

        this.cameraController.OnZoom(delta * 5f, pointerPos);
    }

    /// <summary>
    /// Called when the pan input is performed. (Middle mouse button.)
    /// Passes the pan delta to the camera controller.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPan(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            this.isMiddleMouseHeld = true;
        }

        if (ctx.canceled)
        {
            this.isMiddleMouseHeld = false;
        }
    }

    /// <summary>
    /// Called when the pointer is moved.
    /// Finds the difference in pointer position, is used to move the camera.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPointerDelta(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        Vector2 delta = ctx.ReadValue<Vector2>();

        if (this.ShouldPan())
        {
            this.cameraController.Pan(delta);
        }
    }

    /// <summary>
    /// Moves the camera based on WASD / arrow keys input.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnMoveCamera(InputAction.CallbackContext ctx)
    {
        // Read input vector (normalized in digital mode)
        Vector2 cameraMoveDirection = ctx.ReadValue<Vector2>();

        // Optional: set to zero on cancel
        if (ctx.canceled)
        {
            cameraMoveDirection = Vector2.zero;
        }

        // Pass the direction to camera controller
        this.cameraController.OnMove(cameraMoveDirection);
    }

    /// <summary>
    /// Selects a slot based on bound button pressed.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        int index = (int)ctx.ReadValue<float>(); // or from binding
        Debug.Log("Selected slot " + index);
        this.SelectSlot(index);
    }

    /// <summary>
    /// Called when the player tries to select a slot (with entities or tiles).
    /// Finds the difference in pointer position, is used to move the camera.
    /// </summary>
    /// <param name="index">The index of the slot.</param>
    private void SelectSlot(int index)
    {
        // TODO: Slot selection with both entities and tiles.
    }

    /// <summary>
    /// Checks if we should pan, which is either:
    /// * middleMouse + drag
    /// * drag tool + click + drag.
    /// </summary>
    /// <returns>A value indicating whether we should pan.</returns>
    private bool ShouldPan()
    {
        if (isPointerOverUI)
        {
            return false;
        }

        return this.isMiddleMouseHeld ||
               (this.isPrimaryHeld && this.mapEditorManager.CurrentTool == EditorTool.Drag);
    }

    private void Update()
    {
        isPointerOverUI = EventSystem.current != null &&
                          EventSystem.current.IsPointerOverGameObject();
        this.HandleTouch();
    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
        {
            return;
        }

        var touches = Touchscreen.current.touches;
        var activeTouches = touches.Where(t => t.isInProgress).ToArray();

        // Remove all possible actions when not touching.
        if (activeTouches.Length == 0)
        {
            this.isTouching = false;
            this.touchHoldTimer = 0;
            this.lastPinchDistance = 0;
            return;
        }

        // For dragging or placing.
        if (activeTouches.Length == 1)
        {
            var touch = activeTouches[0];

            Vector2 delta = touch.delta.ReadValue();

            if (!this.isTouching)
            {
                this.isTouching = true;
                this.touchHoldTimer = 0;
            }

            this.touchHoldTimer += Time.deltaTime;

            bool isDragTool = this.mapEditorManager.CurrentTool == EditorTool.Drag;
            bool canPan = isDragTool || this.touchHoldTimer > this.holdThreshold;

            if (!isPointerOverUI && canPan)
            {
                this.cameraController.Pan(delta * this.mobilePanSpeed);
            }
        }

        // For zooming.
        if (activeTouches.Length >= 2)
        {
            var t1 = activeTouches[0];
            var t2 = activeTouches[1];

            Vector2 p1 = t1.position.ReadValue();
            Vector2 p2 = t2.position.ReadValue();

            float currentDistance = Vector2.Distance(p1, p2);

            if (this.lastPinchDistance > 0)
            {
                float delta = currentDistance - this.lastPinchDistance;
                Vector2 center = (p1 + p2) * 0.5f;

                this.cameraController.OnZoom(delta * this.mobileZoomSpeed, center);
            }

            this.lastPinchDistance = currentDistance;
        }
        else
        {
            this.lastPinchDistance = 0;
        }
    }
}
