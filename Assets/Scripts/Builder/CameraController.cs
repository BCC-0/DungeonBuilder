using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

/// <summary>
/// Handles camera movements (zooming and panning) in builder mode.
/// </summary>
public class CameraController : MonoBehaviour
{
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

    private Vector2 cameraMoveDirection;

    private const float minCameraSize = 3f;
    private const float defaultCameraSize = 5f;
    private const float maxCameraSize = 25f;

    /// <summary>
    /// Called by the Pan action in the InputAction map.
    /// Always pans the camera by the given delta.
    /// </summary>
    /// <param name="delta">The movement delta.</param>
    public void Pan(Vector2 delta)
    {
        float scale = this.cam.orthographicSize * this.panSpeed;
        Vector3 move = new Vector3(delta.x, delta.y, 0) * scale;

        this.transform.position -= move;
    }

    /// <summary>
    /// Zooms the camera by a given amount.
    /// </summary>
    /// <param name="amount">Positive to zoom in, negative to zoom out.</param>
    /// <param name="pointerScreenPosition">The position of the pointer on screen.</param>
    public void OnZoom(float amount, Vector2 pointerScreenPosition)
    {
        Vector3 mouseBefore = this.cam.ScreenToWorldPoint(new Vector3(pointerScreenPosition.x, pointerScreenPosition.y, this.cam.nearClipPlane));

        this.cam.orthographicSize -= amount * this.zoomSpeed;
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize, minCameraSize, maxCameraSize);

        // Also update UI to keep it consistent.
        this.zoomSlider.value = this.MapCameraSizeToSlider(this.cam.orthographicSize);

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
    /// Sets camera size directly from slider value.
    /// </summary>
    /// <param name="sliderValue">Slider value</param>
    public void SetCameraSizeFromSlider(float sliderValue)
    {
        this.cam.orthographicSize = this.MapSliderToCameraSize(sliderValue);
    }

    private void FixedUpdate()
    {
        Vector3 movement = this.cameraMoveDirection * this.moveSpeed * Time.deltaTime;

        this.transform.position += movement;
    }

    private void Start()
    {
        // Set initial slider value to camera size
        this.zoomSlider.value = this.MapCameraSizeToSlider(this.cam.orthographicSize);
    }

    private float MapSliderToCameraSize(float sliderValue)
    {
        if (sliderValue < 0.5f)
        {
            float t = sliderValue / 0.5f;
            return Mathf.Lerp(minCameraSize, defaultCameraSize, t * t); // quadratic for slower growth
        }
        else
        {
            float t = (sliderValue - 0.5f) / 0.5f;
            return Mathf.Lerp(defaultCameraSize, maxCameraSize, t * t * t); // cubic for faster growth
        }
    }

    private float MapCameraSizeToSlider(float cameraSize)
    {
        if (cameraSize < defaultCameraSize)
        {
            float t = (cameraSize - minCameraSize) / (defaultCameraSize - minCameraSize);
            return Mathf.Sqrt(t) * 0.5f; // inverse of quadratic
        }
        else
        {
            float t = (cameraSize - defaultCameraSize) / (maxCameraSize - defaultCameraSize);
            return (Mathf.Pow(t, 1f / 3f) * 0.5f) + 0.5f; // inverse of cubic
        }
    }
}