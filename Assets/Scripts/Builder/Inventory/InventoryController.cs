using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Builds and controls the inventory UI from the folders found in the resources folder and the tilelibraries.
/// </summary>
public class InventoryController : MonoBehaviour
{
    [SerializeField]
    private Sprite testA;
    [SerializeField]
    private Sprite testB;

    [SerializeField]
    private Transform folderContainer;

    [SerializeField]
    private GameObject folderPrefab;

    [SerializeField]
    private Transform itemContainer;

    [SerializeField]
    private GameObject itemPrefab;

    [SerializeField]
    private GameObject inventory;

    [SerializeField]
    private ItemBarSlot[] itemSlots;

    private GameObject selectedInventoryItem;
    private GameObject selectedToolbarItem;

    private List<InventoryFolder> folders = new ();
    private bool isOpen;

    /// <summary>
    /// Gets all inventory folders.
    /// </summary>
    public List<InventoryFolder> Folders => this.folders;

    /// <summary>
    /// Opens the inventories UI.
    /// </summary>
    public void OpenInventory()
    {
        if (this.isOpen)
        {
            this.CloseInventory();
            return;
        }

        this.isOpen = true;
        this.inventory.gameObject.SetActive(true);
        Time.timeScale = 0;
    }

    /// <summary>
    /// Opens the inventories UI.
    /// </summary>
    public void CloseInventory()
    {
        this.isOpen = false;
        this.inventory.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    /// <summary>
    /// Selects the given slot.
    /// </summary>
    /// <param name="index">The index of the slot to select.</param>
    public void SelectSlot(int index)
    {
        index--; // To work with the arrays.

        if (index < 0 || index >= this.itemSlots.Length)
        {
            return;
        }

        // If we have an inventory item selected, assign it to this slot.
        if (this.selectedInventoryItem != null)
        {
            InventoryItemUI inventoryItemUI =
                this.selectedInventoryItem.GetComponent<InventoryItemUI>();

            if (inventoryItemUI == null || inventoryItemUI.Item == null)
            {
                return;
            }

            RectTransform itemRect =
                this.selectedInventoryItem.GetComponent<RectTransform>();

            Vector3 screenPos = itemRect != null
                ? RectTransformUtility.WorldToScreenPoint(null, itemRect.position)
                : Vector3.zero;

            this.itemSlots[index].SetItem(
                inventoryItemUI.Item,
                screenPos);

            this.selectedInventoryItem = null;
            return;
        }

        // If we don't have an inventory item selected, select the item
        // that is already in this toolbar slot.
        InventoryItem item = this.itemSlots[index].Item;

        if (item == null)
        {
            return;
        }

        this.selectedToolbarItem = this.itemSlots[index].gameObject;

        // If the inventory is closed, select/use the item for placement.
        if (!this.isOpen)
        {
            // TODO: Select the item's prefab/tile and switch layers.
        }
    }

    /// <summary>
    /// Replaces the current inventory with the supplied folders.
    /// </summary>
    /// <param name="newFolders">The folders to use.</param>
    public void SetFolders(List<InventoryFolder> newFolders)
    {
        this.Folders.Clear();
        this.Folders.AddRange(newFolders);
    }

    /// <summary>
    /// Adds a new inventory folder to the existing list.
    /// </summary>
    /// <param name="folder">The folder to add.</param>
    public void AddFolder(InventoryFolder folder)
    {
        this.Folders.Add(folder);
    }

    /// <summary>
    /// Builds the complete inventory UI.
    /// </summary>
    public void BuildUI()
    {
        this.ClearFolderContainer();
        this.ClearItemContainer();

        foreach (InventoryFolder folder in this.Folders)
        {
            GameObject folderObject = Instantiate(
                this.folderPrefab,
                this.folderContainer);

            InventoryFolderUI folderUI =
                folderObject.GetComponent<InventoryFolderUI>();

            if (folderUI == null)
            {
                continue;
            }

            folderUI.Initialize(folder, this);
        }

        if (this.Folders.Count > 0)
        {
            this.ShowFolder(this.Folders[0]);
        }
    }

    /// <summary>
    /// Displays all items belonging to the selected folder
    /// in the shared item container.
    /// </summary>
    /// <param name="folder">The folder whose items should be displayed.</param>
    public void ShowFolder(InventoryFolder folder)
    {
        this.ClearItemContainer();

        foreach (InventoryItem item in folder.Items)
        {
            GameObject itemObject = Instantiate(
                this.itemPrefab,
                this.itemContainer);

            InventoryItemUI itemUI =
                itemObject.GetComponent<InventoryItemUI>();

            if (itemUI == null)
            {
                continue;
            }

            itemUI.Initialize(item);
        }
    }

    /// <summary>
    /// Removes all folder UI elements.
    /// </summary>
    private void ClearFolderContainer()
    {
        foreach (Transform child in this.folderContainer)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Removes all item UI elements.
    /// </summary>
    private void ClearItemContainer()
    {
        foreach (Transform child in this.itemContainer)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Adds all folders found in the files.
    /// </summary>
    private void ScanFolders()
    {
        string prefabsPath = Path.Combine(Application.dataPath, "Resources/prefabs/SaveableEntities");
        if (!Directory.Exists(prefabsPath))
        {
            Debug.LogWarning($"No prefabs folder found at {prefabsPath}.");
            return;
        }

        foreach (string folderPath in Directory.GetDirectories(prefabsPath))
        {
            string folderName = Path.GetFileName(folderPath);
            Object[] loadedAssets = Resources.LoadAll("prefabs/SaveableEntities/" + folderName);

            Sprite folderSprite = null;
            List<InventoryItem> items = new ();

            foreach (Object asset in loadedAssets)
            {
                if (asset is Sprite sprite)
                {
                    folderSprite ??= sprite;
                    continue;
                }

                if (asset is GameObject prefab && prefab.TryGetComponent(out SaveableEntity saveableEntity))
                {
                    items.Add(saveableEntity.GetAsInventoryItem());
                }
            }

            InventoryFolder folder = new (folderName, folderSprite);
            folder.Items.AddRange(items);
            this.AddFolder(folder);
        }
    }

    private void Start()
    {
        this.ScanFolders();
        this.BuildUI();
        this.CloseInventory();
    }
}