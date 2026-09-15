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
    private const int SaveVersion = 3;

    private static Dictionary<string, SaveableEntity> saveables =
        new Dictionary<string, SaveableEntity>();

    /// <summary>
    /// Clears the registry.
    /// </summary>
    public static void ClearRegistry()
    {
        saveables.Clear();
    }

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
        saveables.TryGetValue(id, out SaveableEntity entity);
        return entity;
    }

    /// <summary>
    /// Writes a single entity record as: id, prefabID, payload length, payload bytes.
    /// The length prefix lets a loader skip a record safely even if it can't
    /// resolve the prefab, keeping the rest of the stream aligned.
    /// </summary>
    /// <param name="writer">The writer for the overall file.</param>
    /// <param name="id">The entity's unique ID.</param>
    /// <param name="prefabID">The entity's prefab ID.</param>
    /// <param name="entity">The entity whose Write method produces the payload.</param>
    private static void WriteEntityRecord(BinaryWriter writer, string id, string prefabID, SaveableEntity entity)
    {
        writer.Write(id);
        writer.Write(prefabID);

        using MemoryStream payloadStream = new MemoryStream();
        using (BinaryWriter payloadWriter = new BinaryWriter(payloadStream))
        {
            entity.Write(payloadWriter);
        }

        byte[] payload = payloadStream.ToArray();
        writer.Write(payload.Length);
        writer.Write(payload);
    }

    /// <summary>
    /// Reads a single entity record's header and payload bytes without
    /// interpreting them, so the caller can decide what to do with the
    /// payload (or discard it) while keeping the stream aligned.
    /// </summary>
    /// <param name="reader">The reader for the overall file.</param>
    /// <param name="id">The entity's unique ID.</param>
    /// <param name="prefabID">The entity's prefab ID.</param>
    /// <param name="payload">The raw payload bytes for this entity.</param>
    private static void ReadEntityRecord(BinaryReader reader, out string id, out string prefabID, out byte[] payload)
    {
        id = reader.ReadString();
        prefabID = reader.ReadString();

        int length = reader.ReadInt32();
        payload = reader.ReadBytes(length);
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

        List<SaveableEntity> runtimeEntities = saveables.Values
            .Where(e => !(e is BuilderEntity))
            .ToList();

        writer.Write(runtimeEntities.Count);

        foreach (SaveableEntity entity in runtimeEntities)
        {
            WriteEntityRecord(writer, entity.GetUniqueID(), entity.GetPrefabID(), entity);
        }

        Debug.Log($"Saved {runtimeEntities.Count} runtime entities.");
    }

    /// <summary>
    /// Loads this map into the current scene.
    /// </summary>
    /// <param name="path">The path to load from. Format: /[username]/[mapName]</param>
    public static void LoadMap(string path)
    {
        ClearRegistry();

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

        Transform entityParent = GameObject.FindWithTag("Entity parent").transform;
        Debug.Log(entityParent.name);

        SaveableTilemap sceneTilemap = GameObject.FindWithTag("TilemapEntity")?.GetComponent<SaveableTilemap>();

        using FileStream stream = File.Open(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(stream);

        int version = reader.ReadInt32();
        int count = reader.ReadInt32();
        int loadedCount = 0;

        for (int i = 0; i < count; i++)
        {
            ReadEntityRecord(reader, out string id, out string prefabID, out byte[] payload);

            using MemoryStream payloadStream = new MemoryStream(payload);
            using BinaryReader payloadReader = new BinaryReader(payloadStream);

            if (sceneTilemap != null && sceneTilemap.GetUniqueID() == id)
            {
                sceneTilemap.Read(payloadReader);
                loadedCount++;
                continue;
            }

            GameObject prefab = SaveRegistry.GetPrefab(prefabID);
            if (prefab != null)
            {
                bool isPlayer = prefab.CompareTag("PlayerEntity");
                GameObject obj;

                if (isPlayer)
                {
                    obj = GameObject.FindWithTag("PlayerEntity");
                    if (obj == null)
                    {
                        Debug.LogWarning($"Player not found in scene.");
                        continue;
                    }
                }
                else
                {
                    obj = GameObject.Instantiate(prefab);
                    obj.transform.SetParent(entityParent);
                }

                SaveableEntity entity = obj.GetComponent<SaveableEntity>();
                if (entity == null)
                {
                    Debug.LogWarning($"Prefab {prefabID} does not contain a SaveableEntity component.");
                    continue;
                }

                typeof(SaveableEntity)
                    .GetField("uniqueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(entity, id);

                entity.Read(payloadReader);
                Register(entity);
                loadedCount++;
            }
            else
            {
                Debug.LogWarning($"Prefab {prefabID} not found for entity {id}. Skipping this entity.");
            }
        }

        Debug.Log($"Loaded {loadedCount}/{count} entities. (player mode).");
    }

    /// <summary>
    /// Saves the map from the builder.
    /// </summary>
    /// <param name="path">The path to save to. Format: /[username]/[mapName]</param>
    public static void SaveBuilderMap(string path)
    {
        path = Application.persistentDataPath + "/" + path;
        List<BuilderEntity> builders = BuilderRegistry.GetAll();
        List<SaveableTilemap> tilemaps = saveables.Values.OfType<SaveableTilemap>().ToList();

        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(SaveVersion);
        writer.Write(builders.Count + tilemaps.Count);

        foreach (SaveableTilemap tilemap in tilemaps)
        {
            string id = tilemap.GetUniqueID();
            string prefabID = tilemap.GetPrefabID();

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"Tilemap has EMPTY ID: {tilemap.name}");
                continue;
            }

            if (string.IsNullOrEmpty(prefabID))
            {
                Debug.LogError($"Tilemap has EMPTY PrefabID: {tilemap.name}");
                continue;
            }

            Debug.Log($"Saving Tilemap | ID: {id} | Prefab: {prefabID}");

            WriteEntityRecord(writer, id, prefabID, tilemap);
        }

        foreach (BuilderEntity builder in builders)
        {
            string id = builder.GetUniqueID();
            string prefabID = builder.PrefabID;

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"Builder entity has EMPTY ID: {builder.name}");
                continue;
            }

            if (string.IsNullOrEmpty(prefabID))
            {
                Debug.LogError($"Builder entity has EMPTY PrefabID: {builder.name}");
                continue;
            }

            WriteEntityRecord(writer, id, prefabID, builder);
        }

        Debug.Log($"Saved {builders.Count} builder entities and {tilemaps.Count} tilemaps.");
    }

    /// <summary>
    /// Loads the map into the builder using BuilderEntities to omit unneeded behaviour.
    /// </summary>
    /// <param name="path">The path to load from. Format: /[username]/[mapName]</param>
    public static void LoadBuilderMap(string path)
    {
        if (!SaveRegistry.IsInitialized)
        {
            SaveRegistry.InitializeRegistryFromResources();
        }

        Transform entityParent = GameObject.FindWithTag("Entity parent").transform;
        path = Application.persistentDataPath + "/" + path;
        if (!File.Exists(path))
        {
            Debug.LogWarning("Builder save file not found.");
            return;
        }

        foreach (BuilderEntity b in BuilderRegistry.GetAll())
        {
            Object.DestroyImmediate(b.gameObject);
        }

        using FileStream stream = File.Open(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(stream);

        int version = reader.ReadInt32();
        int count = reader.ReadInt32();
        int loadedCount = 0;

        for (int i = 0; i < count; i++)
        {
            ReadEntityRecord(reader, out string id, out string prefabID, out byte[] payload);

            using MemoryStream payloadStream = new MemoryStream(payload);
            using BinaryReader payloadReader = new BinaryReader(payloadStream);

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("Builder load: EMPTY ID — skipping entity.");
                continue;
            }

            if (string.IsNullOrEmpty(prefabID))
            {
                Debug.LogError($"Builder load: EMPTY prefabID for entity {id} — skipping.");
                continue;
            }

            SaveableEntity existingSceneEntity = SaveManager.GetEntityByID(id);
            if (existingSceneEntity != null && existingSceneEntity is SaveableTilemap)
            {
                existingSceneEntity.Read(payloadReader);
                loadedCount++;
                continue;
            }

            GameObject go = new GameObject(prefabID);
            go.transform.SetParent(entityParent);
            BuilderEntity builder = go.AddComponent<BuilderEntity>();

            BuilderRegistry.Unregister(builder);

            typeof(SaveableEntity)
                .GetField("uniqueID", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(builder, id);

            builder.Initialize(prefabID);

            builder.Read(payloadReader);
            BuilderRegistry.Register(builder);
            loadedCount++;
        }

        Debug.Log($"Loaded {loadedCount}/{count} builder entities.");
    }
}