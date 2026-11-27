using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles camera movements (zooming and panning) in builder mode.
/// </summary>
public class CameraController : MonoBehaviour
{
    private const float MinCameraSize = 3f;
    private const float DefaultCameraSize = 5f;
    private const float MaxCameraSize = 25f;

    private float[] snapSliderValues = { 0f, 0.25f, 0.5f, 0.75f, 1f };

    [SerializeField]
    private float zoomSpeed = 0.1f;
    [SerializeField]
    private float moveSpeed = 10f;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Slider zoomSlider;
    [SerializeField]
    private SpriteFontText zoomText;
    [SerializeField]
    private SpriteFontText positionText;
    [SerializeField]
    private TMP_InputField positionInput;
    [SerializeField]
    private float inertiaDamping = 6f;
    [SerializeField]
    private float inertiaThreshold = 0.01f;
    [SerializeField]
    private float snapThreshold = 0.02f;

    [SerializeField]
    private float edgeThreshold = 25f;
    [SerializeField]
    private float edgePanSpeed = 15f;

    private Vector3 inertiaVelocity;
    private Vector2 cameraMoveDirection;
    private bool isZooming = false;
    private Vector2? lastPointerPosition = null;
    private bool isPanning = false;
    private Tween navigationTween;
    private int shownPositionX = int.MinValue;
    private int shownPositionY = int.MinValue;

    /// <summary>
    /// Gets a value indicating whether the camera is currently panning.
    /// </summary>
    public bool IsPanning { get => this.isPanning; private set => this.isPanning = value; }

    /// <summary>
    /// Gets the input field that shows and sets the camera position.
    /// </summary>
    private TMP_InputField PositionInput
    {
        get
        {
            if (this.positionInput == null &&
                this.positionText != null)
            {
                this.positionInput = this.positionText.GetComponent<TMP_InputField>();
            }

            return this.positionInput;
        }
    }

    /// <summary>
    /// Called by the Pan action in the InputAction map.
    /// Calculates delta based on currentPointerPosition and moves by it.
    /// </summary>
    /// <param name="currentPointerPosition">The movement delta.</param>
    /// <param name="useInertia">When we use inertia, after stopping panning the movement will smoothly stop instead of instantly.</param>
    public void Pan(Vector2 currentPointerPosition, bool useInertia = false)
    {
        this.isPanning = true;
        float camZ = this.cam.transform.position.z;

        if (this.lastPointerPosition.HasValue)
        {
            Vector2 delta = currentPointerPosition - this.lastPointerPosition.Value;

            float edgeX = this.GetEdgeFactor(
                currentPointerPosition.x,
                this.edgeThreshold,
                Screen.width - this.edgeThreshold
            );

            float edgeY = this.GetEdgeFactor(
                currentPointerPosition.y,
                this.edgeThreshold,
                Screen.height - this.edgeThreshold
            );

            Vector2 edgeDelta = new Vector2(edgeX, edgeY);

            // Apply smooth edge movement
            delta += edgeDelta * this.edgePanSpeed * Time.deltaTime * 100f;

            // Apply edge movement (scaled)
            delta += edgeDelta * this.edgePanSpeed;

            Vector3 worldStart = this.cam.ScreenToWorldPoint(new Vector3(0, 0, -camZ));
            Vector3 worldEnd = this.cam.ScreenToWorldPoint(new Vector3(delta.x, delta.y, -camZ));
            Vector3 worldDelta = worldEnd - worldStart;

            this.transform.position -= worldDelta;

            // Inertia
            if (useInertia)
            {
                this.inertiaVelocity = (-worldDelta) / Time.deltaTime;
            }
            else
            {
                this.inertiaVelocity = Vector3.zero;
            }
        }

        // Update last pointer position if pointer is on screen
        this.lastPointerPosition = currentPointerPosition;
    }

    /// <summary>
    /// Called by the Pan action in the InputAction map when it is cancelled.
    /// Removes lastPointerPosition.
    /// </summary>
    public void EndPan()
    {
        this.isPanning = false;
        this.lastPointerPosition = null;
    }

    /// <summary>
    /// Zooms the camera by a given amount.
    /// </summary>
    /// <param name="amount">Positive to zoom in, negative to zoom out.</param>
    /// <param name="pointerScreenPosition">The position of the pointer on screen.</param>
    public void OnZoom(float amount, Vector2 pointerScreenPosition)
    {
        Vector3 mouseBefore = this.cam.ScreenToWorldPoint(new Vector3(pointerScreenPosition.x, pointerScreenPosition.y, this.cam.nearClipPlane));

        float zoomAmount = amount * this.zoomSpeed * Time.deltaTime * 100f * (this.cam.orthographicSize / DefaultCameraSize);
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize - zoomAmount, MinCameraSize, MaxCameraSize);

        float newSlider = this.MapCameraSizeToSlider(this.cam.orthographicSize);
        this.isZooming = true;
        this.zoomSlider.value = this.MapCameraSizeToSlider(this.cam.orthographicSize);
        this.isZooming = false;
        this.UpdateZoomText();

        Vector3 mouseAfter = this.cam.ScreenToWorldPoint(new Vector3(pointerScreenPosition.x, pointerScreenPosition.y, this.cam.nearClipPlane));
        this.transform.position += mouseBefore - mouseAfter;
    }

    /// <summary>
    /// Moves the camera based on input vector.
    /// </summary>
    /// <param name="input">X = horizontal, Y = vertical, normalized.</param>
    public void OnMove(Vector2 input)
    {
        this.cameraMoveDirection = input;
    }

    /// <summary>
    /// Called when the zoom slider has changed value.
    /// </summary>
    /// <param name="sliderValue">The new value of the slider.</param>
    public void OnSliderChanged(float sliderValue)
    {
        if (this.isZooming)
        {
            return;
        }

        float snappedSliderValue = this.SnapSliderValue(sliderValue);
        this.zoomSlider.value = snappedSliderValue;
        this.cam.orthographicSize = this.MapSliderToCameraSize(snappedSliderValue);
        this.UpdateZoomText();
    }

    /// <summary>
    /// Immediately stops all camera movement and resets input state.
    /// </summary>
    public void CancelMovement()
    {
        this.cameraMoveDirection = Vector2.zero;
        this.inertiaVelocity = Vector3.zero;
        this.lastPointerPosition = null;
        this.isPanning = false;
    }


    /// <summary>
    /// Smoothly moves the camera to the given world position.
    /// Keeps the camera's current Z position.
    /// </summary>
    /// <param name="worldPosition">The world position to move to.</param>
    /// <param name="duration">The duration of the movement.</param>
    public void MoveTo(Vector3 worldPosition, float duration = 0.5f)
    {
        this.CancelMovement();

        if (this.navigationTween != null &&
            this.navigationTween.IsActive())
        {
            this.navigationTween.Kill();
        }

        Vector3 targetPosition = new Vector3(
            worldPosition.x,
            worldPosition.y,
            this.transform.position.z);

        this.navigationTween = this.transform
            .DOMove(targetPosition, duration)
            .SetEase(Ease.InOutCubic);
    }

    private void FixedUpdate()
    {
        Vector3 movement = this.cameraMoveDirection * this.moveSpeed * Time.deltaTime;

        this.transform.position += movement;
    }

    private void Start()
    {
        this.zoomSlider.value = this.MapCameraSizeToSlider(this.cam.orthographicSize);
        this.UpdateZoomText();
        this.RegisterPositionInput();
    }

    private float MapSliderToCameraSize(float sliderValue)
    {
        if (sliderValue < 0.5f)
        {
            float t = sliderValue / 0.5f;
            return Mathf.Lerp(MinCameraSize, DefaultCameraSize, t * t);
        }
        else
        {
            float t = (sliderValue - 0.5f) / 0.5f;
            return Mathf.Lerp(DefaultCameraSize, MaxCameraSize, t * t * t);
        }
    }

    private float MapCameraSizeToSlider(float cameraSize)
    {
        if (cameraSize < DefaultCameraSize)
        {
            float t = (cameraSize - MinCameraSize) / (DefaultCameraSize - MinCameraSize);
            return Mathf.Sqrt(t) * 0.5f;
        }
        else
        {
            float t = (cameraSize - DefaultCameraSize) / (MaxCameraSize - DefaultCameraSize);
            return (Mathf.Pow(t, 1f / 3f) * 0.5f) + 0.5f;
        }
    }

    private void Update()
    {
        this.ApplyInertia();
        this.UpdatePositionText();
    }

    /// <summary>
    /// Shows where the camera is in the world as whole numbers, using the same
    /// numbers the editor uses for entities and tiles.
    /// </summary>
    private void UpdatePositionText()
    {
        TMP_InputField input = this.PositionInput;

        if (this.positionText == null &&
            input == null)
        {
            return;
        }

        // While the field is being edited the player's text must stay as it is.
        if (input != null &&
            input.isFocused)
        {
            return;
        }

        Vector3 position = this.transform.position;
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);

        // Rebuilding the sprite text every frame is wasteful, so only do it
        // when the whole numbers actually change.
        if (x == this.shownPositionX &&
            y == this.shownPositionY)
        {
            return;
        }

        this.shownPositionX = x;
        this.shownPositionY = y;

        string text = $"{x},{y}";

        if (input != null)
        {
            input.SetTextWithoutNotify(text);
        }

        if (this.positionText != null)
        {
            this.positionText.SetText(text);
        }
    }

    /// <summary>
    /// Starts listening for a typed camera position.
    /// </summary>
    private void RegisterPositionInput()
    {
        TMP_InputField input = this.PositionInput;

        if (input == null)
        {
            return;
        }

        input.onEndEdit.RemoveListener(this.OnPositionInputEndEdit);
        input.onEndEdit.AddListener(this.OnPositionInputEndEdit);
    }

    /// <summary>
    /// Moves the camera to the typed position. Warns when the text does not
    /// contain whole numbers.
    /// </summary>
    /// <param name="value">The text of the input field.</param>
    private void OnPositionInputEndEdit(string value)
    {
        if (!this.TryParsePosition(value, out Vector2Int gridPosition))
        {
            WarningManager.Warn("Camera position must be two whole numbers, like 3,4.");

            // Show the real camera position again instead of the bad text.
            this.shownPositionX = int.MinValue;
            this.shownPositionY = int.MinValue;
            return;
        }

        this.CancelMovement();

        if (this.navigationTween != null &&
            this.navigationTween.IsActive())
        {
            this.navigationTween.Kill();
        }

        this.transform.position = new Vector3(
            gridPosition.x,
            gridPosition.y,
            this.transform.position.z);

        // Rewrite the field in the standard format, e.g. after "3, 4".
        this.shownPositionX = int.MinValue;
        this.shownPositionY = int.MinValue;
    }

    /// <summary>
    /// Reads a position like "3,4" or "(3,4)". Both values must be whole
    /// numbers.
    /// </summary>
    /// <param name="value">The text to read.</param>
    /// <param name="gridPosition">The position when the text is valid.</param>
    /// <returns>True when the text contains a whole number for X and Y.</returns>
    private bool TryParsePosition(string value, out Vector2Int gridPosition)
    {
        gridPosition = Vector2Int.zero;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string text = value.Trim();

        if (text.StartsWith("(") &&
            text.EndsWith(")"))
        {
            text = text.Substring(1, text.Length - 2);
        }

        string[] parts = text.Split(',');

        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0].Trim(), out int x) ||
            !int.TryParse(parts[1].Trim(), out int y))
        {
            return false;
        }

        gridPosition = new Vector2Int(x, y);

        return true;
    }

    private void ApplyInertia()
    {
        if (this.inertiaVelocity.magnitude < this.inertiaThreshold)
        {
            this.inertiaVelocity = Vector3.zero;
            return;
        }

        this.transform.position += this.inertiaVelocity * Time.deltaTime;

        this.inertiaVelocity = Vector3.Lerp(
            this.inertiaVelocity,
            Vector3.zero,
            this.inertiaDamping * Time.deltaTime);
    }

    private float SnapSliderValue(float sliderValue)
    {
        float closest = sliderValue;
        float minDistance = float.MaxValue;

        foreach (float snapValue in this.snapSliderValues)
        {
            float distance = Mathf.Abs(sliderValue - snapValue);
            float scaledThreshold = this.snapThreshold * 2;
            if (distance < scaledThreshold && distance < minDistance)
            {
                closest = snapValue;
                minDistance = distance;
            }
        }

        return closest;
    }

    private float GetEdgeFactor(float pos, float min, float max)
    {
        if (pos < min)
        {
            return -(1f - (pos / min));
        }

        if (pos > max)
        {
            return (pos - max) / min;
        }

        return 0f;
    }

    private void UpdateZoomText()
    {
        float magnification = DefaultCameraSize / this.cam.orthographicSize;
        this.zoomText.SetText($"{magnification:F2}x");
    }
}