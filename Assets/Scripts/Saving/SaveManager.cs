using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SaveManager
{
    private static Dictionary<string, SaveableEntity> saveables =
        new Dictionary<string, SaveableEntity>();

    public static void Register(SaveableEntity entity)
    {
        if (!saveables.ContainsKey(entity.GetUniqueID()))
        {
            saveables.Add(entity.GetUniqueID(), entity);
            Debug.Log($"[SaveManager] Registered entity {entity.name} ({entity.GetType().Name})");
        }
    }

    public static void SaveGame(string path)
    {
        var state = new Dictionary<string, Dictionary<string, object>>();

        foreach (var entity in saveables.Values)
        {
            var captured = entity.CaptureState();
            state[entity.GetUniqueID()] = captured;
            Debug.Log($"[SaveManager] Saved entity {entity.name} ({captured.Count} fields)");
        }

        BinaryFormatter bf = new BinaryFormatter();
        using FileStream file = File.Create(path);
        bf.Serialize(file, state);

        Debug.Log($"[SaveManager] Saved {saveables.Count} entities to {path}");
    }

    public static void LoadGame(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] Save file not found: {path}");
            return;
        }

        BinaryFormatter bf = new BinaryFormatter();
        using FileStream file = File.Open(path, FileMode.Open);
        var state = (Dictionary<string, Dictionary<string, object>>)bf.Deserialize(file);

        foreach (var entity in saveables.Values)
        {
            if (state.TryGetValue(entity.GetUniqueID(), out var savedState))
            {
                entity.RestoreState(savedState);
                Debug.Log($"[SaveManager] Loaded entity {entity.name}");
            }
        }

        Debug.Log($"[SaveManager] Loaded {saveables.Count} entities from {path}");
    }

    public static SaveableEntity GetEntityByID(string id)
    {
        if (saveables.TryGetValue(id, out var entity))
            return entity;

        Debug.LogWarning($"[SaveManager] No entity found with ID {id}");
        return null;
    }
}