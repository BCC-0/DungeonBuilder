using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

/// <summary>
/// Handles camera movements (zooming and panning) in builder mode.
/// </summary>
public class CameraController : MonoBehaviour
{
    private const float MinCameraSize = 3f;
    private const float DefaultCameraSize = 5f;
    private const float MaxCameraSize = 25f;

    private float[] snapSliderValues = { 0f, 0.25f, 0.5f, 0.75f, 1f };

    private bool isScrolling = false;

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
    private float zoomThreshold = 0.01f;
    [SerializeField]
    private float snapThreshold = 0.02f;
    [SerializeField]
    private float scrollSnapDelay = 0.2f;

    private Vector3 inertiaVelocity;
    private bool isPanning;

    private Vector2 cameraMoveDirection;

    /// <summary>
    /// Called by the Pan action in the InputAction map.
    /// Always pans the camera by the given delta.
    /// </summary>
    /// <param name="delta">The movement delta.</param>
    /// <param name="useInertia">When we use inertia, after stopping panning the movement will smoothly stop instead of instantly.</param>
    public void Pan(Vector2 delta, bool useInertia = false)
    {
        float scale = this.cam.orthographicSize * this.panSpeed;
        Vector3 move = new Vector3(delta.x, delta.y, 0) * scale;

        this.transform.position -= move;

        if (useInertia)
        {
            this.inertiaVelocity = (-move) / Time.deltaTime;
            this.isPanning = true;
        }
        else
        {
            this.inertiaVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Zooms the camera by a given amount.
    /// </summary>
    /// <param name="amount">Positive to zoom in, negative to zoom out.</param>
    /// <param name="pointerScreenPosition">The position of the pointer on screen.</param>
    public void OnZoom(float amount, Vector2 pointerScreenPosition)
    {
        if (Mathf.Abs(amount) < this.zoomThreshold)
        {
            Debug.Log(amount);

            // Stopped zooming, can now snap.
            this.isScrolling = false;
            this.OnSliderChanged(this.zoomSlider.value);
            return;
        }

        this.isScrolling = true;
        Vector3 mouseBefore = this.cam.ScreenToWorldPoint(new Vector3(pointerScreenPosition.x, pointerScreenPosition.y, this.cam.nearClipPlane));

        float zoomAmount = amount * this.zoomSpeed * Time.deltaTime * 100f;
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize - zoomAmount, MinCameraSize, MaxCameraSize);
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize, MinCameraSize, MaxCameraSize);

        float newSlider = this.MapCameraSizeToSlider(this.cam.orthographicSize);
        this.zoomSlider.value = newSlider;

        Vector3 mouseAfter = this.cam.ScreenToWorldPoint(new Vector3(pointerScreenPosition.x, pointerScreenPosition.y, this.cam.nearClipPlane));
        Vector3 diff = mouseBefore - mouseAfter;
        this.transform.position += diff;
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
        float snappedSliderValue = this.isScrolling ? sliderValue : this.SnapSliderValue(sliderValue);
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
}