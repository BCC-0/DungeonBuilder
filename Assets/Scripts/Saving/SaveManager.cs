using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Keeps track of entities to save, can save and load maps.
/// </summary>
public static class SaveManager
{
    private const int SaveVersion = 2;

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
    /// DO NOT USE!
    /// Saves this map to it's own file using raw entities.
    /// </summary>
    /// <param name="path">The path to save this map to.
    /// Format: /[username]/[mapName]</param>
    public static void SaveMap(string path)
    {
        path = Application.persistentDataPath + "/" + path;

        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(SaveVersion);
        writer.Write(saveables.Count);

        foreach (var entity in saveables.Values)
        {
            writer.Write(entity.GetUniqueID());
            writer.Write(entity.GetPrefabID());
            entity.Write(writer);
        }

        Debug.Log($"Saved {saveables.Count} entities (player mode).");
    }

    /// <summary>
    /// Loads this map into the current scene.
    /// </summary>
    /// <param name="path">The path to load from. Format: /[username]/[mapName]</param>
    public static void LoadMap(string path)
    {
        // First initialize the registry if it hasn't been yet.
        if (!SaveRegistry.IsInitialized)
        {
            SaveRegistry.InitializeRegistryFromResources();
        }

        path = Application.persistentDataPath + "/" + path;
        if (!File.Exists(path))
        {
            Debug.LogWarning("Save file not found.");
            return;
        }

        using FileStream stream = File.Open(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(stream);

        int version = reader.ReadInt32();
        int count = reader.ReadInt32();

        // Don't clear, otherwise we remove the tilemap.
        // saveables.Clear();
        for (int i = 0; i < count; i++)
        {
            string id = reader.ReadString();

            string prefabID = reader.ReadString();

            // The Tilemap already exists in the scene.
            SaveableEntity existingSceneEntity = SaveManager.GetEntityByID(id);

            if (existingSceneEntity != null && existingSceneEntity is SaveableTilemap)
            {
                existingSceneEntity.Read(reader);
                continue;
            }

            GameObject prefab = SaveRegistry.GetPrefab(prefabID);
            if (prefab != null)
            {
                GameObject obj = GameObject.Instantiate(prefab);
                SaveableEntity entity = obj.GetComponent<SaveableEntity>();
                if (entity == null)
                {
                    Debug.LogWarning($"Prefab {prefabID} does not contain a SaveableEntity component.");
                    continue;
                }

                typeof(SaveableEntity)
                    .GetField("uniqueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(entity, id);

                entity.Read(reader);
                Register(entity);
            }
            else
            {
                Debug.LogWarning($"Prefab {prefabID} not found for entity {id}.");
            }
        }

        Debug.Log($"Loaded {count} entities. (player mode).");
    }

    /// <summary>
    /// Saves the map from the builder.
    /// </summary>
    /// <param name="path">The path to save to. Format: /[username]/[mapName]</param>
    public static void SaveBuilderMap(string path)
    {
        path = Application.persistentDataPath + "/" + path;
        var builders = BuilderRegistry.GetAll();
        var tilemaps = saveables.Values.OfType<SaveableTilemap>().ToList();

        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(SaveVersion);
        writer.Write(builders.Count + tilemaps.Count);

        foreach (var tilemap in tilemaps)
        {
            writer.Write(tilemap.GetUniqueID());
            writer.Write(tilemap.GetPrefabID());
            tilemap.Write(writer);
        }

        foreach (var builder in builders)
        {
            writer.Write(builder.GetUniqueID());
            writer.Write(builder.PrefabID);
            builder.Write(writer);
        }

        Debug.Log($"Saved {builders.Count} builder entities and {tilemaps.Count} tilemaps.");
    }

    /// <summary>
    /// Loads the map into the builder using BuilderEntities to omit unneeded behaviour.
    /// </summary>
    /// <param name="path">The path to load from. Format: /[username]/[mapName]</param>
    public static void LoadBuilderMap(string path)
    {
        path = Application.persistentDataPath + "/" + path;
        if (!File.Exists(path))
        {
            Debug.LogWarning("Builder save file not found.");
            return;
        }

        // Clear existing builder entities
        foreach (var b in BuilderRegistry.GetAll())
        {
            Object.DestroyImmediate(b.gameObject);
        }

        using FileStream stream = File.Open(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(stream);

        int version = reader.ReadInt32();
        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            string id = reader.ReadString();
            string prefabID = version >= 2 ? reader.ReadString() : "DefaultPrefab";

            SaveableEntity existingSceneEntity = SaveManager.GetEntityByID(id);
            if (existingSceneEntity != null && existingSceneEntity is SaveableTilemap)
            {
                existingSceneEntity.Read(reader);
                continue;
            }

            GameObject go = new GameObject("BuilderEntity");
            var builder = go.AddComponent<BuilderEntity>();

            BuilderRegistry.Unregister(builder);

            typeof(SaveableEntity)
                .GetField("uniqueID", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(builder, id);

            builder.Read(reader);
            BuilderRegistry.Register(builder);
        }

        Debug.Log($"Loaded {count} builder entities.");
    }
}