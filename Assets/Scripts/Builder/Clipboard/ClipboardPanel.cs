using DG.Tweening;
using UnityEngine;

/// <summary>
/// Slides the clipboard panel up while the clipboard holds something and parks it
/// below the screen when it is empty. Shows a small bounce when something new is
/// copied, so a copy is visible even while the panel is already open.
/// </summary>
public class ClipboardPanel : MonoBehaviour
{
    /// <summary>
    /// How far below its shown position the panel travels when hidden. Must be
    /// large enough to move the whole panel under the screen edge.
    /// </summary>
    [SerializeField]
    private float hiddenDistance = 320f;

    /// <summary>
    /// Seconds the panel takes to slide in and out.
    /// </summary>
    [SerializeField]
    private float slideDuration = 0.35f;

    /// <summary>
    /// How far the panel rises when new content is copied while it is already shown.
    /// </summary>
    [SerializeField]
    private float bounceHeight = 40f;

    /// <summary>
    /// Seconds the bounce takes in total.
    /// </summary>
    [SerializeField]
    private float bounceDuration = 0.25f;

    /// <summary>
    /// Optional group used to fade the panel and to stop clicks reaching it while
    /// it is hidden. Assigning one is recommended, because the panel keeps running
    /// while it is off screen.
    /// </summary>
    [SerializeField]
    private CanvasGroup canvasGroup;

    /// <summary>
    /// The position the panel is configured at in the scene, which is where it is shown.
    /// </summary>
    private Vector2 shownPosition;

    private RectTransform panelRect;

    private Sequence sequence;

    private bool isShown;

    /// <summary>
    /// Pastes the clipboard at the pointer: the copies are created there and can be
    /// moved into place before they are confirmed.
    /// </summary>
    public void Paste()
    {
        MapEditorManager.Instance.OnPasteSelection();
    }

    /// <summary>
    /// Empties the clipboard, which slides the panel back below the screen.
    /// </summary>
    public void ClearClipboard()
    {
        BuilderClipboard.Clear();
    }

    /// <summary>
    /// Caches the configured position, listens for clipboard changes and puts the
    /// panel straight into the state matching the clipboard, without animating.
    /// </summary>
    private void Awake()
    {
        this.panelRect = (RectTransform)this.transform;
        this.shownPosition = this.panelRect.anchoredPosition;

        BuilderClipboard.Changed += this.OnClipboardChanged;

        this.ApplyInstant(BuilderClipboard.HasContent);
    }

    /// <summary>
    /// Stops listening to clipboard changes and kills a running tween.
    /// </summary>
    private void OnDestroy()
    {
        BuilderClipboard.Changed -= this.OnClipboardChanged;

        this.sequence?.Kill();
    }

    /// <summary>
    /// Slides the panel in when the clipboard was filled, bounces it when it was
    /// already open, and slides it out when the clipboard was emptied.
    /// </summary>
    private void OnClipboardChanged()
    {
        if (!BuilderClipboard.HasContent)
        {
            this.Hide();
            return;
        }

        if (this.isShown)
        {
            this.Bounce();
        }
        else
        {
            this.Show();
        }
    }

    /// <summary>
    /// Puts the panel into the given state immediately, used on start so the panel
    /// does not animate before the first copy.
    /// </summary>
    /// <param name="shown">Whether the panel should be visible.</param>
    private void ApplyInstant(bool shown)
    {
        this.isShown = shown;

        this.panelRect.anchoredPosition =
            shown ? this.shownPosition : this.GetHiddenPosition();

        if (this.canvasGroup != null)
        {
            this.canvasGroup.alpha = shown ? 1f : 0f;
        }

        this.SetInteractable(shown);
    }

    /// <summary>
    /// Slides the panel up into its shown position.
    /// </summary>
    private void Show()
    {
        this.isShown = true;

        this.sequence?.Kill();

        this.SetInteractable(true);

        if (this.canvasGroup != null)
        {
            this.canvasGroup.alpha = 0f;
        }

        this.sequence = DOTween.Sequence();

        this.sequence.Join(
            this.panelRect
                .DOAnchorPos(this.shownPosition, this.slideDuration)
                .SetEase(Ease.OutBack));

        if (this.canvasGroup != null)
        {
            this.sequence.Join(
                this.canvasGroup.DOFade(1f, this.slideDuration));
        }
    }

    /// <summary>
    /// Bounces the panel up a little and lets it settle back, as feedback that
    /// something new was copied while the panel was already open.
    /// </summary>
    private void Bounce()
    {
        this.sequence?.Kill();

        this.sequence = DOTween.Sequence();

        this.sequence
            .Append(
                this.panelRect
                    .DOAnchorPos(
                        this.shownPosition + (Vector2.up * this.bounceHeight),
                        this.bounceDuration * 0.4f)
                    .SetEase(Ease.OutQuad))
            .Append(
                this.panelRect
                    .DOAnchorPos(
                        this.shownPosition,
                        this.bounceDuration * 0.6f)
                    .SetEase(Ease.OutBounce));
    }

    /// <summary>
    /// Slides the panel down below the screen and stops it from taking clicks.
    /// </summary>
    private void Hide()
    {
        this.isShown = false;

        this.sequence?.Kill();

        this.SetInteractable(false);

        this.sequence = DOTween.Sequence();

        this.sequence.Join(
            this.panelRect
                .DOAnchorPos(this.GetHiddenPosition(), this.slideDuration)
                .SetEase(Ease.InQuad));

        if (this.canvasGroup != null)
        {
            this.sequence.Join(
                this.canvasGroup.DOFade(0f, this.slideDuration));
        }
    }

    /// <summary>
    /// Gets the position the panel is parked at while the clipboard is empty.
    /// </summary>
    /// <returns>The hidden anchored position.</returns>
    private Vector2 GetHiddenPosition()
    {
        return this.shownPosition + (Vector2.down * this.hiddenDistance);
    }

    /// <summary>
    /// Enables or disables clicks on the panel and everything inside it.
    /// </summary>
    /// <param name="interactable">Whether the panel should take clicks.</param>
    private void SetInteractable(bool interactable)
    {
        if (this.canvasGroup == null)
        {
            return;
        }

        this.canvasGroup.blocksRaycasts = interactable;
        this.canvasGroup.interactable = interactable;
    }
}
