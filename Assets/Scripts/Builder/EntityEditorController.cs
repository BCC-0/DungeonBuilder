using UnityEngine;

/// <summary>
/// The entity editor controller controls the entities (foreground) in the map editor.
/// </summary>
public class EntityEditorController : MonoBehaviour
{
    [SerializeField]
    private GameObject selectedPrefab;
    private Vector3 currentPos;

    /// <summary>
    /// Gets or sets the currently selected prefab.
    /// </summary>
    public GameObject SelectedPrefab
    {
        get { return this.selectedPrefab; }
        set { this.selectedPrefab = value; }
    }

    private void OnEnable()
    {
        EditorEventBus.OnPointerMoved += pos => this.currentPos = pos;
        EditorEventBus.OnPrimaryDown += this.ApplyTool;
        EditorEventBus.OnDelete += this.DeleteSelected;
    }

    private void OnDisable()
    {
        EditorEventBus.OnPrimaryDown -= this.ApplyTool;
        EditorEventBus.OnDelete -= this.DeleteSelected;
    }

    private void ApplyTool()
    {
        if (MapEditorManager.Instance.CurrentLayer != EditLayer.Foreground)
        {
            return;
        }

        var tool = MapEditorManager.Instance.CurrentTool;

        if (tool == EditorTool.Brush)
        {
            Instantiate(this.SelectedPrefab, this.currentPos, Quaternion.identity);
        }

        if (tool == EditorTool.Eraser)
        {
            this.TryErase();
        }
    }

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

    private void DeleteSelected()
    {
        foreach (var e in MapEditorManager.Instance.SelectedEntities)
        {
            Destroy(e.gameObject);
        }
    }
}