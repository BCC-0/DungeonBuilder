using UnityEngine;

/// <summary>
/// Finds all folders from assets/resources/saveablentities and the tilelibrary
/// and adds them to <see cref="InventoryManager"/>.
/// </summary>
public class InventoryFolderScanner : MonoBehaviour
{
    private void Start()
    {
        // Currently only adds some test folders.
        InventoryFolder a = new InventoryFolder("A");
        InventoryFolder b = new InventoryFolder("B");
    }
}
