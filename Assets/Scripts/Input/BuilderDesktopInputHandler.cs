using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all desktop input (mouse + keyboard) for the map editor.
/// </summary>
public class BuilderDesktopInputHandler : MonoBehaviour
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
    [SerializeField]
    private float desktopZoomSpeed = 0.2f;

    private bool isPrimaryHeld;
    private bool isMiddleMouseHeld;
    private bool command;

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
    /// Action depends on position and tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPrimary(InputAction.CallbackContext ctx)
    {
        Vector2 pointerPos = Mouse.current.position.ReadValue();
        if (this.IsPointerOverUI(pointerPos))
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
        else if (ctx.canceled)
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

            if (this.mapEditorManager.CurrentTool == EditorTool.Drag)
            {
                this.cameraController.EndPan();
            }
        }
    }

    /// <summary>
    /// Called when the secondary button is pressed (right mouse button).
    /// Used for erasing when we are using the brush tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSecondary(InputAction.CallbackContext ctx)
    {
        Vector2 pointerPos = Mouse.current.position.ReadValue();
        if (this.IsPointerOverUI(pointerPos))
        {
            return;
        }

        if (ctx.started)
        {
            if (this.mapEditorManager.CurrentLayer == EditLayer.Background)
            {
                this.tileEditor.OnSecondaryDown();
            }
            else
            {
                this.entityEditor.OnSecondaryDown();
            }
        }
        else if (ctx.canceled)
        {
            if (this.mapEditorManager.CurrentLayer == EditLayer.Background)
            {
                this.tileEditor.OnSecondaryUp();
            }
            else
            {
                this.entityEditor.OnSecondaryUp();
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
        this.cameraController.OnZoom(delta * this.desktopZoomSpeed, pointerPos);
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
        else if (ctx.canceled)
        {
            this.isMiddleMouseHeld = false;
            this.cameraController.EndPan();
        }
    }

    /// <summary>
    /// Moves the camera based on WASD / arrow keys input.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnMoveCamera(InputAction.CallbackContext ctx)
    {
        Vector2 dir = ctx.ReadValue<Vector2>();
        if (ctx.canceled)
        {
            dir = Vector2.zero;
        }

        this.cameraController.OnMove(dir);
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
    /// Selects the next tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSwitchTool(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        this.mapEditorManager.SelectNextTool();
    }

    /// <summary>
    /// Selects the next tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSwitchLayer(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        this.mapEditorManager.ToggleLayer();
    }

    /// <summary>
    /// Enables the player to do a command action: (E.g. command + z undoes, command + r redoes, command + s saves).
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnCommand(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            this.command = true;
        }
        else if (ctx.canceled)
        {
            this.command = false;
        }
    }

    private void SelectSlot(int index)
    {
        // TODO: Slot selection with both entities and tiles.
    }

    private void Update()
    {
        this.HandleMouseMovement();
        this.HandleCameraPan();
    }

    private void HandleMouseMovement()
    {
        Vector2 pointerPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = this.cam.ScreenToWorldPoint(pointerPos);
        worldPos.z = 0;
        this.tileEditor.OnPointerMoved(worldPos);
        this.entityEditor.OnPointerMoved(worldPos);
    }

    private void HandleCameraPan()
    {
        if (this.isMiddleMouseHeld || (this.isPrimaryHeld && this.mapEditorManager.CurrentTool == EditorTool.Drag))
        {
            Vector2 pointerPos = Mouse.current.position.ReadValue();
            this.cameraController.Pan(pointerPos);
        }
    }

    private bool IsPointerOverUI(Vector2 pos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = pos,
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}