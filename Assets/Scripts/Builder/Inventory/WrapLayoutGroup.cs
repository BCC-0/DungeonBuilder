using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays children out from left to right and wraps them
/// onto a new row when they no longer fit.
/// </summary>
public class WrapLayoutGroup : LayoutGroup
{
    [SerializeField]
    private float spacing = 10f;

    [SerializeField]
    private float rowSpacing = 10f;

    /// <summary>
    /// Sets the layout dirty until this layout is needed.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        this.SetWrapDirty();
    }

    /// <summary>
    /// Calculates the horizontal width.
    /// </summary>
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        float width = this.rectTransform.rect.width;
        float requiredWidth = 0f;

        foreach (RectTransform child in this.rectChildren)
        {
            requiredWidth = Mathf.Max(
                requiredWidth,
                child.rect.width);
        }

        this.SetLayoutInputForAxis(
            requiredWidth + this.padding.horizontal,
            requiredWidth + this.padding.horizontal,
            -1,
            0);
    }

    /// <summary>
    /// Calculates the vertical width.
    /// </summary>
    public override void CalculateLayoutInputVertical()
    {
        float width = this.rectTransform.rect.width;

        float currentRowWidth = 0f;
        float currentRowHeight = 0f;
        float totalHeight = 0f;

        bool hasChildren = false;

        foreach (RectTransform child in this.rectChildren)
        {
            float childWidth = child.rect.width;
            float childHeight = child.rect.height;

            float nextWidth = currentRowWidth > 0f
                ? currentRowWidth + this.spacing + childWidth
                : childWidth;

            if (currentRowWidth > 0f && nextWidth > width - this.padding.horizontal)
            {
                totalHeight += currentRowHeight;
                totalHeight += this.rowSpacing;

                currentRowWidth = childWidth;
                currentRowHeight = childHeight;
            }
            else
            {
                currentRowWidth = nextWidth;
                currentRowHeight = Mathf.Max(
                    currentRowHeight,
                    childHeight);
            }

            hasChildren = true;
        }

        if (hasChildren)
        {
            totalHeight += currentRowHeight;
        }

        totalHeight += this.padding.vertical;

        this.SetLayoutInputForAxis(
            totalHeight,
            totalHeight,
            -1,
            1);
    }

    /// <summary>
    /// Sets the layout horizontally.
    /// </summary>
    public override void SetLayoutHorizontal()
    {
        this.LayoutChildren();
    }

    /// <summary>
    /// Sets the layout vertically.
    /// </summary>
    public override void SetLayoutVertical()
    {
        this.LayoutChildren();
    }

    private void LayoutChildren()
    {
        float availableWidth =
            this.rectTransform.rect.width - this.padding.horizontal;

        float x = this.padding.left;
        float y = this.padding.top;

        float rowHeight = 0f;

        foreach (RectTransform child in this.rectChildren)
        {
            float width = child.rect.width;
            float height = child.rect.height;

            // Start a new row if this child doesn't fit.
            if (x > this.padding.left &&
                x + width > this.padding.left + availableWidth)
            {
                x = this.padding.left;
                y += rowHeight + this.rowSpacing;
                rowHeight = 0f;
            }

            this.SetChildAlongAxis(
                child,
                0,
                x,
                width);

            this.SetChildAlongAxis(
                child,
                1,
                y,
                height);

            x += width + this.spacing;

            rowHeight = Mathf.Max(
                rowHeight,
                height);
        }
    }

    /// <summary>
    /// Sets the layout dirty.
    /// </summary>
    private void SetWrapDirty()
    {
        if (!this.IsActive())
        {
            return;
        }

        LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
    }
}