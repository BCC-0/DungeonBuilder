using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The slots of the bottom bar in the builder.
/// </summary>
public class ItemBarSlot : MonoBehaviour
{
    [SerializeField]
    private int slotIndex;

    [SerializeField]
    private InventoryItem item;

    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private Image itemImage;

    private InventoryController inventoryController;
    private Tween itemTween;
    private Tween itemSizeTween;

    private Vector2 originalImageSize;
    private Vector2 currentInventorySize;

    /// <summary>
    /// Gets the original (un-animated) inventory size of the item currently in this slot.
    /// </summary>
    public Vector2 InventorySize => this.currentInventorySize;

    /// <summary>
    /// Gets the RectTransform of this slot's item image.
    /// </summary>
    public RectTransform ItemImageRect => this.itemImage.rectTransform;

    /// <summary>
    /// Gets the item this slot contains.
    /// </summary>
    public InventoryItem Item
    {
        get { return this.item; }
    }

    /// <summary>
    /// Gets this slot's zero-based index.
    /// </summary>
    public int SlotIndex
    {
        get { return this.slotIndex; }
    }

    /// <summary>
    /// Sets the item of this slot and animates setting it from the given position.
    /// </summary>
    /// <param name="item">The item this slot should contain.</param>
    /// <param name="screenPos">The on screen position from where to animate the item coming from.</param>
    /// <param name="inventorySize">The size the item had in the inventory.</param>
    public void SetItem(InventoryItem item, Vector3 screenPos, Vector2 inventorySize)
    {
        this.itemTween?.Kill();
        this.itemSizeTween?.Kill();

        this.item = item;

        if (this.item == null)
        {
            this.itemNameText.gameObject.SetActive(false);
            this.itemImage.gameObject.SetActive(false);
            return;
        }

        this.currentInventorySize = inventorySize;

        this.itemNameText.gameObject.SetActive(true);
        this.itemImage.gameObject.SetActive(true);
        this.itemNameText.text = this.item.Name;
        this.itemImage.sprite = this.item.Sprite;

        RectTransform imageTransform = this.itemImage.rectTransform;
        Vector2 targetSize = this.GetAspectRatioSize(inventorySize, this.originalImageSize);
        Vector3 targetPosition = this.transform.position;

        imageTransform.position = screenPos;
        imageTransform.sizeDelta = inventorySize;

        this.itemTween = imageTransform
            .DOMove(targetPosition, 0.25f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        this.itemSizeTween = imageTransform
            .DOSizeDelta(targetSize, 0.25f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    /// <summary>
    /// Selects this slot for assigning an item.
    /// </summary>
    public void Select()
    {
        if (this.inventoryController == null)
        {
            return;
        }

        this.inventoryController.SelectSlot(this.slotIndex);
    }

    /// <summary>
    /// Calculates the largest size that fits inside the target size
    /// while preserving the original aspect ratio.
    /// </summary>
    private Vector2 GetAspectRatioSize(Vector2 originalSize, Vector2 targetSize)
    {
        float aspectRatio = originalSize.x / originalSize.y;

        float width = targetSize.x;
        float height = width / aspectRatio;

        if (height > targetSize.y)
        {
            height = targetSize.y;
            width = height * aspectRatio;
        }

        return new Vector2(width, height);
    }

    private void Start()
    {
        this.inventoryController =
            FindAnyObjectByType<InventoryController>();

        if (this.item == null)
        {
            this.itemNameText.gameObject.SetActive(false);
            this.itemImage.gameObject.SetActive(false);
        }
        else
        {
            this.itemNameText.gameObject.SetActive(true);
            this.itemImage.gameObject.SetActive(true);

            this.itemNameText.text = this.item.Name;
            this.itemImage.sprite = this.item.Sprite;
        }
    }

    private void OnDestroy()
    {
        this.itemTween?.Kill();
        this.itemSizeTween?.Kill();
    }

    private void Awake()
    {
        this.originalImageSize = this.itemImage.rectTransform.rect.size;
    }
}
