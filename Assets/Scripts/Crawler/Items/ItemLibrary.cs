using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of all items in the game.
/// </summary>
[CreateAssetMenu(fileName = "ItemLibrary", menuName = "Items/ItemLibrary")]
public class ItemLibrary : ScriptableObject
{
    private static List<ItemLibrary> allLibraries;

    [SerializeField]
    private List<Item> items = new ();

    /// <summary>
    /// Gets the items found in this library.
    /// </summary>
    public IReadOnlyList<Item> Items => this.items;

    /// <summary>
    /// Gets an item by ID from all libraries.
    /// </summary>
    /// <param name="itemID">The id to get the item for.</param>
    /// <returns>The found item, or null if not found.</returns>
    public static Item GetItemByIDGlobal(string itemID)
    {
        LoadAllLibraries();

        foreach (ItemLibrary library in allLibraries)
        {
            foreach (Item item in library.items)
            {
                if (item != null && item.ItemID == itemID)
                {
                    return item;
                }
            }
        }

        return null;
    }

    private static void LoadAllLibraries()
    {
        if (allLibraries != null)
        {
            return;
        }

        allLibraries = new List<ItemLibrary>(Resources.LoadAll<ItemLibrary>(string.Empty));
    }
}