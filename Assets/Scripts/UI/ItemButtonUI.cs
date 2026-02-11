using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The UI handler for the inventory items.
/// </summary>
public class ItemButtonUI : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private Image outlineImage;
    [SerializeField]
    private TextMeshProUGUI nameText;

    private Item item;
    private CrawlerPlayerData player;

    /// <summary>
    /// Sets this button to a certain item.
    /// </summary>
    /// <param name="player">The player that has this item in it's inventory.</param>
    /// <param name="newItem">The item this button will be for.</param>
    public void SetItem(CrawlerPlayerData player, Item newItem)
    {
        this.player = player;
        this.item = newItem;

        if (this.item != null)
        {
            this.iconImage.sprite = this.item.Icon;
            this.nameText.text = this.item.ItemName;
        }
    }

    /// <summary>
    /// Equips the item and shows it using the outline image.
    /// </summary>
    public void OnClick()
    {
        this.player.EquipItem(this, this.item);
        this.outlineImage.enabled = true;
    }

    /// <summary>
    /// Called by the playerData class when another item of this type is selected.
    /// </summary>
    public void Deselect()
    {
        this.outlineImage.enabled = false;
    }

    private void Start()
    {
        this.outlineImage.enabled = false;
    }
}
