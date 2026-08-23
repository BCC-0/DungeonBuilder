using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds a mapping between prefab IDs and saveable entity prefabs.
/// </summary>
[CreateAssetMenu(
    fileName = "SaveableEntityLibrary",
    menuName = "Dungeon/Saveable Entity Library")]
public class SaveableEntityLibrary : ScriptableObject
{
    [SerializeField]
    private List<EntityEntry> entities = new List<EntityEntry>();

    private Dictionary<string, EntityEntry> entityMap;

    /// <summary>
    /// Gets all registered entities.
    /// </summary>
    public IReadOnlyList<EntityEntry> Entities => this.entities;

    /// <summary>
    /// Initializes the dictionary for fast lookup.
    /// </summary>
    public void Initialize()
    {
        this.entityMap = new Dictionary<string, EntityEntry>();

        foreach (EntityEntry entry in this.entities)
        {
            if (!string.IsNullOrEmpty(entry.PrefabID) &&
                entry.Prefab != null)
            {
                this.entityMap[entry.PrefabID] = entry;
            }
        }
    }

    /// <summary>
    /// Gets an entity prefab by its ID.
    /// </summary>
    public GameObject GetPrefabByID(string prefabID)
    {
        if (this.entityMap == null)
        {
            this.Initialize();
        }

        this.entityMap.TryGetValue(prefabID, out EntityEntry entry);
        return entry?.Prefab;
    }

    /// <summary>
    /// Gets the ID for a given prefab.
    /// </summary>
    public string GetIDForPrefab(GameObject prefab)
    {
        if (this.entityMap == null)
        {
            this.Initialize();
        }

        foreach (EntityEntry entry in this.entityMap.Values)
        {
            if (entry.Prefab == prefab)
            {
                return entry.PrefabID;
            }
        }

        return null;
    }

    /// <summary>
    /// Represents a single saveable entity.
    /// </summary>
    [Serializable]
    public class EntityEntry
    {
        [SerializeField]
        private string prefabID;

        [SerializeField]
        private GameObject prefab;

        /// <summary>
        /// Gets or sets the prefab ID.
        /// </summary>
        public string PrefabID
        {
            get => this.prefabID;
            set => this.prefabID = value;
        }

        /// <summary>
        /// Gets or sets the prefab.
        /// </summary>
        public GameObject Prefab
        {
            get => this.prefab;
            set => this.prefab = value;
        }
    }
}