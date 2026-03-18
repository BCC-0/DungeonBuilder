using UnityEngine;

/// <summary>
/// Handles camera movements (zooming and panning) in builder mode.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float zoomSpeed = 0.1f;
    [SerializeField]
    private float panSpeed = 0.003f;
    [SerializeField]
    private Camera cam;

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
    public void OnZoom(float amount)
    {
        Vector3 mouseBefore = this.cam.ScreenToWorldPoint(Input.mousePosition);

        this.cam.orthographicSize -= amount * this.zoomSpeed;
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize, 3f, 50f);

        Vector3 mouseAfter = this.cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 diff = mouseBefore - mouseAfter;

        this.transform.position += diff;
    }
}