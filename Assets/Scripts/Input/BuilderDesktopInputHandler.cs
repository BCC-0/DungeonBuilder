using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Handles all desktop input (mouse + keyboard) for the map editor.
/// </summary>
public class BuilderDesktopInputHandler : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private CameraController cameraController;
    [SerializeField]
    private InventoryController inventoryController;
    [SerializeField]
    private float desktopZoomSpeed = 0.2f;
    [SerializeField]
    private float dragClickThreshold = 5f;

    private bool isPrimaryHeld;
    private bool isMiddleMouseHeld;
    private bool isDragClickPending;
    private Vector2 dragClickStartPos;

    private bool command;

    private EditorControllerBase ActiveController => MapEditorManager.Instance.ActiveController;

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

        this.ActiveController.OnPointerMoved(world);
    }

    /// <summary>
    /// Called when the primary button is called.
    /// Action depends on position and tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnPrimary(InputAction.CallbackContext ctx)
    {
        Vector2 pointerPos = Mouse.current.position.ReadValue();

        // A left click also confirms a move, so it is not passed on to the tools.
        if (MapEditorManager.Instance.IsMovingSelection)
        {
            return;
        }

        if (this.IsPointerOverUI(pointerPos) ||
            MapEditorManager.Instance.IsPointerOverSelectionButtons(pointerPos))
        {
            return;
        }

        if (ctx.started)
        {
            this.isPrimaryHeld = true;
            this.isDragClickPending =
                MapEditorManager.Instance.CurrentTool == EditorTool.Drag;
            this.dragClickStartPos = pointerPos;

            MapEditorManager.Instance.HideSelectionMenu();

            this.ActiveController.OnPrimaryDown();
        }
        else if (ctx.canceled)
        {
            this.isPrimaryHeld = false;
            this.ActiveController.OnPrimaryUp();

            if (MapEditorManager.Instance.CurrentTool == EditorTool.Drag)
            {
                this.cameraController.EndPan();

                if (this.isDragClickPending &&
                    Vector2.Distance(pointerPos, this.dragClickStartPos) <= this.dragClickThreshold)
                {
                    this.SelectAtPointer(pointerPos);
                }

                this.isDragClickPending = false;
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

        if (ctx.started &&
            !this.ToolErasesOnSecondary() &&
            this.TryOpenSelectionMenu(pointerPos))
        {
            return;
        }

        if (ctx.started)
        {
            this.ActiveController.OnSecondaryDown();
        }
        else if (ctx.canceled)
        {
            this.ActiveController.OnSecondaryUp();
        }
    }

    /// <summary>
    /// Opens or closes the selection menu for a right click. The menu is available in
    /// every tool, so it is not tied to the selection tool.
    /// </summary>
    /// <param name="pointerPos">The screen position that was right clicked.</param>
    /// <returns>True when the press was used by the menu and must not reach the tool.</returns>
    private bool TryOpenSelectionMenu(Vector2 pointerPos)
    {
        Vector3 worldPos = this.cam.ScreenToWorldPoint(pointerPos);
        worldPos.z = 0;

        return MapEditorManager.Instance.ToggleSelectionMenu(worldPos);
    }

    /// <summary>
    /// Gets a value indicating whether the active tool erases with the secondary button.
    /// Those tools keep the right click for erasing instead of opening the selection menu.
    /// </summary>
    /// <returns>True when the right click belongs to the tool.</returns>
    private bool ToolErasesOnSecondary()
    {
        EditorTool tool = MapEditorManager.Instance.CurrentTool;

        return tool == EditorTool.Brush || tool == EditorTool.Eraser;
    }

    /// <summary>
    /// Called when the inventory button is pressed (E).
    /// Opens the inventory and pauses the rest of the builder.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnOpenInventory(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || this.IsTyping())
        {
            return;
        }

        this.CancelCameraInput();
        this.inventoryController.OpenInventory();
    }

    /// <summary>
    /// Deletes the currently selected objects.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnDelete(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (ctx.started)
        {
            this.ActiveController.OnDeleteSelection();
        }
    }

    /// <summary>
    /// Starts moving the currently selected objects.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (ctx.started)
        {
            this.ActiveController.OnMoveSelection();
        }
    }

    /// <summary>
    /// Confirms the current position while moving items.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnConfirmMove(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (ctx.started)
        {
            this.ActiveController.OnConfirmMove();
        }
    }

    /// <summary>
    /// Cancels the move action and places the objects back.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnCancelMove(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (ctx.started)
        {
            this.ActiveController.OnCancelMove();
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

        Vector2 pointerPos = Mouse.current.position.ReadValue();

        if (this.IsPointerOverUI(pointerPos))
        {
            return;
        }

        float delta = ctx.ReadValue<float>();
        this.cameraController.OnZoom(
            delta * this.desktopZoomSpeed,
            pointerPos);
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
        if (this.IsTyping())
        {
            this.CancelCameraInput();
            return;
        }

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
        if (this.IsTyping())
        {
            return;
        }

        if (!ctx.performed)
        {
            return;
        }

        int index = (int)ctx.ReadValue<float>();

        if (index < 1 || index > 9)
        {
            return;
        }

        this.inventoryController.SelectSlot(index - 1);
    }

    /// <summary>
    /// Selects the next tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSwitchTool(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (!ctx.performed)
        {
            return;
        }

        MapEditorManager.Instance.SelectNextTool();
    }

    /// <summary>
    /// Selects the drag tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectDragTool(InputAction.CallbackContext ctx)
    {
        if (!this.CanSelectTool(ctx))
        {
            return;
        }

        MapEditorManager.Instance.SelectDrag();
    }

    /// <summary>
    /// Selects the brush tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectBrushTool(InputAction.CallbackContext ctx)
    {
        if (!this.CanSelectTool(ctx))
        {
            return;
        }

        MapEditorManager.Instance.SelectBrush();
    }

    /// <summary>
    /// Selects the eraser tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectEraserTool(InputAction.CallbackContext ctx)
    {
        if (!this.CanSelectTool(ctx))
        {
            return;
        }

        MapEditorManager.Instance.SelectEraser();
    }

    /// <summary>
    /// Selects the selection tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectSelectionTool(InputAction.CallbackContext ctx)
    {
        if (!this.CanSelectTool(ctx))
        {
            return;
        }

        MapEditorManager.Instance.SelectSelection();
    }

    /// <summary>
    /// Selects the pick tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSelectPickTool(InputAction.CallbackContext ctx)
    {
        if (!this.CanSelectTool(ctx))
        {
            return;
        }

        MapEditorManager.Instance.SelectPick();
    }

    /// <summary>
    /// Selects the next tool.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSwitchLayer(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (!ctx.performed)
        {
            return;
        }

        MapEditorManager.Instance.ToggleLayer();
    }

    /// <summary>
    /// Enables the player to do a command action: (E.g. command + z undoes, command + y redoes, command + s saves).
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnCommand(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (ctx.started)
        {
            this.command = true;
        }
        else if (ctx.canceled)
        {
            this.command = false;
        }
    }

    /// <summary>
    /// Ctrl + S should save the game.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnSave(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (ctx.started && this.IsCommandHeld())
        {
            MapEditorManager.Instance.SaveMap();
        }
    }

    /// <summary>
    /// Ctrl + Z should undo.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnUndo(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (ctx.started && this.IsCommandHeld())
        {
            MapEditorManager.Instance.Undo();
        }
    }

    /// <summary>
    /// Ctrl + Y should undo.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    public void OnRedo(InputAction.CallbackContext ctx)
    {
        if (this.IsTyping())
        {
            return;
        }

        if (ctx.started && this.IsCommandHeld())
        {
            MapEditorManager.Instance.Redo();
        }
    }

    private void Update()
    {
        if (this.IsTyping())
        {
            this.CancelCameraInput();
            return;
        }

        this.HandleClipboardShortcuts();
        this.HandleMouseMovement();
        this.HandleCameraPan();
    }

    /// <summary>
    /// Polls the clipboard shortcuts (Ctrl + C, Ctrl + X, Ctrl + V). They are read
    /// straight from the keyboard so no input action or asset change is needed.
    /// </summary>
    private void HandleClipboardShortcuts()
    {
        if (Keyboard.current == null ||
            !this.IsCommandHeld() ||
            MapEditorManager.Instance.IsMovingSelection)
        {
            return;
        }

        if (Keyboard.current[Key.C].wasPressedThisFrame)
        {
            MapEditorManager.Instance.OnCopySelection();
        }
        else if (Keyboard.current[Key.X].wasPressedThisFrame)
        {
            MapEditorManager.Instance.OnCutSelection();
        }
        else if (Keyboard.current[Key.V].wasPressedThisFrame)
        {
            MapEditorManager.Instance.OnPasteSelection();
        }
    }

    private void HandleMouseMovement()
    {
        Vector2 pointerPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = this.cam.ScreenToWorldPoint(pointerPos);
        worldPos.z = 0;
        this.ActiveController.OnPointerMoved(worldPos);
    }

    private void HandleCameraPan()
    {
        if (this.isMiddleMouseHeld || (this.isPrimaryHeld && MapEditorManager.Instance.CurrentTool == EditorTool.Drag))
        {
            Vector2 pointerPos = Mouse.current.position.ReadValue();
            this.cameraController.Pan(pointerPos);
        }
    }

    /// <summary>
    /// Gets a value indicating whether a tool shortcut may be applied.
    /// Typing and command combinations (e.g. Ctrl + Y) are left alone.
    /// </summary>
    /// <param name="ctx">The input context.</param>
    /// <returns>True when the shortcut should select the tool.</returns>
    private bool CanSelectTool(InputAction.CallbackContext ctx)
    {
        return ctx.performed && !this.IsTyping() && !this.command;
    }

    /// <summary>
    /// Gets a value indicating whether a command modifier (Ctrl) is currently held.
    /// Read straight from the keyboard so the shortcut does not depend on the
    /// separate Command action being evaluated.
    /// </summary>
    /// <returns>True while Ctrl is held.</returns>
    private bool IsCommandHeld()
    {
        return (Keyboard.current != null && Keyboard.current.ctrlKey.isPressed) ||
            this.command;
    }

    /// <summary>
    /// Switches to the selection tool and selects whatever was clicked in drag mode.
    /// </summary>
    /// <param name="pointerPos">The screen position of the click.</param>
    private void SelectAtPointer(Vector2 pointerPos)
    {
        Vector3 worldPos = this.cam.ScreenToWorldPoint(pointerPos);
        worldPos.z = 0;
        MapEditorManager.Instance.SelectAtPointer(worldPos);
    }

    /// <summary>
    /// Cancels all active input that controls the camera.
    /// </summary>
    private void CancelCameraInput()
    {
        this.isPrimaryHeld = false;
        this.isMiddleMouseHeld = false;

        this.cameraController.CancelMovement();
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

    private bool IsTyping()
    {
        GameObject selected =
            EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

        if (selected == null)
        {
            return false;
        }

        TMP_InputField tmpField = selected.GetComponent<TMP_InputField>();

        if (tmpField != null && tmpField.isFocused)
        {
            return true;
        }

        InputField legacyField = selected.GetComponent<InputField>();

        return legacyField != null && legacyField.isFocused;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            this.CancelCameraInput();
        }
    }
}