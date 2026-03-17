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
    private TileEditorController tileEditor;
    [SerializeField]
    private EntityEditorController entityEditor;
    [SerializeField]
    private CameraController cameraController;

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
            this.tileEditor.OnPrimaryDown();
            this.entityEditor.OnPrimaryDown();
        }

        if (ctx.canceled)
        {
            this.tileEditor.OnPrimaryUp();
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

        Vector2 delta = ctx.ReadValue<Vector2>();
        this.cameraController.OnZoom(delta.y);
    }

    /// <summary>
    /// Called when the pan input is performed. (Middle mouse button.)
    /// Passes the pan delta to the camera controller.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPan(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        Vector2 delta = ctx.ReadValue<Vector2>();
        this.cameraController.OnPan(delta);
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
        this.cameraController.OnPointerDelta(delta);
    }

    /// <summary>
    /// Pressed slot 0 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot0(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 0);

    /// <summary>
    /// Pressed slot 1 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot1(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 1);

    /// <summary>
    /// Pressed slot 2 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot2(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 2);

    /// <summary>
    /// Pressed slot 3 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot3(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 3);

    /// <summary>
    /// Pressed slot 4 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot4(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 4);

    /// <summary>
    /// Pressed slot 5 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot5(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 5);

    /// <summary>
    /// Pressed slot 6 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot6(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 6);

    /// <summary>
    /// Pressed slot 7 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot7(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 7);

    /// <summary>
    /// Pressed slot 8 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot8(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 8);

    /// <summary>
    /// Pressed slot 9 button.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSlot9(InputAction.CallbackContext ctx) => this.OnSelectSlot(ctx, 9);

    private void OnSelectSlot(InputAction.CallbackContext ctx, int index)
    {
        if (!ctx.performed)
        {
            return;
        }

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
}