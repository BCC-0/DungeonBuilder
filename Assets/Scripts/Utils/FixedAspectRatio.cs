using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes sure the canvas scales with the ratio for each possible resolution.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FixedAspectRatio : MonoBehaviour
{
    [SerializeField]
    private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField]
    private CanvasScaler canvasScaler;
    [SerializeField]
    private Canvas canvas;

    private Camera cam;
    private Rect viewportRect;
    private int lastWidth;
    private int lastHeight;

    /// <summary>
    /// Gets the viewport rect.
    /// </summary>
    public Rect ViewportRect => this.viewportRect;

    /// <summary>
    /// Adjusts the ratio.
    /// </summary>
    public void AdjustRatio()
    {
        int screenW = Screen.width;
        int screenH = Screen.height;
        float targetAspect = this.referenceResolution.x / this.referenceResolution.y;

        int w, h;
        if ((float)screenW / screenH > targetAspect)
        {
            h = screenH;
            w = Mathf.RoundToInt(h * targetAspect);
        }
        else
        {
            w = screenW;
            h = Mathf.RoundToInt(w / targetAspect);
        }

        // Whole-pixel rect (no fractional edges).
        this.cam.pixelRect = new Rect((screenW - w) / 2, (screenH - h) / 2, w, h);
        this.viewportRect = this.cam.rect;

        this.lastWidth = screenW;
        this.lastHeight = screenH;

        this.AdjustCanvas(h);
    }

    private void AdjustCanvas(int viewportPixelHeight)
    {
        if (this.canvas == null || this.canvasScaler == null)
        {
            return;
        }

        this.canvas.renderMode = RenderMode.ScreenSpaceCamera;
        this.canvas.worldCamera = this.cam;
        this.canvas.pixelPerfect = true;

        // CanvasScaler measures the full screen, not the cropped camera, so set the scale directly.
        this.canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        this.canvasScaler.scaleFactor = viewportPixelHeight / this.referenceResolution.y;
    }

    private void Start()
    {
        this.cam = this.GetComponent<Camera>();
        this.cam.clearFlags = CameraClearFlags.SolidColor;
        this.cam.backgroundColor = Color.black;
        this.AdjustRatio();
    }

    private void Update()
    {
        if (Screen.width != this.lastWidth || Screen.height != this.lastHeight)
        {
            this.AdjustRatio();
        }
    }
}