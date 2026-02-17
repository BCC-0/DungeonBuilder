using System;
using System.Reflection;
using System.Text;
using System.Xml;
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
    /// /// <summary>
    public ItemType ItemType => this.itemType;

    /// <summary>
    /// /// The action when the item is used.
    /// Must be overridden by subclasses.
    /// Should be called immediately for Consumable types.
    /// </summary>
    /// <param name="playerData">The player to use the item.</param>
    public abstract void Use(CrawlerPlayerData playerData);

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

        // Include all [RuntimeEditable] fields
        var type = this.GetType();
        while (type != null && type != typeof(ScriptableObject))
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {
                // Only include fields marked as RuntimeEditable
                if (field.GetCustomAttribute<RuntimeEditableAttribute>() != null)
                {
                    object value = field.GetValue(this);
                    sb.AppendLine($"{field.Name}: {value}");
                }
            }

            type = type.BaseType;
        }

        return sb.ToString();
    }
}
