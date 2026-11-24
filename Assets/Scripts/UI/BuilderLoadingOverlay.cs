using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a dimmed screen with a spinning pixel mark and a progress bar while the
/// builder does work that outlasts a frame, such as pasting many entities. The UI
/// is built in code, so it needs no scene, prefab, sprite or font asset. While it
/// is visible its backdrop blocks pointer input, because the builder ignores
/// clicks that land on UI.
/// </summary>
public class BuilderLoadingOverlay : MonoBehaviour
{
    /// <summary>
    /// Seconds the overlay takes to fade in and out.
    /// </summary>
    private const float FadeDuration = 0.12f;

    /// <summary>
    /// Degrees per second the spinner turns.
    /// </summary>
    private const float SpinSpeed = 220f;

    /// <summary>
    /// Number of blocks in the spinner ring.
    /// </summary>
    private const int SpinnerBlockCount = 8;

    /// <summary>
    /// Side length of one spinner block, in canvas units.
    /// </summary>
    private const float SpinnerBlockSize = 14f;

    /// <summary>
    /// Distance of the spinner blocks from the spinner's center, in canvas units.
    /// </summary>
    private const float SpinnerRadius = 46f;

    /// <summary>
    /// Size of the progress bar, in canvas units.
    /// </summary>
    private static readonly Vector2 ProgressBarSize = new Vector2(360f, 10f);

    /// <summary>
    /// The overlay that was created, so the same one is reused.
    /// </summary>
    private static BuilderLoadingOverlay instance;

    private RectTransform spinner;
    private RectTransform progressFill;
    private CanvasGroup canvasGroup;
    private bool shouldBeVisible;

    /// <summary>
    /// Gets a value indicating whether the overlay is shown or still fading out.
    /// </summary>
    public static bool IsVisible => instance != null && instance.shouldBeVisible;

    /// <summary>
    /// Shows the overlay, creating it on first use.
    /// </summary>
    public static void Show()
    {
        BuilderLoadingOverlay overlay = BuilderLoadingOverlay.GetInstance();

        overlay.shouldBeVisible = true;
        overlay.gameObject.SetActive(true);
    }

    /// <summary>
    /// Sets how much of the work is done, so the bar fills while the task runs.
    /// </summary>
    /// <param name="progress">The completed fraction of the work, from 0 to 1.</param>
    public static void SetProgress(float progress)
    {
        if (instance == null || instance.progressFill == null)
        {
            return;
        }

        instance.progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
    }

    /// <summary>
    /// Fades the overlay out and hides it once it is transparent.
    /// </summary>
    public static void Hide()
    {
        if (instance == null)
        {
            return;
        }

        instance.shouldBeVisible = false;

        // A task that was over within a frame must not leave a flash behind.
        if (instance.canvasGroup.alpha <= 0f)
        {
            instance.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gets the overlay, creating it when it does not exist yet.
    /// </summary>
    /// <returns>The overlay instance.</returns>
    private static BuilderLoadingOverlay GetInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject overlayObject =
            new GameObject("Builder Loading Overlay", typeof(RectTransform));

        instance = overlayObject.AddComponent<BuilderLoadingOverlay>();
        instance.Build();

        // The overlay has to outlive the scene it was created in, because it is
        // shown while the next scene loads.
        Object.DontDestroyOnLoad(overlayObject);

        return instance;
    }

    /// <summary>
    /// Creates the canvas, the backdrop, the spinner and the progress bar.
    /// </summary>
    private void Build()
    {
        Canvas canvas = this.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = this.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        this.gameObject.AddComponent<GraphicRaycaster>();

        this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
        this.canvasGroup.alpha = 0f;
        this.canvasGroup.blocksRaycasts = true;
        this.canvasGroup.interactable = false;

        this.CreateBackdrop();
        this.spinner = this.CreateSpinner();
        this.CreateProgressBar();
    }

    /// <summary>
    /// Creates the dark full-screen layer that hides the scene and eats clicks.
    /// </summary>
    private void CreateBackdrop()
    {
        GameObject backdropObject =
            new GameObject("Backdrop", typeof(RectTransform));

        backdropObject.transform.SetParent(this.transform, false);

        RectTransform rect = (RectTransform)backdropObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = backdropObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);
        image.raycastTarget = true;
    }

    /// <summary>
    /// Creates the ring of blocks that the rotation animates.
    /// </summary>
    /// <returns>The spinner's rect, which is turned every frame.</returns>
    private RectTransform CreateSpinner()
    {
        GameObject spinnerObject =
            new GameObject("Spinner", typeof(RectTransform));

        spinnerObject.transform.SetParent(this.transform, false);

        RectTransform spinnerRect = (RectTransform)spinnerObject.transform;
        spinnerRect.anchoredPosition = new Vector2(0f, 20f);
        spinnerRect.sizeDelta = new Vector2(SpinnerRadius * 2f, SpinnerRadius * 2f);

        for (int i = 0; i < SpinnerBlockCount; i++)
        {
            this.CreateSpinnerBlock(spinnerRect, i);
        }

        return spinnerRect;
    }

    /// <summary>
    /// Creates one block of the spinner ring, with a fading tail so the rotation
    /// reads as movement.
    /// </summary>
    /// <param name="parent">The spinner the block belongs to.</param>
    /// <param name="index">The position of the block in the ring.</param>
    private void CreateSpinnerBlock(RectTransform parent, int index)
    {
        float angle =
            (Mathf.PI * 2f * index) / SpinnerBlockCount;

        GameObject blockObject =
            new GameObject("Block", typeof(RectTransform));

        blockObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)blockObject.transform;
        rect.sizeDelta = new Vector2(SpinnerBlockSize, SpinnerBlockSize);
        rect.anchoredPosition =
            new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * SpinnerRadius;

        Image image = blockObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(
            1f,
            1f,
            1f,
            Mathf.Lerp(1f, 0.15f, index / (float)SpinnerBlockCount));
    }

    /// <summary>
    /// Creates the progress bar that the work fills up while it runs.
    /// </summary>
    private void CreateProgressBar()
    {
        GameObject barObject =
            new GameObject("Progress Bar", typeof(RectTransform));

        barObject.transform.SetParent(this.transform, false);

        RectTransform barRect = (RectTransform)barObject.transform;
        barRect.anchoredPosition = new Vector2(0f, -80f);
        barRect.sizeDelta = ProgressBarSize;

        Image background = barObject.AddComponent<Image>();
        background.raycastTarget = false;
        background.color = new Color(1f, 1f, 1f, 0.2f);

        GameObject fillObject =
            new GameObject("Fill", typeof(RectTransform));

        fillObject.transform.SetParent(barObject.transform, false);

        this.progressFill = (RectTransform)fillObject.transform;
        this.progressFill.anchorMin = Vector2.zero;
        this.progressFill.anchorMax = Vector2.one;
        this.progressFill.offsetMin = Vector2.zero;
        this.progressFill.offsetMax = Vector2.zero;

        Image fill = fillObject.AddComponent<Image>();
        fill.raycastTarget = false;
        fill.color = Color.white;
    }

    /// <summary>
    /// Turns the spinner and fades the overlay in or out.
    /// </summary>
    private void Update()
    {
        if (this.spinner != null)
        {
            this.spinner.Rotate(0f, 0f, -SpinSpeed * Time.unscaledDeltaTime);
        }

        if (this.canvasGroup == null)
        {
            return;
        }

        this.canvasGroup.alpha = Mathf.MoveTowards(
            this.canvasGroup.alpha,
            this.shouldBeVisible ? 1f : 0f,
            Time.unscaledDeltaTime / FadeDuration);

        if (!this.shouldBeVisible &&
            this.canvasGroup.alpha <= 0f)
        {
            this.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Forgets the instance when the overlay is destroyed, for example on a scene change.
    /// </summary>
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
