using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Save Registry keeps track of all entity base IDs and their prefabs.
/// </summary>
public static class SaveRegistry
{
    [SerializeField]
    private static Dictionary<string, GameObject> prefabs = new ();

    private static bool isInitialized = false;

    /// <summary>
    /// Gets a value indicating whether the resources have been loaded into the registry.
    /// </summary>
    public static bool IsInitialized => isInitialized;

    /// <summary>
    /// Gets an entity base prefab based on id.
    /// </summary>
    /// <param name="id">The id to get an entity base for.</param>
    /// <returns>The prefab.</returns>
    public static GameObject GetPrefab(string id)
    {
        prefabs.TryGetValue(id, out var prefab);
        Debug.Log(prefab);
        return prefab;
    }

    /// <summary>
    /// Gets all entries from the Assets/Resources/Prefabs/SaveableEntities folder.
    /// </summary>
    public static void InitializeRegistryFromResources()
    {
        isInitialized = true;
        prefabs.Clear();
        PrefabIdentity[] allPrefabs = Resources.LoadAll<PrefabIdentity>("Prefabs/SaveableEntities");
        foreach (var identity in allPrefabs)
        {
            prefabs[identity.PrefabID] = identity.gameObject;
            Debug.Log("Loaded " + identity.PrefabID + " into the registry.");
        }

        Debug.Log("Loaded " + prefabs.Count + " entities into the registry.");
    }
}