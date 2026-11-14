
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The UI handler for the inventory items.
/// </summary>
public class ItemButtonUI : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField]
    private Image pocketImage;

    [SerializeField]
    private Sprite[] pocketSprites;

    [Header("Item")]
    [SerializeField]
    private Image itemImage;

    [Header("Selection Animation")]
    [SerializeField]
    private float slideDistance = 20f;

    [SerializeField]
    private float slideDuration = 0.2f;

    private Item item;
    private CrawlerPlayerData player;

    private Vector2 itemRestPosition;
    private Tween slideTween;

    /// <summary>
    /// Gets the type of this item.
    /// </summary>
    public ItemType ItemType => this.item.ItemType;

    /// <summary>
    /// Gets the item.
    /// </summary>
    public Item Item => this.item;

    /// <summary>
    /// Sets this button to a certain item.
    /// </summary>
    /// <param name="player">The player that controls this item.</param>
    /// <param name="newItem">The new item to set this to.</param>
    public void SetItem(CrawlerPlayerData player, Item newItem)
    {
        this.player = player;
        this.item = newItem;

        if (this.item != null)
        {
            this.itemImage.sprite = this.item.Icon;
            this.itemImage.enabled = true;
        }
        else
        {
            this.itemImage.enabled = false;
        }
    }

    /// <summary>
    /// Equips the item and shows its selection animation.
    /// </summary>
    public void OnClick()
    {
        this.player.EquipItem(this.item);
        this.OnSelect();
    }

    /// <summary>
    /// Called when this item is selected.
    /// </summary>
    public void OnSelect()
    {
        this.slideTween?.Kill();

        Vector2 selectedPosition =
            this.itemRestPosition + (Vector2.up * this.slideDistance);

        this.slideTween = this.itemImage.rectTransform
            .DOAnchorPos(selectedPosition, this.slideDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    /// <summary>
    /// Called by the playerData class when another item of this type is selected.
    /// </summary>
    public void Deselect()
    {
        this.slideTween?.Kill();

        this.slideTween = this.itemImage.rectTransform
            .DOAnchorPos(this.itemRestPosition, this.slideDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void Awake()
    {
        this.itemRestPosition = this.itemImage.rectTransform.anchoredPosition;

        if (this.pocketSprites != null && this.pocketSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, this.pocketSprites.Length);
            this.pocketImage.sprite = this.pocketSprites[randomIndex];
        }

        // Keep the item behind the pocket.
        this.itemImage.transform.SetAsFirstSibling();
        this.pocketImage.transform.SetAsLastSibling();
    }

    private void OnDestroy()
    {
        this.slideTween?.Kill();
    }
}
