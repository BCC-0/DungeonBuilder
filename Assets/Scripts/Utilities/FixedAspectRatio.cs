using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds black bars to the side if the screen exceeds the allowed aspect ratio.
/// This prevents players with weird aspect ratios to see parts of levels they are not allowed to see yet.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FixedAspectRatio : MonoBehaviour
{
    [SerializeField]
    private float targetAspect = 16f / 9f;
    [SerializeField]
    private float baseOrthoSize = 5f;

    [SerializeField]
    private CanvasScaler canvasScaler;
    [SerializeField]
    private Canvas canvas;

    private Camera cam;

    private Rect viewportRect;

    /// <summary>
    /// Gets the viewport rect of the camera.
    /// </summary>
    public Rect ViewportRect => this.viewportRect;

    /// <summary>
    /// Call when the camera is zooming in/out.
    /// This methods adjusts the black bars to make sure players can't see outside of the allowed ratio.
    /// </summary>
    public void AdjustRatio()
    {
        float screenAspect = (float)Screen.width / Screen.height;
        Rect rect = new Rect(0, 0, 1, 1);

        if (screenAspect > this.targetAspect)
        {
            float scale = this.targetAspect / screenAspect;
            rect.width = scale;
            rect.x = (1f - scale) / 2f;
            this.cam.orthographicSize = this.baseOrthoSize;
        }
        else
        {
            float scale = screenAspect / this.targetAspect;
            rect.height = scale;
            rect.y = (1f - scale) / 2f;
            this.cam.orthographicSize = this.baseOrthoSize;
        }

        this.cam.rect = rect;
        this.viewportRect = rect;

        this.AdjustCanvas();
    }

    private void AdjustCanvas()
    {
        if (this.canvasScaler != null && this.canvas != null)
        {
            float widthScale = this.viewportRect.width;
            float heightScale = this.viewportRect.height;

            // Might need to adjust UI elements manually here
        }
    }


    private void Start()
    {
        this.cam = this.GetComponent<Camera>();
        this.baseOrthoSize = this.cam.orthographicSize;

        this.cam.clearFlags = CameraClearFlags.SolidColor;
        this.cam.backgroundColor = Color.black;
        this.AdjustRatio();
    }

// Visual debugging for different aspect ratios.
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (this.cam == null)
        {
            this.cam = this.GetComponent<Camera>();
        }

        this.AdjustRatio();
    }
#endif
}
