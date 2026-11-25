using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The grab points shown around a selection for resizing it. Everything is
/// created in code, so no sprite, prefab or scene object has to be set up:
/// the handles are plain UI images without a sprite.
/// Each image is placed with a world position computed from the selection box
/// parent, so the handles use exactly the same screen space as the selection box
/// itself and do not depend on anchors or pivots.
/// </summary>
public class SelectionResizeHandles : MonoBehaviour
{
    /// <summary>
    /// Size of a handle in screen pixels on desktop.
    /// </summary>
    private const float DesktopHandleSize = 12f;

    /// <summary>
    /// Size of a handle in screen pixels on touch screens, where fingers need
    /// a bigger target than a mouse cursor.
    /// </summary>
    private const float TouchHandleSize = 26f;

    /// <summary>
    /// How much bigger than the drawn size the hit area of a handle is, so a
    /// handle is easy to grab without aiming exactly at its centre.
    /// </summary>
    private const float HitAreaScale = 2.4f;

    private static readonly ResizeHandle[] HitPriority =
    {
        ResizeHandle.TopLeft,
        ResizeHandle.TopRight,
        ResizeHandle.BottomLeft,
        ResizeHandle.BottomRight,
        ResizeHandle.Center,
        ResizeHandle.Left,
        ResizeHandle.Right,
        ResizeHandle.Top,
        ResizeHandle.Bottom,
    };

    private readonly Dictionary<ResizeHandle, Image> handleImages = new ();

    private RectTransform parent;
    private Camera uiCamera;
    private Color handleColor = Color.white;
    private float handleScreenSize;
    private bool isVisible;

    /// <summary>
    /// Creates the handle images under the selection box parent.
    /// </summary>
    /// <param name="parent">The rect the handles are positioned in.</param>
    /// <param name="cam">The camera the canvas renders with, if it is not an overlay canvas.</param>
    /// <param name="color">The color of the selection outline, which the handles match.</param>
    public void Initialize(
        RectTransform parent,
        Camera cam,
        Color color)
    {
        this.parent = parent;
        this.handleColor = color;

        if (this.parent == null)
        {
            return;
        }

        Canvas canvas = this.parent.GetComponentInParent<Canvas>();

        this.uiCamera =
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

        this.CreateHandle(ResizeHandle.TopLeft);
        this.CreateHandle(ResizeHandle.Top);
        this.CreateHandle(ResizeHandle.TopRight);
        this.CreateHandle(ResizeHandle.Left);
        this.CreateHandle(ResizeHandle.Center);
        this.CreateHandle(ResizeHandle.Right);
        this.CreateHandle(ResizeHandle.BottomLeft);
        this.CreateHandle(ResizeHandle.Bottom);
        this.CreateHandle(ResizeHandle.BottomRight);

        this.SetVisible(false);
    }

    /// <summary>
    /// Shows or hides every handle.
    /// </summary>
    /// <param name="visible">Whether the handles should be visible.</param>
    public void SetVisible(bool visible)
    {
        this.isVisible = visible;

        foreach (Image image in this.handleImages.Values)
        {
            image.enabled = visible;
        }
    }

    /// <summary>
    /// Highlights the handle that is currently being dragged.
    /// </summary>
    /// <param name="handle">The dragged handle, or <see cref="ResizeHandle.None"/>.</param>
    public void SetGrabbed(ResizeHandle handle)
    {
        foreach (KeyValuePair<ResizeHandle, Image> entry in this.handleImages)
        {
            entry.Value.color = entry.Key == handle
                ? this.GetOutlineColor(1f)
                : entry.Key == ResizeHandle.Center
                    ? this.GetOutlineColor(0.45f)
                    : this.GetOutlineColor(this.handleColor.a);
        }
    }

    /// <summary>
    /// Places the handles on the corners, edge midpoints and centre of a
    /// selection, expressed in the local space of the selection box parent.
    /// </summary>
    /// <param name="uiMin">The lower left corner of the selection.</param>
    /// <param name="uiMax">The upper right corner of the selection.</param>
    /// <param name="screenPixelsToLocal">How many local units one screen pixel is.</param>
    public void Layout(
        Vector2 uiMin,
        Vector2 uiMax,
        float screenPixelsToLocal)
    {
        if (this.parent == null)
        {
            return;
        }

        bool desktop =
            BuilderInputSelector.Instance != null &&
            BuilderInputSelector.Instance.IsUsingDesktop;

        this.handleScreenSize = desktop ? DesktopHandleSize : TouchHandleSize;

        Vector2 size = new Vector2(
            this.handleScreenSize * screenPixelsToLocal,
            this.handleScreenSize * screenPixelsToLocal);

        Vector2 center = (uiMin + uiMax) * 0.5f;

        foreach (KeyValuePair<ResizeHandle, Image> entry in this.handleImages)
        {
            Vector2 point = GetHandlePoint(entry.Key, uiMin, uiMax, center);

            RectTransform rect = entry.Value.rectTransform;

            rect.sizeDelta = size;
            rect.position = this.parent.TransformPoint(point);
        }
    }

    /// <summary>
    /// Finds the handle under a screen point. Corners and the centre win over
    /// edges, so overlapping handles on a small selection still pick a useful one.
    /// </summary>
    /// <param name="screenPos">The screen position to test.</param>
    /// <param name="handle">The handle under the point.</param>
    /// <returns>True when a handle is under the point.</returns>
    public bool TryHit(Vector2 screenPos, out ResizeHandle handle)
    {
        handle = ResizeHandle.None;

        if (!this.isVisible || this.handleImages.Count == 0)
        {
            return false;
        }

        float reach = (this.handleScreenSize * HitAreaScale) * 0.5f;

        foreach (ResizeHandle candidate in HitPriority)
        {
            if (!this.handleImages.TryGetValue(candidate, out Image image) ||
                !image.enabled)
            {
                continue;
            }

            Vector2 center = RectTransformUtility.WorldToScreenPoint(
                this.uiCamera,
                image.rectTransform.position);

            if (Mathf.Abs(screenPos.x - center.x) <= reach &&
                Mathf.Abs(screenPos.y - center.y) <= reach)
            {
                handle = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds a color from the outline color with a different alpha, so the
    /// handles read as part of the same selection visual.
    /// </summary>
    /// <param name="alpha">The alpha to use.</param>
    /// <returns>The color of the handle.</returns>
    private Color GetOutlineColor(float alpha)
    {
        return new Color(
            this.handleColor.r,
            this.handleColor.g,
            this.handleColor.b,
            alpha);
    }

    /// <summary>
    /// Gets where a handle belongs on a selection rectangle.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <param name="uiMin">The lower left corner of the selection.</param>
    /// <param name="uiMax">The upper right corner of the selection.</param>
    /// <param name="center">The middle of the selection.</param>
    /// <returns>The local position of the handle.</returns>
    private static Vector2 GetHandlePoint(
        ResizeHandle handle,
        Vector2 uiMin,
        Vector2 uiMax,
        Vector2 center)
    {
        return handle switch
        {
            ResizeHandle.TopLeft => new Vector2(uiMin.x, uiMax.y),
            ResizeHandle.Top => new Vector2(center.x, uiMax.y),
            ResizeHandle.TopRight => new Vector2(uiMax.x, uiMax.y),
            ResizeHandle.Left => new Vector2(uiMin.x, center.y),
            ResizeHandle.Center => center,
            ResizeHandle.Right => new Vector2(uiMax.x, center.y),
            ResizeHandle.BottomLeft => new Vector2(uiMin.x, uiMin.y),
            ResizeHandle.Bottom => new Vector2(center.x, uiMin.y),
            ResizeHandle.BottomRight => new Vector2(uiMax.x, uiMin.y),
            _ => center,
        };
    }

    /// <summary>
    /// Creates one handle image under the selection box parent.
    /// </summary>
    /// <param name="handle">The handle the image belongs to.</param>
    private void CreateHandle(ResizeHandle handle)
    {
        GameObject handleObject = new ($"Resize {handle}", typeof(RectTransform), typeof(Image));

        handleObject.transform.SetParent(this.parent, false);

        RectTransform rect = handleObject.transform as RectTransform;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = handleObject.GetComponent<Image>();

        // A sprite is not needed: an image without a sprite draws a plain quad.
        image.sprite = null;
        image.color = handle == ResizeHandle.Center
            ? this.GetOutlineColor(0.45f)
            : this.GetOutlineColor(this.handleColor.a);

        // The handles are hit tested in code, so they must not swallow world input.
        image.raycastTarget = false;

        this.handleImages[handle] = image;
    }
}
