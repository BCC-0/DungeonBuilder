using System.Reflection;
using UnityEngine;

/// <summary>
/// The name and description that are shown when the player hovers over
/// something in the builder.
/// </summary>
public class HoverInfo
{
    /// <summary>
    /// Hover info without a name or description.
    /// </summary>
    public static readonly HoverInfo Empty = new HoverInfo(string.Empty, string.Empty);

    /// <summary>
    /// Initializes a new instance of the <see cref="HoverInfo"/> class.
    /// </summary>
    /// <param name="name">The name shown as the title of the popup.</param>
    /// <param name="description">The description shown below the name.</param>
    public HoverInfo(string name, string description = null)
    {
        this.Name = name ?? string.Empty;
        this.Description = description ?? string.Empty;
    }

    /// <summary>
    /// Gets the name shown as the title of the popup.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description shown below the name.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets a value indicating whether there is anything to show.
    /// </summary>
    public bool HasContent =>
        !string.IsNullOrEmpty(this.Name) ||
        !string.IsNullOrEmpty(this.Description);

    /// <summary>
    /// Gets the hover info of an entity. A builder entity stands for a
    /// prefab, so the info of the prefab's entity is used.
    /// </summary>
    /// <param name="entity">The entity to describe.</param>
    /// <returns>The hover info of the entity.</returns>
    public static HoverInfo ForEntity(SaveableEntity entity)
    {
        if (entity == null)
        {
            return Empty;
        }

        if (entity is BuilderEntity builderEntity)
        {
            GameObject prefab = SaveRegistry.GetPrefab(builderEntity.PrefabID);
            SaveableEntity source = prefab != null
                ? prefab.GetComponent<SaveableEntity>()
                : null;

            if (source != null)
            {
                return source.GetHoverInfo();
            }
        }

        return entity.GetHoverInfo();
    }

    /// <summary>
    /// Gets the hover info of a prefab by reading the entity on it.
    /// </summary>
    /// <param name="prefab">The prefab to describe.</param>
    /// <returns>The hover info of the prefab.</returns>
    public static HoverInfo ForPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return Empty;
        }

        SaveableEntity entity = prefab.GetComponent<SaveableEntity>();

        if (entity != null)
        {
            return entity.GetHoverInfo();
        }

        return new HoverInfo(prefab.name);
    }

    /// <summary>
    /// Gets the hover info of an inventory item. Items that stand for a
    /// prefab use the info of the entity on that prefab, tiles use their name.
    /// </summary>
    /// <param name="item">The inventory item to describe.</param>
    /// <returns>The hover info of the inventory item.</returns>
    public static HoverInfo ForInventoryItem(InventoryItem item)
    {
        if (item == null)
        {
            return Empty;
        }

        if (item.Prefab != null)
        {
            HoverInfo prefabInfo = ForPrefab(item.Prefab);

            if (prefabInfo.HasContent)
            {
                return prefabInfo;
            }
        }

        if (item.Tile != null)
        {
            TileLibrary.TileEntry tileEntry =
                TileLibrary.GetEntryForTileGlobal(item.Tile);

            if (tileEntry != null)
            {
                string tileName = !string.IsNullOrEmpty(tileEntry.TileID)
                    ? tileEntry.TileID
                    : item.Name;

                return new HoverInfo(tileName, tileEntry.Description);
            }
        }

        return new HoverInfo(item.Name);
    }

    /// <summary>
    /// Gets the hover info of a save field. The name is the name the field is
    /// shown with, the description comes from the field's attributes.
    /// </summary>
    /// <param name="field">The field to describe.</param>
    /// <param name="displayName">The name the field is shown with.</param>
    /// <returns>The hover info of the field.</returns>
    public static HoverInfo ForField(FieldInfo field, string displayName)
    {
        string name = !string.IsNullOrEmpty(displayName)
            ? displayName.TrimEnd(' ', ':')
            : field != null
                ? field.Name
                : string.Empty;

        string description = string.Empty;

        if (field != null)
        {
            SaveFieldAttribute saveAttribute =
                field.GetCustomAttribute<SaveFieldAttribute>();

            if (saveAttribute != null)
            {
                description = saveAttribute.Description;
            }

            if (string.IsNullOrEmpty(description))
            {
                RuntimeEditableAttribute runtimeAttribute =
                    field.GetCustomAttribute<RuntimeEditableAttribute>();

                if (runtimeAttribute != null)
                {
                    description = runtimeAttribute.Description;
                }
            }
        }

        return new HoverInfo(name, description);
    }
}
