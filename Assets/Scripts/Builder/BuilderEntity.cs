using System;
using System.Collections;
using System.Collections.Generic;
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
        Debug.Log("Initializing BuiderEntity with prefab " + prefabID + "at " + this.transform.position);
        this.PrefabID = prefabID;

        GameObject runtimePrefab = SaveRegistry.GetPrefab(prefabID);
        if (runtimePrefab != null)
        {
            // Copy all [SaveField] values from the prefab to this entity
            var mb = runtimePrefab.GetComponent<SaveableEntity>();
            if (mb != null)
            {
                var fields = mb.GetType().GetFields(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    if (Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
                    {
                        field.SetValue(this, field.GetValue(mb));
                    }
                }
            }

            SpriteRenderer prefabRenderer = runtimePrefab.GetComponent<SpriteRenderer>();
            if (prefabRenderer != null)
            {
                SpriteRenderer sr = this.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = prefabRenderer.sprite;
                sr.color = prefabRenderer.color;
                sr.sortingLayerID = prefabRenderer.sortingLayerID;
                sr.sortingOrder = prefabRenderer.sortingOrder;
            }
            else
            {
                Debug.Log("Added invisible entity " + this.GetUniqueID() + " at pos: " + this.transform.position);
            }
        }
    }

    /// <summary>
    /// Override Awake so BuilderEntities register only in the builder registry.
    /// </summary>
    protected override void Awake()
    {
        base.Awake(); // still assigns a uniqueID
        BuilderRegistry.Register(this);

        // Disable all other behaviours to make it lightweight
        foreach (var mb in this.GetComponents<MonoBehaviour>())
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