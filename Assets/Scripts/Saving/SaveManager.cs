using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Keeps track of entities to save, can save and load maps.
/// </summary>
public static class SaveManager
{
    private const int SaveVersion = 1;

    private static Dictionary<string, SaveableEntity> saveables =
        new Dictionary<string, SaveableEntity>();

    /// <summary>
    /// Registers an entity for saving and loading.
    /// </summary>
    /// <param name="entity">The entity to be registered.</param>
    public static void Register(SaveableEntity entity)
    {
        if (!saveables.ContainsKey(entity.GetUniqueID()))
        {
            saveables.Add(entity.GetUniqueID(), entity);
        }
    }

    /// <summary>
    /// Gets an entity by their ID.
    /// </summary>
    /// <param name="id">The id of the entity to find.</param>
    /// <returns>The found SaveableEntity.</returns>
    public static SaveableEntity GetEntityByID(string id)
    {
        saveables.TryGetValue(id, out var entity);
        return entity;
    }

    /// <summary>
    /// Saves this map to it's own file.
    /// </summary>
    /// <param name="path">The path to save this map to.
    /// Should be /[username]/[mapName]</param>
    public static void SaveMap(string path)
    {
        path = Application.persistentDataPath + path;

        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(SaveVersion);
        writer.Write(saveables.Count);

        foreach (var entity in saveables.Values)
        {
            writer.Write(entity.GetUniqueID());
            entity.Write(writer);
        }

        Debug.Log($"Saved {saveables.Count} entities.");
    }

    /// <summary>
    /// Loads this map into the current scene.
    /// </summary>
    /// <param name="path">The path to load from. Usually /[username]/[mapName]</param>
    public static void LoadMap(string path)
    {
        path = Application.persistentDataPath + path;
        if (!File.Exists(path))
        {
            Debug.LogWarning("Save file not found.");
            return;
        }

        using FileStream stream = File.Open(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(stream);

        int version = reader.ReadInt32();
        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            string id = reader.ReadString();

            if (saveables.TryGetValue(id, out var entity))
            {
                entity.Read(reader);
            }
            else
            {
                Debug.LogWarning($"Entity with ID {id} not found during load.");
            }
        }

        Debug.Log($"Loaded {count} entities.");
    }
}