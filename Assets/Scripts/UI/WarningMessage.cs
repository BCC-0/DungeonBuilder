using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One warning line in the builder UI. The <see cref="WarningManager"/> creates
/// it from the warning prefab, gives it the text and stacks it on top of the
/// warnings that were already shown.
/// </summary>
public class WarningMessage : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The sprite font text that draws the message. Found on the prefab when it is left empty.")]
    private SpriteFontText messageText;

    [SerializeField]
    [Tooltip("The height the message takes up, so a short message still has room to be read.")]
    private float minimumHeight = 50f;

    [SerializeField]
    [Tooltip("How far the message slides up while it fades in, and further up while it fades out.")]
    private float riseDistance = 24f;

    private CanvasGroup group;
    private RectTransform rect;
    private Sequence sequence;
    private float baseY;
    private float riseOffset;

    /// <summary>
    /// Raised when the message has faded out and can be removed.
    /// </summary>
    public event Action<WarningMessage> Finished;

    /// <summary>
    /// Gets the rect transform of the message, which the manager positions.
    /// </summary>
    public RectTransform Rect
    {
        get
        {
            if (this.rect == null)
            {
                this.rect = (RectTransform)this.transform;
            }

            return this.rect;
        }
    }

    /// <summary>
    /// Sets the height the message sits at inside the parent. The manager calls
    /// this whenever a warning is added or removed below it.
    /// </summary>
    /// <param name="y">The anchored height of the message in the parent.</param>
    public void SetBasePosition(float y)
    {
        this.baseY = y;
        this.ApplyPosition();
    }

    /// <summary>
    /// Sets the text of the message and grows the message so the whole text
    /// fits. Long text is wrapped at the width of the parent.
    /// </summary>
    /// <param name="text">The complete text to show, prefix included.</param>
    public void SetMessage(string text)
    {
        if (this.messageText != null)
        {
            this.messageText.SetText(text);
        }

        this.UpdateHeight();
    }

    /// <summary>
    /// Fades the message in while it slides up into place, keeps it readable
    /// and then fades it out while it slides up further.
    /// </summary>
    /// <param name="fadeInDuration">Seconds the message takes to fade in.</param>
    /// <param name="visibleDuration">Seconds the message stays fully visible.</param>
    /// <param name="fadeOutDuration">Seconds the message takes to fade out.</param>
    public void Show(
        float fadeInDuration,
        float visibleDuration,
        float fadeOutDuration)
    {
        this.KillSequence();
        this.group.alpha = 0f;
        this.riseOffset = -this.riseDistance;
        this.ApplyPosition();

        this.sequence = DOTween.Sequence();

        // The builder stops time while the inventory is open, so the warning
        // uses unscaled time and keeps fading no matter what.
        this.sequence.SetUpdate(true);
        this.sequence.Append(
            this.group.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));
        this.sequence.Join(
            this.CreateRiseTween(0f, fadeInDuration).SetEase(Ease.OutQuad));
        this.sequence.AppendInterval(visibleDuration);
        this.sequence.Append(
            this.group.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));

        // The slide is only an offset on top of the height the manager gives
        // the message, so a warning that was pushed up still leaves upwards.
        this.sequence.Join(
            this.CreateRiseTween(this.riseDistance, fadeOutDuration)
                .SetEase(Ease.InQuad));
        this.sequence.OnComplete(this.HandleFinished);
    }

    /// <summary>
    /// Removes the message right away, used when too many warnings are shown
    /// at the same time.
    /// </summary>
    public void Dismiss()
    {
        this.KillSequence();
        this.Finished?.Invoke(this);
    }

    /// <summary>
    /// Places the message at the bottom of the parent and makes sure the clicks
    /// meant for the UI behind it are not caught.
    /// </summary>
    private void Awake()
    {
        this.PrepareRect();
        this.baseY = this.Rect.anchoredPosition.y;

        this.group = this.GetComponent<CanvasGroup>();

        if (this.group == null)
        {
            this.group = this.gameObject.AddComponent<CanvasGroup>();
        }

        this.group.alpha = 0f;

        if (this.messageText == null)
        {
            this.messageText = this.GetComponentInChildren<SpriteFontText>();
        }

        if (this.messageText != null)
        {
            this.messageText.KeepWordsWhole = true;
        }

        foreach (Graphic graphic in this.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
    }

    /// <summary>
    /// Keeps the message at the height the manager gave it, moved by the slide.
    /// </summary>
    private void LateUpdate()
    {
        this.ApplyPosition();
    }

    /// <summary>
    /// Moves the message to its stacked height plus the current slide.
    /// </summary>
    private void ApplyPosition()
    {
        this.Rect.anchoredPosition = new Vector2(
            this.Rect.anchoredPosition.x,
            this.baseY + this.riseOffset);
    }

    /// <summary>
    /// Creates the slide that takes the current offset to the wanted one.
    /// </summary>
    /// <param name="endOffset">The offset the message slides to.</param>
    /// <param name="duration">Seconds the slide takes.</param>
    /// <returns>The slide tween, to put into the sequence.</returns>
    private Tweener CreateRiseTween(float endOffset, float duration)
    {
        return DOTween.To(
            () => this.riseOffset,
            value => this.riseOffset = value,
            endOffset,
            duration);
    }

    /// <summary>
    /// Stops the fade when the message is removed.
    /// </summary>
    private void OnDestroy()
    {
        this.KillSequence();
        this.Finished = null;
    }

    /// <summary>
    /// Stretches the message across the parent, keeps its bottom edge where it
    /// is and leaves the height to the text.
    /// </summary>
    private void PrepareRect()
    {
        RectTransform rectTransform = this.Rect;
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);
        rectTransform.anchoredPosition = new Vector2(
            0f, rectTransform.anchoredPosition.y);
    }

    /// <summary>
    /// Sizes the message so the wrapped text fits in it.
    /// </summary>
    private void UpdateHeight()
    {
        float height = this.minimumHeight;

        if (this.messageText != null)
        {
            TMP_Text target = this.messageText.GetComponent<TMP_Text>();

            if (target != null)
            {
                float width = this.Rect.rect.width;
                float preferredHeight =
                    target.GetPreferredValues(target.text, width, 0f).y;

                height = Mathf.Max(height, preferredHeight);
            }
        }

        this.Rect.sizeDelta = new Vector2(this.Rect.sizeDelta.x, height);
    }

    /// <summary>
    /// Tells the manager that the message is done and can be thrown away.
    /// </summary>
    private void HandleFinished()
    {
        this.KillSequence();
        this.Finished?.Invoke(this);
    }

    /// <summary>
    /// Stops the fade if one is running.
    /// </summary>
    private void KillSequence()
    {
        this.sequence?.Kill();
        this.sequence = null;
    }
}
