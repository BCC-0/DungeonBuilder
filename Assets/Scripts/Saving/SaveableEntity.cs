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
    private readonly List<PendingReference> pendingReferences =
        new List<PendingReference>();

    [SerializeField]
    private string uniqueID;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private SaveableEntity consumedBy;

    /// <summary>
    /// Gets the spriteRenderer of this entity.
    /// </summary>
    protected SpriteRenderer SpriteRenderer
    {
        get { return this.spriteRenderer; }
    }

    /// <summary>
    /// Gets a value indicating whether there are references that still wait to be resolved.
    /// </summary>
    protected bool HasPendingReferences => this.pendingReferences.Count > 0;

    /// <summary>
    /// Gets the entity that consumes this entity.
    /// </summary>
    public SaveableEntity ConsumedBy => this.consumedBy;

    /// <summary>
    /// Gets whether this entity currently exists in the world.
    /// </summary>
    public bool ExistsInWorld => this.consumedBy == null;

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
    /// Makes this entity consumed by another entity.
    /// </summary>
    /// <param name="consumer">The entity that consumes this entity.</param>
    /// <param name="consumingField">
    /// The name of the field on the consumer that holds this entity. That field
    /// keeps its reference, all other references to this entity are removed.
    /// </param>
    /// <returns>True when this entity was successfully consumed.</returns>
    public bool SetConsumedBy(SaveableEntity consumer, string consumingField = null)
    {
        if (consumer == null)
        {
            return false;
        }

        if (this.consumedBy != null &&
            this.consumedBy != consumer)
        {
            return false;
        }

        this.consumedBy = consumer;
        this.RemoveOtherReferences(consumer, consumingField);
        this.SetWorldPresence(false);
        return true;
    }

    /// <summary>
    /// Releases this entity from its consumer.
    /// </summary>
    /// <param name="consumer">The entity that consumes this entity.</param>
    public void ReleaseConsumedBy(SaveableEntity consumer)
    {
        if (this.consumedBy != consumer)
        {
            return;
        }

        this.consumedBy = null;
        this.SetWorldPresence(true);
    }

    /// <summary>
    /// Writes this entity to the map file.
    /// The layout is the same as before ownership was added: the transform
    /// first, then the save fields. Consumption is not stored, it is restored
    /// on load from the reference fields of the consumer.
    /// </summary>
    /// <param name="writer">The writer to use.</param>
    public virtual void Write(BinaryWriter writer)
    {
        this.WriteTransformData(writer);

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
        this.ReadTransformData(reader);

        this.pendingReferences.Clear();

        int fieldCount = reader.ReadInt32();

        FieldInfo[] fields = this.GetSaveFields();
        Dictionary<string, FieldInfo> fieldMap = new();

        foreach (FieldInfo f in fields)
        {
            fieldMap[f.Name] = f;
        }

        for (int i = 0; i < fieldCount; i++)
        {
            string fieldName = reader.ReadString();

            if (fieldMap.TryGetValue(fieldName, out FieldInfo field))
            {
                object value = this.ReadFieldValue(reader, field, field.Name);
                field.SetValue(this, value);
            }
            else
            {
                Debug.LogWarning($"Field {fieldName} not found on {this.name}");
            }
        }
    }

    /// <summary>
    /// Called when the map is finished loading. All entities exist now, so
    /// the references that were read as ids are resolved here. Overrides must
    /// call the base method.
    /// </summary>
    public virtual void OnFinishMapLoad()
    {
        this.ResolvePendingReferences();
    }

    /// <summary>
    /// Whether a type is a list of entities.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a list of entities.</returns>
    protected static bool IsEntityList(Type type)
    {
        return typeof(IList).IsAssignableFrom(type) &&
               type.IsGenericType &&
               typeof(SaveableEntity).IsAssignableFrom(
                   type.GetGenericArguments()[0]);
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

        List<FieldInfo> saveFields = new ();

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
                foreach (object item in list)
                {
                    // Empty slots ("None" in the inspector) are stored as an
                    // empty id, the same way single references are.
                    SaveableEntity entity = item as SaveableEntity;

                    writer.Write(
                        entity != null
                            ? entity.GetUniqueID()
                            : string.Empty);
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

    /// <summary>
    /// Reads one saved field. Entity references are stored as ids and are
    /// only filled in when the map is finished loading, because the entity
    /// they point to may not have been loaded yet.
    /// </summary>
    /// <param name="reader">The reader that is read.</param>
    /// <param name="field">The field that is read.</param>
    /// <param name="key">The name the value is stored under.</param>
    /// <returns>The value, with empty slots for references.</returns>
    protected object ReadFieldValue(
        BinaryReader reader,
        FieldInfo field,
        string key)
    {
        Type type = field.FieldType;

        if (typeof(SaveableEntity).IsAssignableFrom(type))
        {
            this.AddPendingReference(
                field,
                key,
                -1,
                reader.ReadString());

            return null;
        }

        if (IsEntityList(type))
        {
            int count = reader.ReadInt32();
            IList list = this.CreateReferenceList(type);

            for (int i = 0; i < count; i++)
            {
                // Keep the slot, so indices stay the same.
                list.Add(null);

                this.AddPendingReference(
                    field,
                    key,
                    i,
                    reader.ReadString());
            }

            return list;
        }

        return this.ReadValue(reader, type);
    }

    /// <summary>
    /// Forgets the references that were still waiting to be resolved.
    /// </summary>
    protected void ClearPendingReferences()
    {
        this.pendingReferences.Clear();
    }

    /// <summary>
    /// Creates the list that holds references while loading.
    /// </summary>
    /// <param name="listType">The type of the field.</param>
    /// <returns>An empty list.</returns>
    protected virtual IList CreateReferenceList(Type listType)
    {
        return (IList)Activator.CreateInstance(listType);
    }

    /// <summary>
    /// Finds an entity by its id.
    /// </summary>
    /// <param name="id">The id to look for.</param>
    /// <returns>The entity, or null when it does not exist.</returns>
    protected virtual SaveableEntity FindEntityByID(string id)
    {
        return SaveManager.GetEntityByID(id) as SaveableEntity;
    }

    /// <summary>
    /// Stores a resolved reference in the field it belongs to.
    /// </summary>
    /// <param name="pending">The reference that was resolved.</param>
    /// <param name="entity">The entity it points to.</param>
    /// <returns>True when the reference was stored.</returns>
    protected virtual bool AssignReference(
        PendingReference pending,
        SaveableEntity entity)
    {
        if (pending.Index < 0)
        {
            if (!pending.Field.FieldType.IsInstanceOfType(entity))
            {
                return false;
            }

            pending.Field.SetValue(this, entity);
            return true;
        }

        IList list = pending.Field.GetValue(this) as IList;

        Type elementType =
            pending.Field.FieldType.GetGenericArguments()[0];

        if (list == null ||
            pending.Index >= list.Count ||
            !elementType.IsInstanceOfType(entity))
        {
            return false;
        }

        list[pending.Index] = entity;
        return true;
    }

    /// <summary>
    /// Removes every reference to the target from this entity's reference
    /// fields, except from the field with the given name.
    /// </summary>
    /// <param name="target">The entity that should no longer be referenced.</param>
    /// <param name="exceptField">The field name to leave untouched, or null.</param>
    protected virtual void RemoveReferencesTo(
        SaveableEntity target,
        string exceptField)
    {
        foreach (FieldInfo field in this.GetSaveFields())
        {
            if (field.Name == exceptField)
            {
                continue;
            }

            if (typeof(SaveableEntity).IsAssignableFrom(field.FieldType))
            {
                if (field.GetValue(this) as SaveableEntity == target)
                {
                    field.SetValue(this, null);
                }

                continue;
            }

            if (!IsEntityList(field.FieldType))
            {
                continue;
            }

            IList list = field.GetValue(this) as IList;

            if (list == null)
            {
                continue;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] as SaveableEntity == target)
                {
                    list.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Removes this entity from all reference fields on other entities.
    /// The consuming field of the consumer keeps its reference.
    /// </summary>
    /// <param name="consumer">The entity that now consumes this entity.</param>
    /// <param name="consumingField">The field on the consumer that keeps the reference.</param>
    private void RemoveOtherReferences(
        SaveableEntity consumer,
        string consumingField)
    {
        Debug.Log($"Consume {this.name} by {consumer.name}, keep field: '{consumingField}'");

        SaveableEntity[] entities = FindObjectsByType<SaveableEntity>();

        foreach (SaveableEntity entity in entities)
        {
            if (entity == null ||
                entity == this)
            {
                continue;
            }

            // Only the consuming field of the consumer is exempt.
            string exceptField =
                entity == consumer
                    ? consumingField
                    : null;

            entity.RemoveReferencesTo(this, exceptField);
        }
    }


    private void AddPendingReference(
        FieldInfo field,
        string key,
        int index,
        string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        this.pendingReferences.Add(
            new PendingReference
            {
                Key = key,
                Index = index,
                ID = id,
                Field = field,
            });
    }

    /// <summary>
    /// Fills in the references that were read as ids, and makes this entity
    /// the consumer of the referenced ones when the field uses a consuming
    /// reference mode.
    /// </summary>
    private void ResolvePendingReferences()
    {
        foreach (PendingReference pending in this.pendingReferences)
        {
            SaveableEntity found =
                this.FindEntityByID(pending.ID);

            if (found == null)
            {
                Debug.LogWarning(
                    $"{this.name}: could not find the entity with id " +
                    $"'{pending.ID}' referenced by '{pending.Key}'.");

                continue;
            }

            if (!this.AssignReference(pending, found))
            {
                Debug.LogWarning(
                    $"{this.name}: could not store the reference " +
                    $"'{pending.Key}' (wrong type or missing slot).");

                continue;
            }

            SaveFieldAttribute attribute =
                pending.Field.GetCustomAttribute<SaveFieldAttribute>();

            if (attribute != null &&
                attribute.ReferenceMode == ReferenceMode.Consuming &&
                !found.SetConsumedBy(this, pending.Key))
            {
                Debug.LogWarning(
                    $"{this.name}: could not consume the entity " +
                    $"'{found.name}' because it is already consumed.");
            }
        }

        this.pendingReferences.Clear();
    }

    /// <summary>
    /// A reference that was read from the map file as an id. The entity it
    /// points to may not exist yet, so it is resolved when the map is loaded.
    /// </summary>
    protected class PendingReference
    {
        /// <summary>
        /// Gets or sets the name the value is stored under.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the list index, or -1 for a single reference.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the id of the referenced entity.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the field that holds the reference.
        /// </summary>
        public FieldInfo Field { get; set; }
    }
}