using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// A lightweight version of SaveableEntity used in the builder.
/// Inherits SaveableEntity to reuse save/load logic.
/// </summary>
public class BuilderEntity : SaveableEntity
{
    private Item originalItem;

    /// <summary>
    /// Gets the prefab this builder entity represents.
    /// </summary>
    public string PrefabID { get; private set; }

    /// <summary>
    /// Initialize the builder entity dynamically from a prefab.
    /// </summary>
    /// <param name="prefabID">The ID for the prefab we stimulate.</param>
    public void Initialize(string prefabID)
    {
        this.PrefabID = prefabID;

        // Store prefab ID in identity component
        PrefabIdentity identity = this.GetComponent<PrefabIdentity>();
        if (identity != null)
        {
            identity.PrefabID = prefabID;
        }

        GameObject prefab = SaveRegistry.GetPrefab(prefabID);
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found for ID: {prefabID}");
            return;
        }

        this.gameObject.tag = prefab.tag;

        SaveableEntity source = prefab.GetComponent<SaveableEntity>();
        if (source != null)
        {
            if (source is ItemObject itemSource)
            {
                FieldInfo originalItemField = typeof(ItemObject).GetField(
                    "originalItem",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                this.originalItem = (Item)originalItemField.GetValue(itemSource);

                if (this.originalItem == null || string.IsNullOrEmpty(this.originalItem.ItemID))
                {
                    Debug.LogError(
                        $"BuilderEntity '{prefabID}' failed to capture a valid originalItem " +
                        $"(item={(this.originalItem == null ? "NULL" : this.originalItem.name)}, " +
                        $"ItemID='{this.originalItem?.ItemID}'). This entity WILL corrupt the save stream.");
                }
            }

            FieldInfo[] sourceFields = source.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            FieldInfo[] targetFields = this.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            Dictionary<string, FieldInfo> targetMap = new ();
            foreach (FieldInfo f in targetFields)
            {
                targetMap[f.Name] = f;
            }

            foreach (FieldInfo field in sourceFields)
            {
                if (!Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
                {
                    continue;
                }

                if (targetMap.TryGetValue(field.Name, out FieldInfo targetField))
                {
                    object value = field.GetValue(source);

                    try
                    {
                        targetField.SetValue(this, value);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(
                            $"Failed to copy field '{field.Name}' on {this.name}: {e.Message}");
                    }
                }
            }
        }

        SpriteRenderer prefabRenderer = prefab.GetComponent<SpriteRenderer>();
        if (prefabRenderer != null)
        {
            SpriteRenderer sr = this.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = this.gameObject.AddComponent<SpriteRenderer>();
            }

            sr.sprite = prefabRenderer.sprite;
            sr.color = prefabRenderer.color;
            sr.sortingLayerID = prefabRenderer.sortingLayerID;
            sr.sortingOrder = prefabRenderer.sortingOrder;
        }
        else
        {
            Debug.Log($"BuilderEntity is invisible: {prefabID} at {this.transform.position}");
        }

        Transform selectionOutline = prefab.transform.Find("SelectionOutline");

        if (selectionOutline != null)
        {
            GameObject outline = Instantiate(
                selectionOutline.gameObject,
                this.transform);

            outline.name = selectionOutline.name;
        }

        foreach (MonoBehaviour mb in this.GetComponents<MonoBehaviour>())
        {
            if (mb != this && !(mb is SaveableEntity))
            {
                mb.enabled = false;
            }
        }
    }

    /// <summary>
    /// Writes base builder data, plus item data in the same wire format
    /// ItemObject.Write produces, so ItemObject.Read can consume it on load.
    /// </summary>
    /// <param name="writer">The writer used for saving/loading.</param>
    public override void Write(BinaryWriter writer)
    {
        base.Write(writer);

        bool isItemEntity = this.originalItem != null;

        if (isItemEntity)
        {
            if (string.IsNullOrEmpty(this.originalItem.ItemID))
            {
                throw new System.InvalidOperationException(
                    $"Cannot save: BuilderEntity for prefab '{this.PrefabID}' has an " +
                    $"originalItem with no ItemID. Fix the asset before saving.");
            }

            writer.Write(this.originalItem.ItemID);
            this.originalItem.WriteRuntimeFields(writer);
        }
    }

    /// <summary>
    /// Mirrors Write: if this entity represents an item, consume the same
    /// itemID + runtime fields Write appended, keeping the stream aligned.
    /// </summary>
    /// <param name="reader">The reader used for saving/loading.</param>
    public override void Read(BinaryReader reader)
    {
        base.Read(reader);

        if (this.originalItem != null)
        {
            string itemID = reader.ReadString();
            Item def = ItemLibrary.GetItemByIDGlobal(itemID);
            if (def == null)
            {
                Debug.LogError($"BuilderEntity: could not find Item with ID '{itemID}'.");
                return;
            }

            Item copy = Instantiate(def);
            copy.name = def.name + "_RuntimeCopy";
            copy.ReadRuntimeFields(reader);
            this.originalItem = copy;
        }
    }

    /// <summary>
    /// Override Awake so BuilderEntities register only in the builder registry.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        BuilderRegistry.Register(this);

        // Disable all other behaviours to make it lightweight
        foreach (MonoBehaviour mb in this.GetComponents<MonoBehaviour>())
        {
            if (mb != this)
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