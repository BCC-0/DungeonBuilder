using UnityEngine;

/// <summary>
/// Builds the inventory UI from the folders provided by the InventoryManager.
/// </summary>
public class InventoryBuilderUI : MonoBehaviour
{
    [SerializeField]
    private InventoryManager inventoryManager;

    [SerializeField]
    private Transform folderContainer;

    [SerializeField]
    private GameObject folderPrefab;

    [SerializeField]
    private GameObject itemPrefab;

    /// <summary>
    /// Builds the complete inventory UI.
    /// </summary>
    public void BuildUI()
    {
        foreach (Transform child in this.folderContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (InventoryFolder folder in this.inventoryManager.Folders)
        {
            GameObject folderObject = Instantiate(
                this.folderPrefab,
                this.folderContainer);

            // Your folder UI script would receive the folder data here.
            // Example:
            // folderObject.GetComponent<InventoryFolderUI>().Initialize(folder);

            foreach (InventoryItem item in folder.Items)
            {
                GameObject itemObject = Instantiate(
                    this.itemPrefab,
                    folderObject.transform);

                InventoryItemUI itemUI =
                    itemObject.GetComponent<InventoryItemUI>();

                itemUI.Initialize(item);
            }
        }
    }
    private void Start()
    {
        this.BuildUI();
    }
}