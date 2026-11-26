using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The popup that shows the name and description of the thing the cursor
/// rests on. Lives on the popup prefab.
/// </summary>
public class HoverTooltip : MonoBehaviour
{
    [SerializeField]
    private SpriteFontText nameText;

    [SerializeField]
    private SpriteFontText descriptionText;

    [SerializeField]
    [Tooltip("Optional. Hidden when the description is empty.")]
    private GameObject descriptionContainer;

    private RectTransform rect;

    /// <summary>
    /// Gets the rect transform of the popup.
    /// </summary>
    public RectTransform Rect
    {
        get
        {
            if (this.rect == null)
            {
                this.rect = this.GetComponent<RectTransform>();
            }

            return this.rect;
        }
    }

    /// <summary>
    /// Shows or hides the popup.
    /// </summary>
    /// <param name="visible">Whether the popup should be visible.</param>
    public void SetVisible(bool visible)
    {
        this.gameObject.SetActive(visible);

        if (visible)
        {
            this.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Sets the name and description shown in the popup.
    /// </summary>
    /// <param name="info">The info to show.</param>
    public void SetContent(HoverInfo info)
    {
        if (info == null)
        {
            info = HoverInfo.Empty;
        }

        if (this.nameText != null)
        {
            this.nameText.SetText(info.Name);
        }

        if (this.descriptionText != null)
        {
            this.descriptionText.SetText(info.Description);
        }

        if (this.descriptionContainer != null)
        {
            this.descriptionContainer.SetActive(
                !string.IsNullOrEmpty(info.Description));
        }

        if (this.GetComponent<ContentSizeFitter>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.Rect);
        }
    }

    /// <summary>
    /// Moves the popup so a corner sits exactly on the cursor. The top left
    /// corner is used, unless that would put the popup past the right side of
    /// its parent, in which case the top right corner is used and the popup
    /// opens to the left.
    /// </summary>
    /// <param name="screenPosition">The cursor position in screen pixels.</param>
    public void SetScreenPosition(Vector2 screenPosition)
    {
        RectTransform parent = this.Rect.parent as RectTransform;

        if (parent == null)
        {
            return;
        }

        Canvas canvas = this.GetComponentInParent<Canvas>();

        Camera camera = canvas != null &&
                        canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                screenPosition,
                camera,
                out Vector2 localPosition))
        {
            return;
        }

        Vector2 size = this.Rect.rect.size;
        Vector2 pivot = this.Rect.pivot;
        Rect parentRect = parent.rect;

        // The pivot point sits somewhere inside the rect, so shift the rect
        // until a corner is on the cursor. When the popup would stick out of
        // the right side of its parent, the top right corner is used so the
        // popup opens to the left instead.
        bool useRightCorner = localPosition.x + size.x > parentRect.xMax;

        Vector2 corner = new Vector2(
            useRightCorner ? -(1f - pivot.x) * size.x : pivot.x * size.x,
            -(1f - pivot.y) * size.y);

        this.Rect.localPosition = new Vector3(
            localPosition.x + corner.x,
            localPosition.y + corner.y,
            0f);
    }

    /// <summary>
    /// The popup must never catch the cursor, otherwise it would hide itself
    /// right after showing.
    /// </summary>
    private void Awake()
    {
        foreach (Graphic graphic in this.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        if (this.nameText != null)
        {
            this.nameText.KeepWordsWhole = true;
        }

        if (this.descriptionText != null)
        {
            this.descriptionText.KeepWordsWhole = true;
        }
    }
}
