using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all player input for the map editor using Unity Events.
/// </summary>
public class BuilderInputHandler : MonoBehaviour
{
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

    private bool isMiddleMouseHeld;
    private bool isPrimaryHeld;

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
        return this.isMiddleMouseHeld || (this.isPrimaryHeld && this.mapEditorManager.CurrentTool == EditorTool.Drag);
    }
}