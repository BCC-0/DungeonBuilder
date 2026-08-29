using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Builds and controls the inventory UI from the folders
/// provided by the InventoryManager.
/// </summary>
public class InventoryBuilderUI : MonoBehaviour
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

    private List<InventoryFolder> folders = new ();

    /// <summary>
    /// Gets all inventory folders.
    /// </summary>
    public List<InventoryFolder> Folders => this.folders;

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

        Debug.Log("A");
        foreach (string folderPath in Directory.GetDirectories(prefabsPath))
        {
            Debug.Log("B");
            string folderName = Path.GetFileName(folderPath);
            Object[] loadedAssets = Resources.LoadAll("prefabs/SaveableEntities/" + folderName);

            Sprite folderSprite = null;
            List<InventoryItem> items = new ();

            foreach (Object asset in loadedAssets)
            {
                Debug.Log("C");
                if (asset is Sprite sprite)
                {
                    Debug.Log("D");
                    folderSprite ??= sprite;
                    continue;
                }

                if (asset is GameObject prefab && prefab.TryGetComponent(out SaveableEntity saveableEntity))
                {
                    Debug.Log("E");
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
    }
}