using UnityEngine;

/// <summary>
/// Controls entity placement, movement, and deletion in the map editor foreground layer.
/// Works with Unity Events, no subscriptions required.
/// </summary>
public class EntityEditorController : MonoBehaviour
{
    [SerializeField] private GameObject selectedPrefab;
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
    /// Hook to PlayerInput Point action.
    /// </summary>
    /// <param name="worldPos">The world position of the pointer.</param>
    public void OnPointerMoved(Vector3 worldPos)
    {
        this.currentPos = worldPos;
    }

    /// <summary>
    /// Called when the primary button/touch is pressed.
    /// Places or erases entities depending on the current tool.
    /// Hook to PlayerInput Primary action started event.
    /// </summary>
    public void OnPrimaryDown()
    {
        if (MapEditorManager.Instance.CurrentLayer != EditLayer.Foreground) return;

        var tool = MapEditorManager.Instance.CurrentTool;

        if (tool == EditorTool.Brush && this.SelectedPrefab != null)
        {
            Instantiate(this.SelectedPrefab, this.currentPos, Quaternion.identity);
        }

        if (tool == EditorTool.Eraser)
        {
            this.TryErase();
        }
    }

    /// <summary>
    /// Deletes all selected entities.
    /// Hook to PlayerInput Delete action.
    /// </summary>
    public void OnDelete()
    {
        foreach (var entity in MapEditorManager.Instance.SelectedEntities)
        {
            Destroy(entity.gameObject);
        }
    }

    /// <summary>
    /// Attempts to erase an entity under the pointer.
    /// </summary>
    private void TryErase()
    {
        RaycastHit2D hit = Physics2D.Raycast(this.currentPos, Vector2.zero);
        if (hit.collider == null)
        {
            return;
        }

        var entity = hit.collider.GetComponent<SaveableEntity>();
        if (entity != null)
        {
            Destroy(entity.gameObject);
        }
    }
}