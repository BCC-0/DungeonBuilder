using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Builds and controls the inventory UI from the folders found in the resources folder and the tilelibraries.
/// </summary>
public class InventoryController : MonoBehaviour
{
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

    [SerializeField]
    private RectTransform selectionImage;

    [SerializeField]
    private float selectionScale = 1.2f;

    private InventoryItemUI selectedInventoryItem;

    private int selectedSlot = -1;
    private int lastSelectedSlot = -1;

    private List<InventoryFolder> folders = new ();

    private bool isOpen;

    private Transform selectionImageParent;

    private EditorControllerBase lastController;

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
        this.inventory.SetActive(true);
        Time.timeScale = 0;
        this.lastSelectedSlot = this.selectedSlot;
        this.selectedSlot = -1;
        this.HideSelectionImage();
    }

    /// <summary>
    /// Closes the inventories UI.
    ///
    /// Note: this intentionally does NOT clear the equipped toolbar slot -
    /// a slot must always remain selected/equipped while the inventory is closed.
    /// </summary>
    public void CloseInventory()
    {
        this.isOpen = false;
        this.inventory.SetActive(false);
        Time.timeScale = 1;

        this.ClearItemSelection();
        this.SelectSlot(this.lastSelectedSlot);
        this.lastSelectedSlot = -1;
    }

    /// <summary>
    /// Selects the given inventory item.
    ///
    /// If a slot is already selected, the item is immediately assigned
    /// to that slot. Otherwise the item remains selected until a slot is clicked.
    /// </summary>
    /// <param name="itemUI">The inventory item UI that was clicked.</param>
    public void SelectItem(InventoryItemUI itemUI)
    {
        if (itemUI == null || itemUI.Item == null)
        {
            return;
        }

        // A slot is already selected.
        // Assign this item directly to that slot.
        if (this.selectedSlot >= 0)
        {
            this.AssignItemToSlot(
                itemUI.Item,
                this.selectedSlot,
                itemUI);

            return;
        }

        // Otherwise select the inventory item.
        this.selectedInventoryItem = itemUI;

        this.SetSelectionImage(itemUI.gameObject);
    }

    /// <summary>
    /// Selects the given toolbar slot, equipping whatever item currently
    /// lives in it (switching layers and assigning it to the relevant
    /// editor controller).
    ///
    /// If an inventory item is already selected, the item is immediately
    /// assigned to this slot instead. Otherwise the slot itself becomes
    /// the new equipped slot.
    /// </summary>
    /// <param name="index">The zero-based index of the slot.</param>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= this.itemSlots.Length)
        {
            return;
        }

        if (this.isOpen && this.selectedSlot >= 0 && this.selectedSlot != index)
        {
            this.SwapSlotItems(this.selectedSlot, index);

            this.EquipItem(this.itemSlots[index].Item);

            this.selectedSlot = -1;
            this.HideSelectionImage();

            return;
        }

        // An inventory item is already selected.
        // Assign it directly to this slot.
        if (this.selectedInventoryItem != null)
        {
            this.AssignItemToSlot(
                this.selectedInventoryItem.Item,
                index,
                this.selectedInventoryItem);

            return;
        }

        this.selectedSlot = index;

        this.SetSelectionImage(this.itemSlots[index].gameObject);

        this.EquipItem(this.itemSlots[index].Item);
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
        if (folder == null)
        {
            return;
        }

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

            itemUI.Initialize(item, this);
        }
    }

    /// <summary>
    /// Swaps the items between two toolbar slots.
    /// </summary>
    /// <param name="firstIndex">The first slot index.</param>
    /// <param name="secondIndex">The second slot index.</param>
    private void SwapSlotItems(int firstIndex, int secondIndex)
    {
        if (firstIndex < 0 ||
            firstIndex >= this.itemSlots.Length ||
            secondIndex < 0 ||
            secondIndex >= this.itemSlots.Length ||
            firstIndex == secondIndex)
        {
            return;
        }

        ItemBarSlot firstSlot = this.itemSlots[firstIndex];
        ItemBarSlot secondSlot = this.itemSlots[secondIndex];

        RectTransform firstImageRect = firstSlot.ItemImageRect;
        RectTransform secondImageRect = secondSlot.ItemImageRect;

        if (firstImageRect == null || secondImageRect == null)
        {
            return;
        }

        InventoryItem firstItem = firstSlot.Item;
        InventoryItem secondItem = secondSlot.Item;

        Vector2 firstInventorySize = firstSlot.InventorySize;
        Vector2 secondInventorySize = secondSlot.InventorySize;

        Vector3 firstScreenPos = RectTransformUtility.WorldToScreenPoint(null, firstImageRect.position);
        Vector3 secondScreenPos = RectTransformUtility.WorldToScreenPoint(null, secondImageRect.position);

        firstSlot.SetItem(secondItem, secondScreenPos, secondInventorySize);
        secondSlot.SetItem(firstItem, firstScreenPos, firstInventorySize);
    }

    /// <summary>
    /// Assigns an inventory item to a toolbar slot, then equips it.
    /// </summary>
    /// <param name="item">The item to assign.</param>
    /// <param name="slotIndex">The target slot index.</param>
    /// <param name="sourceUI">The UI element the item came from.</param>
    private void AssignItemToSlot(
            InventoryItem item,
            int slotIndex,
            InventoryItemUI sourceUI)
    {
        if (item == null ||
            slotIndex < 0 ||
            slotIndex >= this.itemSlots.Length ||
            sourceUI == null)
        {
            return;
        }

        RectTransform itemRect = sourceUI.GetComponent<RectTransform>();

        if (itemRect == null)
        {
            return;
        }

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(
                null,
                itemRect.position);

        Vector2 inventorySize = itemRect.rect.size;

        this.itemSlots[slotIndex].SetItem(item, screenPos, inventorySize);

        this.selectedSlot = -1;
        this.ClearItemSelection();

        this.EquipItem(item);
    }

    /// <summary>
    /// Detects whether the item is a tile or an entity, switches the map
    /// editor to the matching layer, and assigns the item to that layer's
    /// controller as the currently selected tile/prefab.
    /// </summary>
    /// <param name="item">The item being equipped. May be null (empty slot).</param>
    private void EquipItem(InventoryItem item)
    {
        if (item == null || MapEditorManager.Instance == null)
        {
            return;
        }

        if (item.IsTile)
        {
            MapEditorManager.Instance.SetLayer(EditLayer.Background);

            if (MapEditorManager.Instance.ActiveController
                is TileEditorController tileController)
            {
                tileController.SelectedTile = item.Tile;

                if (this.lastController is EntityEditorController entityController)
                {
                    entityController.SelectedPrefab = null;
                }

                this.lastController = tileController;
            }
        }
        else
        {
            MapEditorManager.Instance.SetLayer(EditLayer.Foreground);

            if (MapEditorManager.Instance.ActiveController
                is EntityEditorController entityController)
            {
                entityController.SelectedPrefab = item.Prefab;

                if (this.lastController is TileEditorController tileController)
                {
                    tileController.SelectedTile = null;
                }

                this.lastController = entityController;
            }
        }
    }

    /// <summary>
    /// Moves the selection image to the supplied UI object.
    /// </summary>
    /// <param name="target">The UI object to select.</param>
    private void SetSelectionImage(GameObject target)
    {
        if (this.selectionImage == null)
        {
            return;
        }

        if (target == null)
        {
            this.HideSelectionImage();
            return;
        }

        RectTransform targetRect = target.GetComponent<RectTransform>();

        if (targetRect == null)
        {
            return;
        }

        Vector2 targetSize = targetRect.rect.size;
        bool isSlot = target.TryGetComponent<ItemBarSlot>(out _);
        Vector2 selectionSize;

        if (isSlot)
        {
            selectionSize = targetSize;
        }
        else
        {
            float largestDimension = Mathf.Max(targetSize.x, targetSize.y);
            float selectionDimension = largestDimension * this.selectionScale;
            selectionSize = new Vector2(selectionDimension, selectionDimension);
        }

        this.selectionImage.SetParent(
            targetRect,
            false);

        this.selectionImage.gameObject.SetActive(true);

        this.selectionImage.sizeDelta =
            selectionSize;
    }

    /// <summary>
    /// Clears only the pending inventory-item pick.
    /// </summary>
    private void ClearItemSelection()
    {
        if (this.selectedInventoryItem == null)
        {
            return;
        }

        this.selectedInventoryItem = null;

        if (this.selectedSlot >= 0)
        {
            this.SetSelectionImage(this.itemSlots[this.selectedSlot].gameObject);
        }
        else
        {
            this.HideSelectionImage();
        }
    }

    /// <summary>
    /// Hides the selection image and restores it to its permanent parent.
    /// </summary>
    private void HideSelectionImage()
    {
        if (this.selectionImage == null)
        {
            return;
        }

        this.selectionImage.gameObject.SetActive(false);

        if (this.selectionImageParent != null)
        {
            this.selectionImage.SetParent(
                this.selectionImageParent,
                false);
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
        this.ClearItemSelection();

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
        string prefabsPath = Path.Combine(
            Application.dataPath,
            "Resources/prefabs/SaveableEntities");

        if (!Directory.Exists(prefabsPath))
        {
            Debug.LogWarning(
                $"No prefabs folder found at {prefabsPath}.");

            return;
        }

        foreach (string folderPath in Directory.GetDirectories(prefabsPath))
        {
            string folderName =
                Path.GetFileName(folderPath);

            Object[] loadedAssets =
                Resources.LoadAll(
                    "prefabs/SaveableEntities/" + folderName);

            Sprite folderSprite = null;
            List<InventoryItem> items = new();

            foreach (Object asset in loadedAssets)
            {
                if (asset is Sprite sprite)
                {
                    folderSprite ??= sprite;
                    continue;
                }

                if (asset is TileLibrary tileLibrary)
                {
                    items.AddRange(
                        tileLibrary.GetInventoryItems());

                    continue;
                }

                if (asset is GameObject prefab &&
                    prefab.TryGetComponent(
                        out SaveableEntity saveableEntity))
                {
                    items.Add(
                        saveableEntity.GetAsInventoryItem());
                }
            }

            InventoryFolder folder =
                new InventoryFolder(
                    folderName,
                    folderSprite);

            folder.Items.AddRange(items);

            this.AddFolder(folder);
        }
    }

    private void Start()
    {
        if (this.selectionImage != null)
        {
            this.selectionImageParent =
                this.selectionImage.parent;
        }

        this.ScanFolders();
        this.BuildUI();
        this.CloseInventory();

        // A toolbar slot must always be equipped, even before the player
        // has ever opened the inventory or touched the toolbar.
        if (this.itemSlots.Length > 0)
        {
            this.SelectSlot(0);
        }
    }
}