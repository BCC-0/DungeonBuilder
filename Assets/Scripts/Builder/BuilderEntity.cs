using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class BuilderEntity : SaveableEntity
{
    private readonly Dictionary<string, RuntimeEditableValue> runtimeEditableFields =
        new Dictionary<string, RuntimeEditableValue>();

    private Item originalItem;

    public string PrefabID { get; private set; }

    public IReadOnlyDictionary<string, RuntimeEditableValue> RuntimeEditableFields => this.runtimeEditableFields;

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

    public bool TryGetRuntimeEditableField(
        string fieldName,
        out RuntimeEditableValue editableValue)
    {
        return this.runtimeEditableFields.TryGetValue(
            fieldName,
            out editableValue);
    }

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

        if (value != null &&
            !fieldType.IsInstanceOfType(value))
        {
            return;
        }

        bool fitsFieldType =
            value == null ||
            fieldType.IsInstanceOfType(value);

        // Builder proxies stand in for entities of the real type.
        bool isEntityProxy =
            value is BuilderEntity &&
            typeof(SaveableEntity).IsAssignableFrom(fieldType);

        if (!fitsFieldType && !isEntityProxy)
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

    public override void Read(BinaryReader reader)
    {
        this.ReadTransformData(reader);

        this.runtimeEditableFields.Clear();

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
                    this.ReadValue(
                        reader,
                        field.FieldType);

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

    protected override void Awake()
    {
        base.Awake();

        BuilderRegistry.Register(this);

        foreach (MonoBehaviour mb in
                 this.GetComponents<MonoBehaviour>())
        {
            if (mb != this)
            {
                mb.enabled = false;
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
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

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
                field.GetValue(source);

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
                field.GetValue(source);

            this.runtimeEditableFields[field.Name] =
                new RuntimeEditableValue(
                    field,
                    value);
        }
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

    private void OnDestroy()
    {
        BuilderRegistry.Unregister(this);
    }
}