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
    private float panSpeed = 0.002f;
    [SerializeField]
    private float moveSpeed = 10f;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Slider zoomSlider;
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

    /// <summary>
    /// Gets a value indicating whether the camera is currently panning.
    /// </summary>
    public bool IsPanning { get => this.isPanning; private set => this.isPanning = value; }

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
    }

    private void FixedUpdate()
    {
        Vector3 movement = this.cameraMoveDirection * this.moveSpeed * Time.deltaTime;

        this.transform.position += movement;
    }

    private void Start()
    {
        this.zoomSlider.value = this.MapCameraSizeToSlider(this.cam.orthographicSize);
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
}