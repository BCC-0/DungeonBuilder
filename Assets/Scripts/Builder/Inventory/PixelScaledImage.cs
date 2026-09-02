using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scales an image with the given amount of pixels per 100 units.
/// </summary>
public class PixelScaledImage : MonoBehaviour
{
    [SerializeField]
    private RectTransform rectTransform;
    [SerializeField]
    private Image image;
    [SerializeField]
    private float pixelsPer100Units = 16f;

    private void Update()
    {
        if (this.image == null)
        {
            return;
        }

        Sprite sprite = this.image.sprite;

        if (sprite == null)
        {
            return;
        }

        float scale = 100f / this.pixelsPer100Units;

        this.rectTransform.sizeDelta = new Vector2(
            sprite.rect.width * scale,
            sprite.rect.height * scale);
    }
}