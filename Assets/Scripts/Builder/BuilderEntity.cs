using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// A light-weight replacement of the actual entity behaviour.
/// </summary>
public class BuilderEntity : SaveableEntity
{
    private readonly Dictionary<string, RuntimeEditableValue> runtimeEditableFields =
        new Dictionary<string, RuntimeEditableValue>();

    private Item originalItem;

    /// <summary>
    /// Gets the ID of the entity's prefab.
    /// </summary>
    public string PrefabID { get; private set; }

    /// <summary>
    /// Gets the editable fields on this entity.
    /// </summary>
    public IReadOnlyDictionary<string, RuntimeEditableValue> RuntimeEditableFields => this.runtimeEditableFields;

    /// <summary>
    /// Initializes the builder entity.
    /// </summary>
    /// <param name="prefabID">The id of the actual entity.</param>
    public void Initialize(string prefabID)
    {
        this.PrefabID = prefabID;

        this.runtimeEditableFields.Clear();

        PrefabIdentity identity = this.GetComponent<PrefabIdentity>();

        if (identity != null)
        {
            identity.PrefabID = prefabID;
        }

        GameObject prefab = SaveRegistry.GetPrefab(prefabID);

        if (prefab == null)
        {
            Debug.LogError(
                $"Prefab not found for ID: {prefabID}");

            return;
        }

        this.gameObject.tag = prefab.tag;

        SaveableEntity source =
            prefab.GetComponent<SaveableEntity>();

        if (source != null)
        {
            this.InitializeSourceData(source);
        }

        this.CopyRenderer(prefab);
        this.CopySelectionOutline(prefab);
        this.DisableOtherBehaviours();
    }

    /// <summary>
    /// Tries to get a given editable field.
    /// </summary>
    /// <param name="fieldName">The name of the field to get.</param>
    /// <param name="editableValue">The value to get.</param>
    /// <returns>A value indicating whether this action was succesful.</returns>
    public bool TryGetRuntimeEditableField(
        string fieldName,
        out RuntimeEditableValue editableValue)
    {
        return this.runtimeEditableFields.TryGetValue(
            fieldName,
            out editableValue);
    }

    /// <summary>
    /// Sets an editable field.
    /// </summary>
    /// <param name="fieldName">The name of the field to edit.</param>
    /// <param name="value">The value to set.</param>
    public void SetRuntimeEditableValue(
        string fieldName,
        object value)
    {
        if (!this.runtimeEditableFields.TryGetValue(
                fieldName,
                out RuntimeEditableValue editableValue))
        {
            return;
        }

        if (editableValue == null ||
            editableValue.Field == null)
        {
            return;
        }

        Type fieldType =
            editableValue.Field.FieldType;

        bool fitsFieldType =
            value == null ||
            fieldType.IsInstanceOfType(value);

        bool isEntityProxy =
            value is BuilderEntity &&
            typeof(SaveableEntity).IsAssignableFrom(fieldType);

        bool isProxyList =
            value is List<SaveableEntity> &&
            IsEntityList(fieldType);

        if (!fitsFieldType &&
            !isEntityProxy &&
            !isProxyList)
        {
            return;
        }

        editableValue.Value = value;

        if (fitsFieldType &&
            this.originalItem != null &&
            Attribute.IsDefined(
                editableValue.Field,
                typeof(RuntimeEditableAttribute)) &&
            editableValue.Field.DeclaringType != null &&
            editableValue.Field.DeclaringType.IsAssignableFrom(
                this.originalItem.GetType()))
        {
            editableValue.Field.SetValue(
                this.originalItem,
                value);
        }
    }

    /// <summary>
    /// Gets the editable field's value.
    /// </summary>
    /// <param name="fieldName">The name of the field to get.</param>
    /// <returns>The found value.</returns>
    public object GetRuntimeEditableValue(string fieldName)
    {
        if (!this.runtimeEditableFields.TryGetValue(
                fieldName,
                out RuntimeEditableValue editableValue))
        {
            return null;
        }

        return editableValue.Value;
    }

    /// <summary>
    /// Writes the entity to the map file.
    /// </summary>
    /// <param name="writer">The writer to use.</param>
    /// <exception cref="InvalidOperationException">The exception that can be found.</exception>
    public override void Write(BinaryWriter writer)
    {
        this.WriteTransformData(writer);

        List<KeyValuePair<string, RuntimeEditableValue>> fieldsToSave =
            new List<KeyValuePair<string, RuntimeEditableValue>>();

        foreach (KeyValuePair<string, RuntimeEditableValue> entry in
                 this.runtimeEditableFields)
        {
            RuntimeEditableValue editableValue =
                entry.Value;

            if (editableValue == null ||
                editableValue.Field == null)
            {
                continue;
            }

            if (!Attribute.IsDefined(
                    editableValue.Field,
                    typeof(SaveFieldAttribute)))
            {
                continue;
            }

            fieldsToSave.Add(entry);
        }

        writer.Write(fieldsToSave.Count);

        foreach (KeyValuePair<string, RuntimeEditableValue> entry in
                 fieldsToSave)
        {
            RuntimeEditableValue editableValue =
                entry.Value;

            writer.Write(entry.Key);

            this.WriteValue(
                writer,
                editableValue.Field.FieldType,
                editableValue.Value);
        }

        if (this.originalItem != null)
        {
            if (string.IsNullOrEmpty(
                    this.originalItem.ItemID))
            {
                throw new InvalidOperationException(
                    $"Cannot save: BuilderEntity for prefab " +
                    $"'{this.PrefabID}' has an originalItem with no " +
                    $"ItemID. Fix the asset before saving.");
            }

            writer.Write(this.originalItem.ItemID);
            this.originalItem.WriteRuntimeFields(writer);
        }
    }

    /// <summary>
    /// Reads the entity from the map file.
    /// </summary>
    /// <param name="reader">The reader to use.</param>
    public override void Read(BinaryReader reader)
    {
        this.ReadTransformData(reader);

        this.runtimeEditableFields.Clear();
        this.ClearPendingReferences();

        int fieldCount =
            reader.ReadInt32();

        for (int i = 0; i < fieldCount; i++)
        {
            string fieldName =
                reader.ReadString();

            FieldInfo field =
                this.FindSourceField(
                    fieldName);

            if (field != null)
            {
                object value =
                    this.ReadFieldValue(
                        reader,
                        field,
                        fieldName);

                this.runtimeEditableFields[fieldName] =
                    new RuntimeEditableValue(
                        field,
                        value);
            }
            else
            {
                Debug.LogWarning(
                    $"BuilderEntity: could not resolve source field " +
                    $"'{fieldName}' for prefab '{this.PrefabID}'. " +
                    $"Save data may be corrupted from this point.");
            }
        }

        if (this.originalItem != null)
        {
            string itemID =
                reader.ReadString();

            Item def =
                ItemLibrary.GetItemByIDGlobal(itemID);

            if (def == null)
            {
                Debug.LogError(
                    $"BuilderEntity: could not find Item " +
                    $"with ID '{itemID}'.");

                return;
            }

            Item copy =
                Instantiate(def);

            copy.name =
                def.name + "_RuntimeCopy";

            copy.ReadRuntimeFields(reader);

            this.originalItem = copy;

            this.CopyRuntimeEditableFields(
                this.originalItem);
        }
    }

    /// <summary>
    /// Registers this builder entity and disables the entities behaviour.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        BuilderRegistry.Register(this);

        foreach (MonoBehaviour mb in this.GetComponents<MonoBehaviour>())
        {
            if (mb != this)
            {
                mb.enabled = false;
            }
        }
    }

    /// <summary>
    /// Builder entities are not known to the SaveManager, so referenced
    /// entities are looked up among the builder entities.
    /// </summary>
    /// <param name="id">The id to look for.</param>
    /// <returns>The builder entity, or null when it does not exist.</returns>
    protected override SaveableEntity FindEntityByID(string id)
    {
        foreach (BuilderEntity candidate in
                 FindObjectsByType<BuilderEntity>())
        {
            if (candidate != null &&
                candidate != this &&
                candidate.GetUniqueID() == id)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// A list of builder proxies cannot be a List of the real type.
    /// </summary>
    /// <param name="listType">The type of the field.</param>
    /// <returns>An empty list of entities.</returns>
    protected override IList CreateReferenceList(Type listType)
    {
        return new List<SaveableEntity>();
    }

    /// <summary>
    /// Stores a resolved reference in the runtime editable values.
    /// </summary>
    /// <param name="pending">The reference that was resolved.</param>
    /// <param name="entity">The entity it points to.</param>
    /// <returns>True when the reference was stored.</returns>
    protected override bool AssignReference(
        PendingReference pending,
        SaveableEntity entity)
    {
        if (!this.runtimeEditableFields.TryGetValue(
                pending.Key,
                out RuntimeEditableValue editableValue) ||
            editableValue == null)
        {
            return false;
        }

        if (pending.Index < 0)
        {
            editableValue.Value = entity;
            return true;
        }

        if (editableValue.Value is List<SaveableEntity> list &&
            pending.Index < list.Count)
        {
            list[pending.Index] = entity;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes references to a target except for the given field.
    /// </summary>
    /// <param name="target">The target to remove.</param>
    /// <param name="exceptField">The field to keep it in.</param>
    protected override void RemoveReferencesTo(
    SaveableEntity target,
    string exceptField)
    {
        foreach (KeyValuePair<string, RuntimeEditableValue> entry in
                 this.runtimeEditableFields)
        {
            if (entry.Key == exceptField ||
                entry.Value == null)
            {
                continue;
            }

            object value = entry.Value.Value;

            if (value is SaveableEntity single)
            {
                if (single == target)
                {
                    entry.Value.Value = null;
                }

                continue;
            }

            if (value is IList list)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] as SaveableEntity == target)
                    {
                        list.RemoveAt(i);
                    }
                }
            }
        }
    }

    private void InitializeSourceData(SaveableEntity source)
    {
        if (source is ItemObject itemSource)
        {
            FieldInfo originalItemField =
                typeof(ItemObject).GetField(
                    "originalItem",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (originalItemField != null)
            {
                this.originalItem =
                    (Item)originalItemField.GetValue(itemSource);
            }

            if (this.originalItem == null ||
                string.IsNullOrEmpty(this.originalItem.ItemID))
            {
                Debug.LogError(
                    $"BuilderEntity '{this.PrefabID}' failed to capture " +
                    $"a valid originalItem " +
                    $"(item={(this.originalItem == null ? "NULL" : this.originalItem.name)}, " +
                    $"ItemID='{this.originalItem?.ItemID}'). " +
                    $"This entity WILL corrupt the save stream.");
            }

            if (this.originalItem != null)
            {
                this.CopyRuntimeEditableFields(
                    this.originalItem);
            }

            this.CopySaveFields(source);

            return;
        }

        this.CopySaveFields(source);
    }

    private void CopyRuntimeEditableFields(object source)
    {
        if (source == null)
        {
            return;
        }

        List<FieldInfo> sourceFields =
            this.GetAllFields(source.GetType());

        foreach (FieldInfo field in sourceFields)
        {
            if (!Attribute.IsDefined(
                    field,
                    typeof(RuntimeEditableAttribute)))
            {
                continue;
            }

            object value =
                this.CopyIfEntityList(
                    field,
                    field.GetValue(source));

            this.runtimeEditableFields[field.Name] =
                new RuntimeEditableValue(
                    field,
                    value);
        }
    }

    private void CopySaveFields(SaveableEntity source)
    {
        if (source == null)
        {
            return;
        }

        List<FieldInfo> sourceFields =
            this.GetAllFields(source.GetType());

        foreach (FieldInfo field in sourceFields)
        {
            if (!Attribute.IsDefined(
                    field,
                    typeof(SaveFieldAttribute)))
            {
                continue;
            }

            object value =
                this.CopyIfEntityList(
                    field,
                    field.GetValue(source));

            this.runtimeEditableFields[field.Name] =
                new RuntimeEditableValue(
                    field,
                    value);
        }
    }

    /// <summary>
    /// Gives every builder entity its own list, so entities made from the same
    /// prefab do not share one list instance with the prefab.
    /// </summary>
    /// <param name="field">The field the value belongs to.</param>
    /// <param name="value">The value read from the source.</param>
    /// <returns>A copy when the value is a list of entities, otherwise the value.</returns>
    private object CopyIfEntityList(FieldInfo field, object value)
    {
        if (IsEntityList(field.FieldType) &&
            value is IList original)
        {
            return new List<SaveableEntity>(
                original.Cast<SaveableEntity>());
        }

        return value;
    }

    private List<FieldInfo> GetAllFields(Type type)
    {
        List<FieldInfo> fields =
            new List<FieldInfo>();

        while (type != null)
        {
            fields.AddRange(
                type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly));

            type = type.BaseType;
        }

        return fields;
    }

    private FieldInfo FindSourceField(string fieldName)
    {
        if (string.IsNullOrEmpty(this.PrefabID))
        {
            return null;
        }

        GameObject prefab =
            SaveRegistry.GetPrefab(this.PrefabID);

        if (prefab == null)
        {
            return null;
        }

        SaveableEntity source =
            prefab.GetComponent<SaveableEntity>();

        if (source == null)
        {
            return null;
        }

        List<FieldInfo> fields =
            this.GetAllFields(source.GetType());

        foreach (FieldInfo field in fields)
        {
            if (field.Name == fieldName &&
                Attribute.IsDefined(
                    field,
                    typeof(SaveFieldAttribute)))
            {
                return field;
            }
        }

        return null;
    }

    private void CopyRenderer(GameObject prefab)
    {
        SpriteRenderer prefabRenderer =
            prefab.GetComponent<SpriteRenderer>();

        if (prefabRenderer != null)
        {
            SpriteRenderer sr =
                this.GetComponent<SpriteRenderer>();

            if (sr == null)
            {
                sr =
                    this.gameObject.AddComponent<SpriteRenderer>();
            }

            sr.sprite =
                prefabRenderer.sprite;

            sr.color =
                prefabRenderer.color;

            sr.sortingLayerID =
                prefabRenderer.sortingLayerID;

            sr.sortingOrder =
                prefabRenderer.sortingOrder;
        }
        else
        {
            Debug.Log(
                $"BuilderEntity is invisible: " +
                $"{this.PrefabID} at {this.transform.position}");
        }
    }

    private void CopySelectionOutline(GameObject prefab)
    {
        Transform selectionOutline =
            prefab.transform.Find("SelectionOutline");

        if (selectionOutline == null)
        {
            return;
        }

        GameObject outline =
            Instantiate(
                selectionOutline.gameObject,
                this.transform);

        outline.name =
            selectionOutline.name;
    }

    private void DisableOtherBehaviours()
    {
        foreach (MonoBehaviour mb in
                 this.GetComponents<MonoBehaviour>())
        {
            if (mb != this &&
                !(mb is SaveableEntity))
            {
                mb.enabled = false;
            }
        }
    }

    private void Start()
    {
        if (this.HasPendingReferences)
        {
            this.OnFinishMapLoad();
        }
    }

    private void OnDestroy()
    {
        BuilderRegistry.Unregister(this);
    }
}