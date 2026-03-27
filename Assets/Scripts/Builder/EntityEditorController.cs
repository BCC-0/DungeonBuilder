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
    private bool primaryHolding;
    private bool secondaryHolding;

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
    /// Called on primary button pressed when in the background layer.
    /// Places or erases tiles depending on the current tool.
    /// </summary>
    public void OnPrimaryDown()
    {
        this.primaryHolding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Stops dragging tool if it was.
    /// </summary>
    public void OnPrimaryUp()
    {
        this.primaryHolding = false;
    }

    /// <summary>
    /// Called on secondary button pressed when in the background layer.
    /// Erases entities depending on the current tool.
    /// </summary>
    public void OnSecondaryDown()
    {
        this.secondaryHolding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Stops dragging tool if it was.
    /// </summary>
    public void OnSecondaryUp()
    {
        this.secondaryHolding = false;
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

    private void ApplyTool()
    {
        // TODO: Add applying tool for entities.
    }
}