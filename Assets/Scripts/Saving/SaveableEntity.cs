using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// The base class for all entities that will be saved in a map.
/// This automatically saves and restores transforms.
/// </summary>
[RequireComponent(typeof(PrefabIdentity))]
public abstract class SaveableEntity : MonoBehaviour
{
    [SerializeField]
    private string uniqueID;

    /// <summary>
    /// Gets the unique ID of this entity.
    /// </summary>
    /// <returns>The UID.</returns>
    public string GetUniqueID() => this.uniqueID;

    /// <summary>
    /// Gets the prefab ID of this entity. The prefab ID for each entity of the same type should be the same.
    /// </summary>
    /// <returns>The Prefab ID.</returns>
    public string GetPrefabID()
    {
        PrefabIdentity identity = this.GetComponent<PrefabIdentity>();
        return identity != null ? identity.PrefabID : null;
    }

    /// <summary>
    /// Writes all fields we need to save from this entity.
    /// </summary>
    /// <param name="writer">The writer which will save the fields.</param>
    public virtual void Write(BinaryWriter writer)
    {
        // --- Save Transform ---
        writer.Write(this.transform.position.x);
        writer.Write(this.transform.position.y);
        writer.Write(this.transform.position.z);

        writer.Write(this.transform.rotation.x);
        writer.Write(this.transform.rotation.y);
        writer.Write(this.transform.rotation.z);
        writer.Write(this.transform.rotation.w);

        writer.Write(this.transform.localScale.x);
        writer.Write(this.transform.localScale.y);
        writer.Write(this.transform.localScale.z);

        // --- Save Fields ---
        var fields = this.GetSaveFields();
        writer.Write(fields.Length);

        foreach (var field in fields)
        {
            object value = field.GetValue(this);
            writer.Write(field.Name);

            this.WriteValue(writer, field.FieldType, value);
        }
    }

    /// <summary>
    /// Reads all fields we need to restore for this entity.
    /// </summary>
    /// <param name="reader">The reader which will read the fields to restore.</param>
    public virtual void Read(BinaryReader reader)
    {
        // --- Load Transform ---
        Vector3 pos = new Vector3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());

        Quaternion rot = new Quaternion(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());

        Vector3 scale = new Vector3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());

        this.transform.position = pos;
        this.transform.rotation = rot;
        this.transform.localScale = scale;

        // --- Load Fields ---
        int fieldCount = reader.ReadInt32();

        var fields = this.GetSaveFields();
        Dictionary<string, FieldInfo> fieldMap = new ();

        foreach (var f in fields)
        {
            fieldMap[f.Name] = f;
        }

        for (int i = 0; i < fieldCount; i++)
        {
            string fieldName = reader.ReadString();

            if (fieldMap.TryGetValue(fieldName, out var field))
            {
                object value = this.ReadValue(reader, field.FieldType);
                field.SetValue(this, value);
            }
            else
            {
                Debug.LogWarning($"Field {fieldName} not found on {this.name}");
            }
        }
    }

    /// <summary>
    /// Registers this entity to the SaveManager and makes sure it has a unique ID.
    /// </summary>
    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(this.uniqueID))
        {
            this.uniqueID = Guid.NewGuid().ToString();
        }

        SaveManager.Register(this);
    }

    private FieldInfo[] GetSaveFields()
    {
        var allFields = this.GetType().GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        List<FieldInfo> saveFields = new ();

        foreach (var field in allFields)
        {
            if (Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
            {
                saveFields.Add(field);
            }
        }

        return saveFields.ToArray();
    }

    private void WriteValue(BinaryWriter writer, Type type, object value)
    {
        if (type == typeof(int))
        {
            writer.Write((int)value);
        }
        else if (type == typeof(float))
        {
            writer.Write((float)value);
        }
        else if (type == typeof(bool))
        {
            writer.Write((bool)value);
        }
        else if (type == typeof(string))
        {
            writer.Write((string)value ?? string.Empty);
        }
        else if (typeof(SaveableEntity).IsAssignableFrom(type))
        {
            string id = value != null
                ? ((SaveableEntity)value).GetUniqueID()
                : string.Empty;
            writer.Write(id);
        }
        else if (typeof(IList).IsAssignableFrom(type) &&
                 type.IsGenericType &&
                 typeof(SaveableEntity).IsAssignableFrom(
                     type.GetGenericArguments()[0]))
        {
            IList list = value as IList;
            writer.Write(list?.Count ?? 0);

            if (list != null)
            {
                foreach (SaveableEntity entity in list)
                {
                    writer.Write(entity.GetUniqueID());
                }
            }
        }
        else
        {
            Debug.LogError($"Unsupported save type: {type}");
        }
    }

    private object ReadValue(BinaryReader reader, Type type)
    {
        if (type == typeof(int))
        {
            return reader.ReadInt32();
        }

        if (type == typeof(float))
        {
            return reader.ReadSingle();
        }

        if (type == typeof(bool))
        {
            return reader.ReadBoolean();
        }

        if (type == typeof(string))
        {
            return reader.ReadString();
        }

        if (typeof(SaveableEntity).IsAssignableFrom(type))
        {
            string id = reader.ReadString();
            return SaveManager.GetEntityByID(id);
        }

        if (typeof(IList).IsAssignableFrom(type) &&
            type.IsGenericType &&
            typeof(SaveableEntity).IsAssignableFrom(
                type.GetGenericArguments()[0]))
        {
            int count = reader.ReadInt32();
            IList list = (IList)Activator.CreateInstance(type);

            for (int i = 0; i < count; i++)
            {
                string id = reader.ReadString();
                list.Add(SaveManager.GetEntityByID(id));
            }

            return list;
        }

        Debug.LogError($"Unsupported load type: {type}");
        return null;
    }
}