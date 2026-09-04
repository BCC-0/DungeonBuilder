using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A wrapper for an item the player can pick up.
/// </summary>
public class ItemObject : SaveableEntity
{
    [SerializeField]
    private Item originalItem;

    [Header("Runtime Copy (Editable)")]
    [SerializeField]
    private Item runtimeItem;

    /// <summary>
    /// Gets the runtime item this object contains.
    /// </summary>
    public Item Item => this.runtimeItem;

    /// <summary>
    /// Sets the item of this object.
    /// </summary>
    /// <param name="item">A copy of a scriptable item object. Don't ever use the scriptable object itself.</param>
    public void SetItem(Item item)
    {
#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(item))
        {
            Debug.LogError("You cannot assign the asset directly. Use a runtime copy instead!");
            return;
        }
#endif
        this.runtimeItem = item;

        if (this.SpriteRenderer != null && this.runtimeItem != null)
        {
            this.SpriteRenderer.sprite = this.runtimeItem.Icon;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Creates a copy of the scriptable object for editing in the unity editor.
    /// </summary>
    [ContextMenu("Create Runtime Copy")]
    public void CreateRuntimeCopy()
    {
        if (this.originalItem != null)
        {
            this.runtimeItem = ScriptableObject.Instantiate(this.originalItem);
            this.runtimeItem.name = this.originalItem.name + "_RuntimeCopy";
            this.SpriteRenderer.sprite = this.runtimeItem.Icon;
        }
    }
#endif

    /// <summary>
    /// Writes the item's definition ID and its runtime-editable fields.
    /// </summary>
    /// <param name="writer">The writer which will save the fields.</param>
    public override void Write(BinaryWriter writer)
    {
        base.Write(writer);

        writer.Write(this.runtimeItem.ItemID);

        this.runtimeItem.WriteRuntimeFields(writer);
    }

    /// <summary>
    /// Reads the item's definition ID and restores its runtime-editable fields.
    /// </summary>
    /// <param name="reader">The reader which will read the fields to restore.</param>
    public override void Read(BinaryReader reader)
    {
        base.Read(reader);

        string itemID = reader.ReadString();
        Debug.Log($"Read item ID: '{itemID}'");
        Item def = ItemLibrary.GetItemByIDGlobal(itemID);

        if (def == null)
        {
            Debug.LogError($"Could not find Item with ID '{itemID}'.");
            return;
        }

        Item copy = Instantiate(def);
        copy.name = def.name + "_RuntimeCopy";
        copy.ReadRuntimeFields(reader);

        this.originalItem = def;
        this.SetItem(copy);
    }

    /// <summary>
    /// Creates a runtime copy if it doesnt exist yet.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        if (this.originalItem == null)
        {
            Debug.LogError($"ItemObject on {this.gameObject.name} has no Item assigned!");
            return;
        }

        if (this.runtimeItem != null)
        {
            this.SpriteRenderer.sprite = this.runtimeItem.Icon;
            return;
        }

        Item runtimeCopy = Instantiate(this.originalItem);

#if UNITY_EDITOR
        runtimeCopy.name = this.originalItem.name + "_RuntimeCopy";
#endif

        this.SetItem(runtimeCopy);
    }

    private void OnDestroy()
    {
        if (this.runtimeItem != null)
        {
            Destroy(this.runtimeItem);
        }
    }
}