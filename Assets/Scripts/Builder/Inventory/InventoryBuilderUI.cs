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

            InventoryFolderUI folderUI = folderObject.GetComponent<InventoryFolderUI>();

            folderUI.Initialize(folder);
        }
    }

    private void Start()
    {
        this.BuildUI();
    }
}