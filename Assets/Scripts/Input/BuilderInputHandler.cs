using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all player input for the map editor using Unity Events.
/// </summary>
public class BuilderInputHandler : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private TileEditorController tileEditor;
    [SerializeField] private EntityEditorController entityEditor;
    [SerializeField] private CameraController cameraController;

    /// <summary>
    /// Called when the pointer moves on screen (mouse or touch).
    /// Converts the screen position to world coordinates and updates the tile and entity editors.
    /// Hook this to the "Point" action in your Builder Action Map.
    /// </summary>
    /// <param name="ctx">The input action context from the PlayerInput system.</param>
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
    /// Called when the primary action is pressed or released (e.g., left mouse button or touch).
    /// Calls the brush/eraser actions on the tile and entity editors.
    /// Hook this to the "Primary" action in your Builder Action Map.
    /// </summary>
    /// <param name="ctx">The input action context from the PlayerInput system.</param>
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
    /// Deletes the currently selected entities.
    /// Hook this to the "Delete" action in your Builder Action Map.
    /// </summary>
    /// <param name="ctx">The input action context from the PlayerInput system.</param>
    public void OnDelete(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            this.entityEditor.OnDelete();
        }
    }

    /// <summary>
    /// Called when the zoom input is performed (e.g., mouse scroll or pinch gesture).
    /// Passes the zoom delta to the camera controller.
    /// Hook this to the "Zoom" action in your Builder Action Map.
    /// </summary>
    /// <param name="ctx">The input action context from the PlayerInput system.</param>
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
    /// Hook this to the "Pan" action in your Builder Action Map.
    /// </summary>
    /// <param name="ctx">The input action context from the PlayerInput system.</param>
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
    /// 
    /// </summary>
    /// <param name="ctx"></param>
    public void OnPointerDelta(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Vector2 delta = ctx.ReadValue<Vector2>();
        this.cameraController.OnPointerDelta(delta);
    }
}