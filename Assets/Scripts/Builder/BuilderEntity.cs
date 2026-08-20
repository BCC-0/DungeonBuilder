using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// A lightweight version of SaveableEntity used in the builder.
/// Inherits SaveableEntity to reuse save/load logic.
/// </summary>
public class BuilderEntity : SaveableEntity
{
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