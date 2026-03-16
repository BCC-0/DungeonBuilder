using UnityEngine;

/// <summary>
/// Handles camera movements (zooming and panning) in builder mode.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 0.1f;
    [SerializeField] private float panSpeed = 0.01f;

    private Camera cam;

    /// <summary>
    /// Called by the Pan action in the InputAction map.
    /// Always pans the camera by the given delta.
    /// </summary>
    /// <param name="delta">The movement delta.</param>
    public void OnPan(Vector2 delta)
    {
        this.PanCamera(delta);
    }

    /// <summary>
    /// Called by the PointerDelta action in the InputAction map.
    /// Only pans if the current tool is Drag.
    /// </summary>
    /// <param name="delta">The movement delta.</param>
    public void OnPointerDelta(Vector2 delta)
    {
        if (MapEditorManager.Instance.CurrentTool == EditorTool.Drag)
        {
            this.PanCamera(delta);
        }
    }

    /// <summary>
    /// Zooms the camera by a given amount.
    /// </summary>
    /// <param name="amount">Positive to zoom in, negative to zoom out.</param>
    public void OnZoom(float amount)
    {
        this.cam.orthographicSize -= amount * this.zoomSpeed;
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize, 3f, 50f);
    }

    private void PanCamera(Vector2 delta)
    {
        this.transform.position -= new Vector3(delta.x, delta.y, 0) * this.panSpeed;
    }

    private void Awake()
    {
        this.cam = Camera.main;
    }
}