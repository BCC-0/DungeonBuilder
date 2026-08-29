using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single inventory folder/category button in the UI.
/// </summary>
public class InventoryFolderUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text folderNameText;

    [SerializeField]
    private Image folderImage;

    private InventoryFolder folder;
    private InventoryBuilderUI inventoryBuilder;

    /// <summary>
    /// Initializes this UI element with an inventory folder.
    /// </summary>
    /// <param name="inventoryFolder">The folder represented by this UI element.</param>
    /// <param name="builder">The inventory UI builder.</param>
    public void Initialize(
            InventoryFolder inventoryFolder,
            InventoryBuilderUI builder)
    {
        this.folder = inventoryFolder;
        this.inventoryBuilder = builder;

        if (this.folderNameText != null)
        {
            this.folderNameText.text = this.folder.Name;
        }

        if (this.folderImage != null)
        {
            this.folderImage.sprite = this.folder.Image;
        }
    }

    /// <summary>
    /// Selects this folder and displays its items.
    /// </summary>
    public void Select()
    {
        if (this.folder == null || this.inventoryBuilder == null)
        {
            return;
        }

        this.inventoryBuilder.ShowFolder(this.folder);
    }
}