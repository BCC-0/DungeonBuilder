using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all mobile touch input for the map editor.
/// </summary>
public class BuilderMobileInputHandler : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private float holdThreshold = 0.2f;
    [SerializeField]
    private float mobileZoomSpeed = 0.2f;
    [SerializeField]
    private float doubleTapWindow = 0.3f;
    [SerializeField]
    private RectTransform ripplePrefab;
    [SerializeField]
    private Canvas uiCanvas;

    private bool isTouching;
    private float touchHoldTimer;
    private float lastPinchDistance;
    private bool isTouchPanningOverride;
    private Vector2 touchStartPosition;
    private Coroutine rippleCoroutine;
    private Sequence currentRippleSequence;
    private Dictionary<int, bool> touchesOverUI = new ();
    private bool isSelectionStarted;
    private bool isResizingSelection;
    private Vector2 lastTouchScreenPos;
    private float lastTapTime = -999f;
    private Vector2 lastTapPosition;
    private List<SaveableEntity> entitiesBeforeTap = new ();
    private List<Vector2Int> tilesBeforeTap = new ();

    private EditorControllerBase ActiveController => MapEditorManager.Instance.ActiveController;

    private void Update()
    {
        this.HandleTouch();
    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
        {
            return;
        }

        UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.Controls.TouchControl> touches = Touchscreen.current.touches;
        UnityEngine.InputSystem.Controls.TouchControl[] activeTouches = touches.Where(t => t.isInProgress).ToArray();

        if (activeTouches.Length == 0)
        {
            this.HandleNoTouch();
            return;
        }

        if (activeTouches.Length == 1)
        {
            this.HandleOneTouch(activeTouches[0]);
        }

        if (activeTouches.Length >= 2)
        {
            this.HandleMultiTouch(activeTouches[0], activeTouches[1]);
        }
        else
        {
            this.lastPinchDistance = 0;
        }
    }

    private void HandleNoTouch()
    {
        if (this.isResizingSelection)
        {
            this.isResizingSelection = false;
            this.isTouching = false;
            this.touchHoldTimer = 0;
            this.isTouchPanningOverride = false;

            MapEditorManager.Instance.EndSelectionResize();
        }

        if (this.isTouching && this.IsTap())
        {
            if (this.IsDoubleTap(this.lastTouchScreenPos))
            {
                this.lastTapTime = -999f;

                this.RestoreSelectionBeforeTap();
                this.ToggleSelectionMenu(this.lastTouchScreenPos);
            }
            else
            {
                this.lastTapTime = Time.unscaledTime;
                this.lastTapPosition = this.lastTouchScreenPos;

                this.CaptureSelectionBeforeTap();

                MapEditorManager.Instance.HideSelectionMenu();

                this.ActiveController.OnPrimaryDown();
                this.ActiveController.OnPrimaryUp();

                EditorTool tool = MapEditorManager.Instance.CurrentTool;

                if (tool == EditorTool.Drag)
                {
                    this.SelectAtPointer(this.lastTouchScreenPos);
                }
                else if (tool == EditorTool.Brush &&
                         MapEditorManager.Instance.CurrentLayer == EditLayer.Foreground)
                {
                    this.SelectEntityAtPointer(this.lastTouchScreenPos);
                }
            }
        }
        else if (this.isTouching)
        {
            this.ActiveController.OnPrimaryUp();
        }

        this.isTouching = false;
        this.touchHoldTimer = 0;
        this.lastPinchDistance = 0;
        this.isTouchPanningOverride = false;
        this.cameraController.EndPan();
        this.isSelectionStarted = false;

        if (this.rippleCoroutine != null)
        {
            this.StopCoroutine(this.rippleCoroutine);
            this.rippleCoroutine = null;
        }

        // Clean up touches over UI
        foreach (UnityEngine.InputSystem.Controls.TouchControl touch in Touchscreen.current.touches)
        {
            int id = touch.touchId.ReadValue();
            if (!touch.isInProgress && this.touchesOverUI.ContainsKey(id))
            {
                this.touchesOverUI.Remove(id);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the released touch was a tap rather than a pan or hold.
    /// </summary>
    /// <returns>True when the touch was short and stayed near where it started.</returns>
    private bool IsTap()
    {
        return !this.isTouchPanningOverride &&
            this.touchHoldTimer < this.holdThreshold &&
            Vector2.Distance(this.lastTouchScreenPos, this.touchStartPosition) <=
                Mathf.Max(5f, Screen.dpi * 0.05f);
    }

    /// <summary>
    /// Gets a value indicating whether the released tap completes a double tap.
    /// </summary>
    /// <param name="screenPos">The screen position that was tapped.</param>
    /// <returns>True when the tap is the second one of a double tap.</returns>
    private bool IsDoubleTap(Vector2 screenPos)
    {
        return Time.unscaledTime - this.lastTapTime <= this.doubleTapWindow &&
            Vector2.Distance(screenPos, this.lastTapPosition) <=
                Mathf.Max(20f, Screen.dpi * 0.1f);
    }

    /// <summary>
    /// Opens or closes the selection menu at a screen position. This is the touch
    /// equivalent of a right click.
    /// </summary>
    /// <param name="screenPos">The screen position that was double tapped.</param>
    private void ToggleSelectionMenu(Vector2 screenPos)
    {
        Vector3 worldPos = this.cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;

        MapEditorManager.Instance.ToggleSelectionMenu(worldPos);
    }

    /// <summary>
    /// Selects whatever is under a screen position without leaving the current tool.
    /// Matches a click in the desktop drag tool.
    /// </summary>
    /// <param name="screenPos">The screen position that was tapped.</param>
    private void SelectAtPointer(Vector2 screenPos)
    {
        Vector3 worldPos = this.cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;
        MapEditorManager.Instance.SelectAtPointer(worldPos);
    }

    /// <summary>
    /// Selects the entity under a tapped screen position without leaving the tool,
    /// for brush taps in the foreground layer.
    /// </summary>
    /// <param name="screenPos">The screen position that was tapped.</param>
    private void SelectEntityAtPointer(Vector2 screenPos)
    {
        Vector3 worldPos = this.cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;
        MapEditorManager.Instance.SelectEntityAtPointer(worldPos);
    }

    /// <summary>
    /// Remembers the selection before a tap changes it, so a double tap can put it
    /// back before opening the selection menu.
    /// </summary>
    private void CaptureSelectionBeforeTap()
    {
        this.entitiesBeforeTap =
            new List<SaveableEntity>(
                MapEditorManager.Instance.SelectedEntities);

        this.tilesBeforeTap =
            new List<Vector2Int>(
                MapEditorManager.Instance.SelectedTiles);
    }

    /// <summary>
    /// Puts the selection back after the first tap of a double tap cleared it, so the
    /// menu the double tap opens still belongs to the items that were selected.
    /// </summary>
    private void RestoreSelectionBeforeTap()
    {
        MapEditorManager manager = MapEditorManager.Instance;

        bool hadSelection =
            this.entitiesBeforeTap.Count > 0 ||
            this.tilesBeforeTap.Count > 0;

        bool selectionCleared =
            (manager.SelectedEntities == null ||
             manager.SelectedEntities.Count == 0) &&
            (manager.SelectedTiles == null ||
             manager.SelectedTiles.Count == 0);

        if (!hadSelection || !selectionCleared)
        {
            return;
        }

        manager.SelectedEntities = this.entitiesBeforeTap;
        manager.SelectedTiles = this.tilesBeforeTap;

        manager.RefreshSelectionVisuals();
    }

    private void HandleOneTouch(UnityEngine.InputSystem.Controls.TouchControl touch)
    {
        int id = touch.touchId.ReadValue();
        Vector2 screenPos = touch.position.ReadValue();
        this.lastTouchScreenPos = screenPos;
        Vector3 worldPos = this.cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;

        if (this.isResizingSelection)
        {
            this.ActiveController.OnPointerMoved(worldPos);
            return;
        }

        if (!this.touchesOverUI.ContainsKey(id))
        {
            this.touchesOverUI[id] = this.IsPointerOverUI(screenPos) ||
                MapEditorManager.Instance.IsPointerOverSelectionButtons(screenPos);
        }

        if (this.touchesOverUI[id])
        {
            return;
        }

        if (!this.isTouching &&
            MapEditorManager.Instance.BeginSelectionResize(screenPos))
        {
            this.isResizingSelection = true;
            this.isTouching = true;
            this.touchStartPosition = screenPos;

            this.ActiveController.OnPointerMoved(worldPos);
            return;
        }

        // Update world pos first, then call the primary down method.
        if (MapEditorManager.Instance.CurrentTool == EditorTool.Drag || this.isTouchPanningOverride)
        {
            this.cameraController.Pan(screenPos);
        }
        else
        {
            this.ActiveController.OnPointerMoved(worldPos);
        }

        if (!this.isTouching)
        {
            this.isTouching = true;
            this.touchHoldTimer = 0;
            this.isTouchPanningOverride = false;
            this.touchStartPosition = screenPos;

            this.ActiveController.OnPointerMoved(worldPos);
        }

        this.touchHoldTimer += Time.deltaTime;

        float moveDelta = Vector2.Distance(screenPos, this.touchStartPosition);
        bool fingerIsStill = moveDelta < Screen.dpi * 0.05f;

        this.HandleRipple(screenPos, id, fingerIsStill);

        if (!fingerIsStill && !this.isTouchPanningOverride)
        {
            if (MapEditorManager.Instance.CurrentTool != EditorTool.Drag &&
                !(MapEditorManager.Instance.CurrentTool == EditorTool.Selection && this.isSelectionStarted))
            {
                this.ActiveController.OnPrimaryDown();

                if (MapEditorManager.Instance.CurrentTool == EditorTool.Selection)
                {
                    this.isSelectionStarted = true;
                }
            }
        }
    }

    private void HandleMultiTouch(UnityEngine.InputSystem.Controls.TouchControl t1, UnityEngine.InputSystem.Controls.TouchControl t2)
    {
        if (this.isResizingSelection)
        {
            return;
        }

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

    private void HandleRipple(Vector2 screenPos, int id, bool fingerIsStill)
    {
        if (!this.isTouchPanningOverride &&
            MapEditorManager.Instance.CurrentTool != EditorTool.Drag &&
            fingerIsStill &&
            this.touchHoldTimer >= this.holdThreshold &&
            this.rippleCoroutine == null &&
            !this.touchesOverUI[id])
        {
            this.isTouchPanningOverride = true;

            // this.ActiveController.OnPrimaryUp();
            // this.isSelectionStarted = false;

            this.rippleCoroutine = this.StartCoroutine(this.PlayRipple(screenPos, () =>
            {
                // no longer needed for logic, just keep for feedback if you want
                // Handheld.Vibrate();
                this.rippleCoroutine = null;
            }));
        }
    }

    private IEnumerator PlayRipple(Vector2 screenPosition, System.Action onComplete)
    {
        RectTransform ripple = Instantiate(this.ripplePrefab, this.uiCanvas.transform);
        CanvasGroup cg = ripple.GetComponent<CanvasGroup>() ?? ripple.gameObject.AddComponent<CanvasGroup>();

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
                this.isSelectionStarted = false;
                dragActivated = true;
                onComplete?.Invoke();
            }
        });

        this.currentRippleSequence.OnComplete(() => Destroy(ripple.gameObject));
        yield return this.currentRippleSequence.WaitForCompletion();
    }

    private bool IsPointerOverUI(Vector2 screenPos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos,
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}