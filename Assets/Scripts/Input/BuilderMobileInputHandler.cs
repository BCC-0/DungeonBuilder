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

        var touches = Touchscreen.current.touches;
        var activeTouches = touches.Where(t => t.isInProgress).ToArray();

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
        if (this.isTouching && !this.isTouchPanningOverride && this.touchHoldTimer < this.holdThreshold)
        {
            this.ActiveController.OnPrimaryDown();
            this.ActiveController.OnPrimaryUp();
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

        if (this.rippleCoroutine != null)
        {
            this.StopCoroutine(this.rippleCoroutine);
            this.rippleCoroutine = null;
        }

        // Clean up touches over UI
        foreach (var touch in Touchscreen.current.touches)
        {
            int id = touch.touchId.ReadValue();
            if (!touch.isInProgress && this.touchesOverUI.ContainsKey(id))
            {
                this.touchesOverUI.Remove(id);
            }
        }
    }

    private void HandleOneTouch(UnityEngine.InputSystem.Controls.TouchControl touch)
    {
        int id = touch.touchId.ReadValue();
        Vector2 screenPos = touch.position.ReadValue();
        Vector3 worldPos = this.cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;

        if (!this.touchesOverUI.ContainsKey(id))
        {
            this.touchesOverUI[id] = this.IsPointerOverUI(screenPos);
        }

        if (this.touchesOverUI[id])
        {
            return;
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
            if (MapEditorManager.Instance.CurrentTool != EditorTool.Drag)
            {
                this.ActiveController.OnPrimaryDown();
            }
        }

        if (MapEditorManager.Instance.CurrentTool == EditorTool.Drag || this.isTouchPanningOverride)
        {
            this.cameraController.Pan(screenPos);
        }
        else
        {
            this.ActiveController.OnPointerMoved(worldPos);
        }
    }

    private void HandleMultiTouch(UnityEngine.InputSystem.Controls.TouchControl t1, UnityEngine.InputSystem.Controls.TouchControl t2)
    {
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

            this.ActiveController.OnPrimaryUp();

            this.rippleCoroutine = this.StartCoroutine(this.PlayRipple(screenPos, () =>
            {
                // no longer needed for logic, just keep for feedback if you want
                Handheld.Vibrate();
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
                dragActivated = true;
                onComplete?.Invoke();
                Handheld.Vibrate();
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