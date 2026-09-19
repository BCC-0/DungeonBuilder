using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// All entities that should be saved to a map should extend from this class.
/// </summary>
[RequireComponent(typeof(PrefabIdentity))]
public abstract class SaveableEntity : MonoBehaviour
{
    [SerializeField]
    private string uniqueID;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private SaveableEntity ownedBy;

    /// <summary>
    /// Gets the spriteRenderer of this entity.
    /// </summary>
    protected SpriteRenderer SpriteRenderer
    {
        get { return this.spriteRenderer; }
    }

    /// <summary>
    /// Gets the unique id of this entity.
    /// Can be used for referencing other entities.
    /// </summary>
    /// <returns>The unique id of this entity.</returns>
    public string GetUniqueID() => this.uniqueID;

    /// <summary>
    /// Gets the prefab id this entity uses.
    /// </summary>
    /// <returns>Returns the id. </returns>
    public string GetPrefabID()
    {
        PrefabIdentity identity = this.GetComponent<PrefabIdentity>();
        return identity != null ? identity.PrefabID : string.Empty;
    }

    /// <summary>
    /// Returns this entity as item for the builder inventory.
    /// </summary>
    /// <returns>An inventory item of this entity.</returns>
    public virtual InventoryItem GetAsInventoryItem()
    {
        return new InventoryItem(this.gameObject, this.spriteRenderer.sprite);
    }

    /// <summary>
    /// Gets the entity that owns this entity.
    /// </summary>
    public SaveableEntity OwnedBy => this.ownedBy;

    /// <summary>
    /// Gets whether this entity currently exists in the world.
    /// </summary>
    public bool ExistsInWorld => this.ownedBy == null;

    /// <summary>
    /// Makes this entity owned by another entity.
    /// </summary>
    /// <param name="owner">The entity that owns this entity.</param>
    public void SetOwner(SaveableEntity owner)
    {
        this.ownedBy = owner;
        this.SetWorldPresence(false);
    }

    /// <summary>
    /// Releases this entity from its owner.
    /// </summary>
    public void ReleaseOwner()
    {
        this.ownedBy = null;
        this.SetWorldPresence(true);
    }

    /// <summary>
    /// Writes this entity to the map file.
    /// </summary>
    /// <param name="writer">The writer to use.</param>
    public virtual void Write(BinaryWriter writer)
    {
        bool existsInWorld = this.ExistsInWorld;

        writer.Write(existsInWorld);

        if (existsInWorld)
        {
            this.WriteTransformData(writer);
        }

        FieldInfo[] fields = this.GetSaveFields();
        writer.Write(fields.Length);

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(this);
            writer.Write(field.Name);

            this.WriteValue(writer, field.FieldType, value);
        }
    }

    /// <summary>
    /// Reads this entity from the map file.
    /// </summary>
    /// <param name="reader">The reader to use.</param>
    public virtual void Read(BinaryReader reader)
    {
        bool existsInWorld = reader.ReadBoolean();

        if (existsInWorld)
        {
            this.ReadTransformData(reader);
        }

        int fieldCount = reader.ReadInt32();

        FieldInfo[] fields = this.GetSaveFields();
        Dictionary<string, FieldInfo> fieldMap = new ();

        foreach (FieldInfo f in fields)
        {
            fieldMap[f.Name] = f;
        }

        for (int i = 0; i < fieldCount; i++)
        {
            string fieldName = reader.ReadString();

            if (fieldMap.TryGetValue(fieldName, out FieldInfo field))
            {
                object value = this.ReadValue(reader, field.FieldType);
                field.SetValue(this, value);

                SaveFieldAttribute attribute = field.GetCustomAttribute<SaveFieldAttribute>();

                if (attribute != null &&
                    !attribute.ReferenceOnly &&
                    value is SaveableEntity entity)
                {
                    entity.SetOwner(this);
                }
            }
            else
            {
                Debug.LogWarning($"Field {fieldName} not found on {this.name}");
            }
        }

        if (!existsInWorld)
        {
            this.SetWorldPresence(false);
        }
    }

    /// <summary>
    /// Called when the map is finished loading.
    /// </summary>
    public virtual void OnFinishMapLoad()
    {
    }

    /// <summary>
    /// Sets whether this entity exists in the world.
    /// </summary>
    /// <param name="exists">Whether this entity should exist in the world.</param>
    protected virtual void SetWorldPresence(bool exists)
    {
        foreach (Renderer renderer in this.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = exists;
        }

        foreach (Collider collider in this.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = exists;
        }

        foreach (Collider2D collider in this.GetComponentsInChildren<Collider2D>(true))
        {
            collider.enabled = exists;
        }
    }

    /// <summary>
    /// Generates an Id if it does not exist and registers this entity to the save manager.
    /// </summary>
    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(this.uniqueID))
        {
            this.uniqueID = Guid.NewGuid().ToString();
        }

        if (!(this is BuilderEntity))
        {
            SaveManager.Register(this);
        }
    }

    /// <summary>
    /// Writes the transform to the map file.
    /// </summary>
    /// <param name="writer">The writes to use.</param>
    protected void WriteTransformData(BinaryWriter writer)
    {
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
    }

    /// <summary>
    /// Reads the transform from the map file.
    /// </summary>
    /// <param name="reader">The reader to use.</param>
    protected void ReadTransformData(BinaryReader reader)
    {
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
    }

    /// <summary>
    /// Gets all fields that should be saved on this entity.
    /// </summary>
    /// <returns>The save fields.</returns>
    protected FieldInfo[] GetSaveFields()
    {
        FieldInfo[] allFields = this.GetType().GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        List<FieldInfo> saveFields = new();

        foreach (FieldInfo field in allFields)
        {
            if (Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
            {
                saveFields.Add(field);
            }
        }

        return saveFields.ToArray();
    }

    /// <summary>
    /// Write the value of a given type.
    /// </summary>
    /// <param name="writer">The writer to use.</param>
    /// <param name="type">The type of the field to write.</param>
    /// <param name="value">The value of the field to write.</param>
    protected void WriteValue(BinaryWriter writer, Type type, object value)
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

    /// <summary>
    /// Reads a value back and returns the found object.
    /// </summary>
    /// <param name="reader">The reader to use.</param>
    /// <param name="type">The type to expect.</param>
    /// <returns>The read object.</returns>
    protected object ReadValue(BinaryReader reader, Type type)
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