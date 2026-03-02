using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles saving and loading maps.
/// </summary>
public static class SaveManager
{
    // All registered saveable objects in this map.
    private static Dictionary<string, SaveableEntity> saveables =
        new Dictionary<string, SaveableEntity>();

    /// <summary>
    /// Register a saveable object.
    /// Called automatically by SaveableEntity.
    /// </summary>
    /// <param name="entity">The entity to register into the dictionary.</param>
    public static void Register(SaveableEntity entity)
    {
        if (!saveables.ContainsKey(entity.GetUniqueID()))
        {
            saveables.Add(entity.GetUniqueID(), entity);
        }
        else
        {
            Debug.Log("This entity has already been registered.");
        }
    }

    /// <summary>
    /// Save all registered objects to a JSON file.
    /// </summary>
    /// <param name="filePath">The file path to save the game to.</param>
    public static void SaveGame(string filePath)
    {
        Dictionary<string, object> state = new Dictionary<string, object>();

        foreach (var entity in saveables.Values)
        {
            state[entity.GetUniqueID()] = entity.CaptureState();
        }

        // Wrap in helper to serialize dictionary
        string json = JsonUtility.ToJson(new SerializableDictionary(state), true);
        File.WriteAllText(filePath, json);
        Debug.Log($"Game saved to {filePath}");
    }

    /// <summary>
    /// Load all registered objects from a JSON file.
    /// </summary>
    /// <param name="filePath"> The file path to load the game from.</param>
    public static void LoadGame(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Save file not found: " + filePath);
            return;
        }

        string json = File.ReadAllText(filePath);
        var wrapper = JsonUtility.FromJson<SerializableDictionary>(json);
        var state = wrapper.ToDictionary();

        foreach (var entity in saveables.Values)
        {
            if (state.TryGetValue(entity.GetUniqueID(), out object savedState))
            {
                entity.RestoreState(savedState);
            }
        }

        Debug.Log("Game loaded from " + filePath);
    }

    /// <summary>
    /// Finds an entity by it's id.
    /// </summary>
    /// <param name="id">The id to find it by.</param>
    /// <returns>Returns the SaveableEntity.</returns>
    public static SaveableEntity GetEntityByID(string id)
    {
        if (saveables.TryGetValue(id, out SaveableEntity entity))
        {
            return entity;
        }

        Debug.LogWarning($"SaveManager: No entity found with ID {id}");
        return null;
    }
}