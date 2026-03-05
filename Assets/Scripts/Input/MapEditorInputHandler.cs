using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The map editor input handler.
/// </summary>
public class MapEditorInputHandler : MonoBehaviour
{
    private Camera cam;

    /// <summary>
    /// Called when a pointer points at a new position.
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

        EditorEventBus.RaisePointerMoved(world);
    }

    /// <summary>
    /// Called on the primary action (either tap or right click).
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPrimary(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            EditorEventBus.RaisePrimaryDown();
        }

        if (ctx.canceled)
        {
            EditorEventBus.RaisePrimaryUp();
        }
    }

    /// <summary>
    /// Called on the delete action.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnDelete(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            EditorEventBus.RaiseDelete();
        }
    }

    /// <summary>
    /// Called on the zoom action.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnZoom(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        Vector2 delta = ctx.ReadValue<Vector2>();
        EditorEventBus.RaiseZoom(delta.y);
    }

    /// <summary>
    /// Called on the pan action.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPan(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        Vector2 delta = ctx.ReadValue<Vector2>();
        EditorEventBus.RaisePan(delta);
    }

    private void Awake()
    {
        this.cam = Camera.main;
    }
}