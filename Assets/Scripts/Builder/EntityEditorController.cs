using UnityEngine;

/// <summary>
/// Controls entity placement, movement, and deletion in the map editor foreground layer.
/// Works with Unity Events, no subscriptions required.
/// </summary>
public class EntityEditorController : MonoBehaviour
{
    [SerializeField]
    private GameObject selectedPrefab;
    private Vector3 currentPos;

    /// <summary>
    /// Gets or sets the currently selected prefab for painting entities.
    /// </summary>
    public GameObject SelectedPrefab
    {
        get => this.selectedPrefab;
        set => this.selectedPrefab = value;
    }

    /// <summary>
    /// Updates the current pointer position (screen-to-world) for placing entities.
    /// </summary>
    /// <param name="worldPos">The world position of the pointer.</param>
    public void OnPointerMoved(Vector3 worldPos)
    {
        this.currentPos = worldPos;
    }

    /// <summary>
    /// // TODO: Add primary up.
    /// </summary>
    public void OnPrimaryUp()
    {
        Debug.Log("Primary up entity editor.");
        // TODO: If in placement mode:
        // TODO: Stop placing entities when dragging.
    }

    /// <summary>
    /// Called on primary button pressed when in the foreground layer.
    /// Places or erases entities depending on the current tool.
    /// </summary>
    public void OnPrimaryDown()
    {
        Debug.Log("Primary down entity editor.");
        // TODO: If in placement mode:
        // Place one entity or multiple when dragging.

        // TODO: If in erasing mode:
        // Erase one entity or multiple when dragging.

        // TODO: If in editing mode:
        // Edit the entity we are currently pointing at (if any).
        // If this + shift, select multiple enemies and allow multi-edits.

    }

    /// <summary>
    /// Deletes all selected entities.
    /// </summary>
    public void OnDelete()
    {
        Debug.Log("Deleting entities");
        // TODO
    }

    /// <summary>
    /// Attempts to erase an entity under the pointer.
    /// </summary>
    private void TryErase()
    {
        Debug.Log("Deleting 1 entity");
        // TODO
    }
}