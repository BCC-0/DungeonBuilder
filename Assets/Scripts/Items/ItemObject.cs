using UnityEditor;
using UnityEngine;

/// <summary>
/// A wrapper for an item the player can pick up.
/// </summary>
public class ItemObject : MonoBehaviour
{
    [SerializeField]
    private Item originalItem;

    [Header("Runtime Copy (Editable)")]
    [SerializeField] private Item runtimeItem;
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Gets the runtime item this object contains.
    /// </summary>
    public Item Item => this.runtimeItem;

    /// <summary>
    /// sets the item of this object.
    /// </summary>
    /// <param name="item">A copy of a scriptable item object. Don't ever use the scriptable object iself.</param>
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

        if (this.spriteRenderer != null && this.runtimeItem != null)
        {
            this.spriteRenderer.sprite = this.runtimeItem.Icon;
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

            // Update sprite if available
            if (this.spriteRenderer == null)
            {
                this.spriteRenderer = this.GetComponent<SpriteRenderer>();
            }

            this.spriteRenderer.sprite = this.runtimeItem.Icon;
        }
    }
#endif

    private void Awake()
    {
        if (this.originalItem == null)
        {
            Debug.LogError($"ItemObject on {this.gameObject.name} has no Item assigned!");
            return;
        }

        // Initialize SpriteRenderer first
        this.spriteRenderer = this.GetComponent<SpriteRenderer>();

        if (this.runtimeItem != null)
        {
            return;
        }

        // Create a runtime copy of the original item
        Item runtimeCopy = ScriptableObject.Instantiate(this.originalItem);

#if UNITY_EDITOR
        // Optional: rename in editor for clarity
        runtimeCopy.name = this.originalItem.name + "_RuntimeCopy";
#endif

        // Assign the runtime copy
        this.SetItem(runtimeCopy);
    }
}
