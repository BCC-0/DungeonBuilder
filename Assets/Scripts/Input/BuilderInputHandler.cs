using System.Collections;
using System.Linq;
using DG.Tweening;
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
    private RectTransform ripplePrefab;
    [SerializeField]
    private Canvas uiCanvas;

    private bool isMiddleMouseHeld;
    private bool isPrimaryHeld;

    private bool isTouching;
    private float touchHoldTimer;
    private float lastPinchDistance;

    private bool isTouchPanningOverride;
    private Vector2 touchStartPosition;
    private Coroutine rippleCoroutine;
    private Sequence currentRippleSequence;
    private RectTransform currentRipple;

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
        if (isPointerOverUI)
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

        if (ctx.canceled)
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
            this.HandleCameraPan(Vector2.zero, false);
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

        Vector2 pointerPos = Mouse.current.position.ReadValue();

        if (this.ShouldPan())
        {
            this.HandleCameraPan(pointerPos, true);
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

    /// <summary>
    /// Called when the player tries to select a slot (with entities or tiles).
    /// Finds the difference in pointer position, is used to move the camera.
    /// </summary>
    /// <param name="index">The index of the slot.</param>
    private void SelectSlot(int index)
    {
        // TODO: Slot selection with both entities and tiles.
    }

    private bool ShouldPan()
    {
        if (isPointerOverUI && !this.cameraController.IsPanning)
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

    private void HandleCameraPan(Vector2 pointerPosition, bool isDragging)
    {
        if (isDragging)
        {
            // Pass current pointer position to CameraController
            this.cameraController.Pan(pointerPosition);
        }
        else
        {
            // Stop pan when drag ends
            this.cameraController.EndPan();
        }
    }

    private void HandleNoTouch()
    {
        // Reset all needed variables and stop from dragging if it was.
        this.isTouching = false;
        this.touchHoldTimer = 0;
        this.lastPinchDistance = 0;
        this.isTouchPanningOverride = false;
        this.HandleCameraPan(Vector2.zero, false);

        // Cancel ripple if any
        if (this.rippleCoroutine != null)
        {
            this.StopCoroutine(this.rippleCoroutine);
            this.rippleCoroutine = null;
        }
    }

    private void HandleOneTouch(UnityEngine.InputSystem.Controls.TouchControl[] activeTouches)
    {
        var touch = activeTouches[0];
        Vector2 screenPos = touch.position.ReadValue();
        Vector3 worldPos = this.cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;

        if (!this.isTouching)
        {
            this.isTouching = true;
            this.touchHoldTimer = 0;
            this.isTouchPanningOverride = false;
            this.touchStartPosition = screenPos;

            if (!isPointerOverUI)
            {
                this.tileEditor.OnPointerMoved(worldPos);
                this.entityEditor.OnPointerMoved(worldPos);

                // Start primary down if not drag
                if (this.mapEditorManager.CurrentTool != EditorTool.Drag)
                {
                    if (this.mapEditorManager.CurrentLayer == EditLayer.Background)
                    {
                        this.tileEditor.OnPrimaryDown();
                    }
                    else
                    {
                        this.entityEditor.OnPrimaryDown();
                    }
                }
            }
        }

        this.HandleRipple(screenPos);

        // Handle drag / pointer movement
        if (!isPointerOverUI || this.cameraController.IsPanning)
        {
            if (this.mapEditorManager.CurrentTool == EditorTool.Drag || this.isTouchPanningOverride)
            {
                Vector2 delta = touch.delta.ReadValue();
                this.HandleCameraPan(screenPos, true);
            }
            else
            {
                this.tileEditor.OnPointerMoved(worldPos);
                this.entityEditor.OnPointerMoved(worldPos);
            }
        }
    }

    private void HandleMultiTouch(UnityEngine.InputSystem.Controls.TouchControl[] activeTouches)
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

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
        {
            return;
        }

        // Find currently active touches.
        var touches = Touchscreen.current.touches;
        UnityEngine.InputSystem.Controls.TouchControl[] activeTouches = touches.Where(t => t.isInProgress).ToArray();

        if (activeTouches.Length == 0)
        {
            this.HandleNoTouch();
            return;
        }

        // Single touch behavior
        if (activeTouches.Length == 1)
        {
            this.HandleOneTouch(activeTouches);
        }

        // Pinch zoom
        if (activeTouches.Length >= 2)
        {
            this.HandleMultiTouch(activeTouches);
        }
        else
        {
            // If we don't have multiple touches we dont have a pinch distant.
            this.lastPinchDistance = 0;
        }
    }

    private void HandleRipple(Vector2 screenPos)
    {
        this.touchHoldTimer += Time.deltaTime;

        float moveDelta = Vector2.Distance(screenPos, this.touchStartPosition);
        bool fingerIsStill = moveDelta < Screen.dpi * 0.05f;

        // Cancel ripple if finger moves while it’s animating
        if (this.rippleCoroutine != null && !fingerIsStill)
        {
            if (this.currentRippleSequence != null)
            {
                this.currentRippleSequence.Kill(false); // stop without calling OnComplete
                this.currentRippleSequence = null;
            }

            this.StartCoroutine(this.FadeOutRippleImmediate());
            this.rippleCoroutine = null;
        }

        // Show ripple if hold threshold reached and finger is still
        if (!this.isTouchPanningOverride &&
            this.mapEditorManager.CurrentTool != EditorTool.Drag &&
            fingerIsStill &&
            this.touchHoldTimer >= this.holdThreshold &&
            this.rippleCoroutine == null &&
            !isPointerOverUI)
        {
            this.rippleCoroutine = this.StartCoroutine(this.PlayRipple(screenPos, () =>
            {
                // After ripple ends, allow drag
                this.isTouchPanningOverride = true;

                if (this.mapEditorManager.CurrentLayer == EditLayer.Background)
                {
                    this.tileEditor.OnPrimaryUp();
                }
                else
                {
                    this.entityEditor.OnPrimaryUp();
                }

                this.rippleCoroutine = null;
            }));
        }
    }

    private IEnumerator PlayRipple(Vector2 screenPosition, System.Action onComplete)
    {
        RectTransform ripple = Instantiate(this.ripplePrefab, this.uiCanvas.transform);
        this.currentRipple = ripple;

        CanvasGroup cg = ripple.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = ripple.gameObject.AddComponent<CanvasGroup>();
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            this.uiCanvas.transform as RectTransform,
            screenPosition,
            this.uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : this.cam,
            out Vector2 localPoint);

        ripple.localPosition = localPoint;

        ripple.localScale = Vector3.one * 2f;
        cg.alpha = 0.6f;

        float duration = 0.75f;
        float dragStartTime = 0.25f;

        this.currentRippleSequence = DOTween.Sequence();
        this.currentRippleSequence.Append(ripple.DOScale(1f, duration).SetEase(Ease.OutCubic));
        this.currentRippleSequence.Join(cg.DOFade(0f, duration));

        bool dragActivated = false;

        this.currentRippleSequence.OnUpdate(() =>
        {
            if (!dragActivated && this.currentRippleSequence.Elapsed() >= dragStartTime)
            {
                dragActivated = true;
                onComplete?.Invoke();
                Handheld.Vibrate();
            }
        });

        this.currentRippleSequence.OnComplete(() =>
        {
            Destroy(ripple.gameObject);
            this.currentRippleSequence = null;
        });

        yield return this.currentRippleSequence.WaitForCompletion();
    }

    private IEnumerator FadeOutRippleImmediate()
    {
        RectTransform ripple = this.currentRipple;
        CanvasGroup cg = ripple.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = ripple.gameObject.AddComponent<CanvasGroup>();
        }

        float duration = 0.4f;
        float startAlpha = cg.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
            yield return null;
        }

        Destroy(ripple.gameObject);
    }
}
