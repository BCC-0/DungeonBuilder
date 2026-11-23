using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scales an image with the given amount of pixels per 100 units.
/// </summary>
public class PixelScaledImage : MonoBehaviour
{
    /// <summary>
    /// The default amount of pixels per 100 units used for inventory icons.
    /// </summary>
    public const float DefaultPixelsPer100Units = 16f;

    [SerializeField]
    private RectTransform rectTransform;
    [SerializeField]
    private Image image;
    [SerializeField]
    private float pixelsPer100Units = DefaultPixelsPer100Units;

    /// <summary>
    /// Gets the size an image is displayed at for the given amount of pixels per 100 units.
    /// </summary>
    /// <param name="sprite">The sprite to size.</param>
    /// <param name="pixelsPer100Units">The amount of pixels per 100 units.</param>
    /// <returns>The displayed size of the sprite, or zero if it cannot be sized.</returns>
    public static Vector2 GetScaledSize(Sprite sprite, float pixelsPer100Units)
    {
        if (sprite == null || pixelsPer100Units <= 0f)
        {
            return Vector2.zero;
        }

        float scale = 100f / pixelsPer100Units;

        return new Vector2(
            sprite.rect.width * scale,
            sprite.rect.height * scale);
    }

    private void Update()
    {
        if (this.image == null)
        {
            return;
        }

        Vector2 size = GetScaledSize(
            this.image.sprite,
            this.pixelsPer100Units);

        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        this.rectTransform.sizeDelta = size;
    }
}