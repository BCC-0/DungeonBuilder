using UnityEngine;

/// <summary>
/// The camera controller controls camera movements (zooming, panning) in the builder mode.
/// </summary>
public class CameraController : MonoBehaviour
{
    private float zoomSpeed = 0.1f;
    private float panSpeed = 0.01f;
    private Camera cam;

    /// <summary>
    /// Gets or sets the zoom speed of this camera.
    /// </summary>
    public float ZoomSpeed
    {
        get { return this.zoomSpeed; }
        set { this.zoomSpeed = value; }
    }

    /// <summary>
    /// Gets or sets the panning speed of this camera.
    /// </summary>
    public float PanSpeed
    {
        get { return this.panSpeed; }
        set { this.panSpeed = value; }
    }


    private void Awake()
    {
        this.cam = this.GetComponent<Camera>();
    }

    private void OnEnable()
    {
        EditorEventBus.OnZoom += this.Zoom;
        EditorEventBus.OnPan += this.Pan;
    }

    private void OnDisable()
    {
        EditorEventBus.OnZoom -= this.Zoom;
        EditorEventBus.OnPan -= this.Pan;
    }

    private void Zoom(float amount)
    {
        this.cam.orthographicSize -= amount * this.ZoomSpeed;
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize, 3f, 50f);
    }

    private void Pan(Vector2 delta)
    {
        this.transform.position -= new Vector3(delta.x, delta.y, 0) * this.PanSpeed;
    }
}