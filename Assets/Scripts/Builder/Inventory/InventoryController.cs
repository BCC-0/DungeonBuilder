using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField]
    private string[] folderNames;

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
    /// Opens the inventory UI.
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
    /// Closes the inventory UI.
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
    /// </summary>
    /// <param name="itemUI">The inventory item UI element that was selected.</param>
    public void SelectItem(InventoryItemUI itemUI)
    {
        if (itemUI == null || itemUI.Item == null)
        {
            return;
        }

        if (this.selectedInventoryItem == itemUI)
        {
            this.ClearItemSelection();
            return;
        }

        if (this.selectedSlot >= 0)
        {
            this.AssignItemToSlot(
                itemUI.Item,
                this.selectedSlot,
                itemUI);

            return;
        }

        this.selectedInventoryItem = itemUI;

        this.SetSelectionImage(itemUI.gameObject);
    }

    /// <summary>
    /// Selects the given toolbar slot, equipping its item.
    /// </summary>
    /// <param name="index">The index of the toolbar slot to select.</param>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= this.itemSlots.Length)
        {
            return;
        }

        if (this.isOpen &&
            this.selectedSlot == index &&
            this.selectedInventoryItem == null)
        {
            this.selectedSlot = -1;
            this.HideSelectionImage();
            return;
        }

        if (this.isOpen &&
            this.selectedSlot >= 0 &&
            this.selectedSlot != index)
        {
            this.SwapSlotItems(this.selectedSlot, index);

            this.EquipItem(this.itemSlots[index].Item);

            this.selectedSlot = -1;
            this.HideSelectionImage();

            return;
        }

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
    /// Captures the items currently assigned to the toolbar slots.
    /// Used when entering playtest mode.
    /// </summary>
    /// <returns>The item of each toolbar slot, in slot order. Empty slots are null.</returns>
    public InventoryItem[] GetSlotItems()
    {
        InventoryItem[] items =
            new InventoryItem[this.itemSlots.Length];

        for (int i = 0; i < this.itemSlots.Length; i++)
        {
            items[i] = this.itemSlots[i].Item;
        }

        return items;
    }

    /// <summary>
    /// Captures the toolbar inventory sizes.
    /// These are used to restore item icons correctly.
    /// </summary>
    /// <returns>The inventory size of each toolbar slot, in slot order.</returns>
    public Vector2[] GetSlotInventorySizes()
    {
        Vector2[] sizes = new Vector2[this.itemSlots.Length];

        for (int i = 0; i < this.itemSlots.Length; i++)
        {
            sizes[i] = this.itemSlots[i].InventorySize;
        }

        return sizes;
    }

    /// <summary>
    /// Gets the selected toolbar slot, including the previously selected
    /// slot if the inventory is currently open.
    /// </summary>
    /// <returns>The selected slot index, or -1 if no slot is selected.</returns>
    public int GetSelectedSlot()
    {
        return this.selectedSlot >= 0
            ? this.selectedSlot
            : this.lastSelectedSlot;
    }

    /// <summary>
    /// Restores the toolbar contents and selected slot after a scene change.
    /// </summary>
    /// <param name="items">The items to place in the toolbar slots, in slot order. Null entries leave the slot empty.</param>
    /// <param name="inventorySizes">The inventory size of each slot, in slot order. May be null.</param>
    /// <param name="selectedSlotIndex">The slot to select afterwards. Negative values select nothing.</param>
    public void RestoreSlotItems(
        InventoryItem[] items,
        Vector2[] inventorySizes,
        int selectedSlotIndex)
    {
        if (items == null)
        {
            return;
        }

        this.RebuildSlotLayouts();

        int count = Mathf.Min(
            items.Length,
            this.itemSlots.Length);

        for (int i = 0; i < count; i++)
        {
            InventoryItem item = items[i];

            // Empty slots do not need to be reconstructed.
            if (item == null)
            {
                continue;
            }

            ItemBarSlot slot = this.itemSlots[i];

            RectTransform imageRect = slot.ItemImageRect;

            if (imageRect == null)
            {
                imageRect = slot.GetComponent<RectTransform>();
            }

            if (imageRect == null)
            {
                continue;
            }

            Vector3 screenPosition = GetScreenPoint(imageRect);

            Vector2 inventorySize = slot.InventorySize;

            if (inventorySizes != null &&
                i < inventorySizes.Length)
            {
                inventorySize = inventorySizes[i];
            }

            slot.SetItem(
                item,
                screenPosition,
                inventorySize);
        }

        // Clear any temporary selection left by initialization.
        this.selectedInventoryItem = null;
        this.selectedSlot = -1;
        this.lastSelectedSlot = -1;
        this.HideSelectionImage();

        // Re-select and equip the slot that was active before playtesting.
        if (selectedSlotIndex >= 0 &&
            selectedSlotIndex < this.itemSlots.Length)
        {
            this.SelectSlot(selectedSlotIndex);
        }
    }

    /// <summary>
    /// Adds a new inventory folder.
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
    /// Displays all items belonging to the selected folder.
    /// </summary>
    /// <param name="folder">The folder whose items are shown.</param>
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
    /// <param name="firstIndex">The index of the first slot.</param>
    /// <param name="secondIndex">The index of the second slot.</param>
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

        Vector3 firstScreenPos = GetScreenPoint(firstImageRect);

        Vector3 secondScreenPos = GetScreenPoint(secondImageRect);

        firstSlot.SetItem(
            secondItem,
            secondScreenPos,
            secondInventorySize);

        secondSlot.SetItem(
            firstItem,
            firstScreenPos,
            firstInventorySize);
    }

    /// <summary>
    /// Assigns an inventory item to a toolbar slot, then equips it.
    /// </summary>
    /// <param name="item">The item to assign.</param>
    /// <param name="slotIndex">The index of the target toolbar slot.</param>
    /// <param name="sourceUI">The inventory item UI element the item came from.</param>
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

        RectTransform itemRect =
            sourceUI.GetComponent<RectTransform>();

        if (itemRect == null)
        {
            return;
        }

        Vector3 screenPos = GetScreenPoint(itemRect);

        Vector2 inventorySize = itemRect.rect.size;

        this.itemSlots[slotIndex].SetItem(
            item,
            screenPos,
            inventorySize);

        this.selectedSlot = -1;
        this.ClearItemSelection();

        this.EquipItem(item);
    }

    /// <summary>
    /// Equips the item and switches to the matching editor layer.
    /// </summary>
    /// <param name="item">The item to equip. Null is ignored.</param>
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
    /// <param name="target">The UI object to highlight. Null hides the selection image.</param>
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

        RectTransform targetRect =
            target.GetComponent<RectTransform>();

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
            float largestDimension =
                Mathf.Max(targetSize.x, targetSize.y);

            float selectionDimension =
                largestDimension * this.selectionScale;

            selectionSize = new Vector2(
                selectionDimension,
                selectionDimension);
        }

        this.selectionImage.SetParent(targetRect, false);

        this.selectionImage.gameObject.SetActive(true);

        this.selectionImage.sizeDelta = selectionSize;
    }

    /// <summary>
    /// Clears only the pending inventory-item selection.
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
            this.SetSelectionImage(
                this.itemSlots[this.selectedSlot].gameObject);
        }
        else
        {
            this.HideSelectionImage();
        }
    }

    /// <summary>
    /// Hides the selection image and restores its permanent parent.
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
    /// Adds all folders found in Resources.
    /// </summary>
    private void ScanFolders()
    {
        if (this.folderNames == null ||
            this.folderNames.Length == 0)
        {
            Debug.LogWarning(
                "No inventory folder names have been assigned.");

            return;
        }

        foreach (string folderName in this.folderNames)
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                continue;
            }

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

    /// <summary>
    /// Converts the world position of a UI element to a screen point,
    /// using the camera of the canvas it belongs to.
    /// </summary>
    /// <param name="rect">The UI element to convert.</param>
    /// <returns>The screen position of the element.</returns>
    private static Vector3 GetScreenPoint(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();

        Camera cam =
            canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

        return RectTransformUtility.WorldToScreenPoint(
            cam,
            rect.position);
    }

    /// <summary>
    /// Forces a layout rebuild of every canvas that contains a toolbar slot.
    /// </summary>
    private void RebuildSlotLayouts()
    {
        Canvas.ForceUpdateCanvases();

        HashSet<Canvas> done = new();

        foreach (ItemBarSlot slot in this.itemSlots)
        {
            Canvas root = slot.GetComponentInParent<Canvas>()?.rootCanvas;

            if (root == null || !done.Add(root))
            {
                continue;
            }

            foreach (RectTransform rt in
                root.GetComponentsInChildren<RectTransform>(true))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        Canvas.ForceUpdateCanvases();
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

        // Equip the first slot by default.
        if (this.itemSlots.Length > 0)
        {
            this.SelectSlot(0);
        }
    }
}