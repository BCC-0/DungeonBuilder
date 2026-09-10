using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Defines the kind of item.
/// We currently have 3 item types.
/// </summary>
public enum ItemType
{
    /// <summary>
    /// The weapon item type is used with left click when equipped.
    /// </summary>
    Weapon,

    /// <summary>
    /// The tool item type is used with right click when equipped.
    /// </summary>
    Tool,

    /// <summary>
    /// The consumable type is used immediately when picked up.
    /// </summary>
    Consumable,
}

/// <summary>
/// The item base class.
/// </summary>
public abstract class Item : ScriptableObject
{
    [SerializeField]
    private string itemID;

    [Header("Basic Item Info")]
    [SerializeField]
    private string itemName;
    [SerializeField]
    private Sprite icon;
    [TextArea]
    [SerializeField]
    private string description;
    [SerializeField]
    private GameObject prefab;

    [Header("Item Classification")]
    [SerializeField]
    private ItemType itemType;

    [Header("Runtime Handling")]
    [SerializeField]
    [HideInInspector]
    private string uniqueID;

    /// <summary>
    /// Gets the ID of this item.
    /// </summary>
    public string ItemID => this.itemID;

    /// <summary>
    /// Gets the name of the item.
    /// </summary>
    public string ItemName => this.itemName;

    /// <summary>
    /// Gets the icon of this item.
    /// </summary>
    public Sprite Icon => this.icon;

    /// <summary>
    /// Gets the description of this item.
    /// </summary>
    public string Description => this.description;

    /// <summary>
    /// Gets the prefab of this item.
    /// This stores the item itself and all parts of the animation when used.
    /// Replace the players WeaponHolder object with this if we pick up a weapon. (Same for tools).
    /// </summary>
    public GameObject Prefab => this.prefab;

    /// <summary>
    /// Gets the item type.
    /// </summary>
    public ItemType ItemType => this.itemType;

    /// <summary>
    /// Gets the definition ID used to look this item up in the ItemDatabase on load.
    /// </summary>
    public string UniqueID => this.uniqueID;

    /// <summary>
    /// The action when the item is used.
    /// Must be overridden by subclasses.
    /// Should be called immediately for Consumable types.
    /// </summary>
    /// <param name="playerHandler">The player handler to use the item.</param>
    /// <param name="playerData">The player data to use the item.</param>
    public abstract void Use(CrawlerPlayerHandler playerHandler, CrawlerPlayerData playerData);

    /// <summary>
    /// Optional method to check if the item can be used currently.
    /// Can be overridden by subclasses to add cooldowns, durability checks, etc.
    /// </summary>
    /// <returns>Whether the player can use this item.</returns>
    public virtual bool CanUse()
    {
        return true;
    }

    /// <summary>
    /// Checks if this item is considered a duplicate of another.
    /// Override in subclasses to compare relevant fields.
    /// </summary>
    /// <param name="other">The item to compare with.</param>
    /// <returns>Whether the items are duplicate.</returns>
    public virtual bool IsDuplicate(Item other)
    {
        if (other == null)
        {
            return false;
        }

        return this.itemName == other.itemName && this.itemType == other.itemType;

        // Basic check, override for comparing specific stats!
    }

    /// <summary>
    /// Helper method to get display info for UI.
    /// Also useful for debugging.
    /// </summary>
    /// <returns>Returns the item and description.</returns>
    public virtual string GetDisplayInfo()
    {
        return $"{this.itemName}\n{this.description}";
    }

    /// <summary>
    /// Gets the description for all user-editable fields.
    /// </summary>
    /// <returns>A string with the descriptions.</returns>
    public virtual string GetRuntimeEditableDescription()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(this.GetDisplayInfo()); // existing name/description

        foreach (FieldInfo field in this.GetRuntimeEditableFields())
        {
            object value = field.GetValue(this);
            sb.AppendLine($"{field.Name}: {value}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Writes all [RuntimeEditable] fields on this item and its base types.
    /// </summary>
    /// <param name="writer">The writer which will save the fields.</param>
    public virtual void WriteRuntimeFields(BinaryWriter writer)
    {
        FieldInfo[] fields = this.GetRuntimeEditableFields();
        writer.Write(fields.Length);

        foreach (FieldInfo field in fields)
        {
            writer.Write(field.Name);
            this.WriteFieldValue(writer, field.FieldType, field.GetValue(this));
        }
    }

    /// <summary>
    /// Reads all [RuntimeEditable] fields on this item and its base types.
    /// </summary>
    /// <param name="reader">The reader which will read the fields to restore.</param>
    public virtual void ReadRuntimeFields(BinaryReader reader)
    {
        int count = reader.ReadInt32();

        Dictionary<string, FieldInfo> map = new ();
        foreach (FieldInfo f in this.GetRuntimeEditableFields())
        {
            map[f.Name] = f;
        }

        for (int i = 0; i < count; i++)
        {
            string name = reader.ReadString();
            FieldInfo field = map.TryGetValue(name, out FieldInfo f) ? f : null;
            object value = this.ReadFieldValue(reader, field?.FieldType);

            if (field != null)
            {
                field.SetValue(this, value);
            }
        }
    }

    private FieldInfo[] GetRuntimeEditableFields()
    {
        List<FieldInfo> result = new();
        Type type = this.GetType();

        while (type != null && type != typeof(ScriptableObject))
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in fields)
            {
                if (field.GetCustomAttribute<RuntimeEditableAttribute>() != null)
                {
                    result.Add(field);
                }
            }

            type = type.BaseType;
        }

        return result.ToArray();
    }

    private void WriteFieldValue(BinaryWriter writer, Type type, object value)
    {
        if (type == typeof(float))
        {
            writer.Write((float)value);
        }
        else if (type == typeof(int))
        {
            writer.Write((int)value);
        }
        else if (type == typeof(bool))
        {
            writer.Write((bool)value);
        }
        else if (type == typeof(string))
        {
            writer.Write((string)value ?? string.Empty);
        }
        else
        {
            Debug.LogError($"Unsupported RuntimeEditable type: {type}");
        }
    }

    private object ReadFieldValue(BinaryReader reader, Type type)
    {
        // Always read something to keep the stream aligned, even if the field no longer exists.
        if (type == typeof(float) || type == null)
        {
            return reader.ReadSingle();
        }

        if (type == typeof(int))
        {
            return reader.ReadInt32();
        }

        if (type == typeof(bool))
        {
            return reader.ReadBoolean();
        }

        if (type == typeof(string))
        {
            return reader.ReadString();
        }

        Debug.LogError($"Unsupported RuntimeEditable type: {type}");
        return null;
    }
}