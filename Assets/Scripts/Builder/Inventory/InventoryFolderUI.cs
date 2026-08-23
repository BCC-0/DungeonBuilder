using TMPro;
using UnityEngine;

/// <summary>
/// Controls a single inventory folder/category in the UI.
/// </summary>
public class InventoryFolderUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text folderNameText;

    [SerializeField]
    private Transform itemContainer;

    [SerializeField]
    private GameObject itemPrefab;

    /// <summary>
    /// Initializes the folder UI and creates its inventory items.
    /// </summary>
    /// <param name="folder">The inventory folder.</param>
    public void Initialize(InventoryFolder folder)
    {
        this.folderNameText.text = folder.Name;

        foreach (Transform child in this.itemContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (InventoryItem item in folder.Items)
        {
            GameObject itemObject = Instantiate(
                this.itemPrefab,
                this.itemContainer);

            InventoryItemUI itemUI =
                itemObject.GetComponent<InventoryItemUI>();

            itemUI.Initialize(item);
        }
    }
}