using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls entity placement, movement, and deletion in the foreground layer.
/// </summary>
public class EntityEditorController : EditorControllerBase
{
    [SerializeField]
    private GameObject selectedPrefab;

    private Transform entityParent;

    /// <summary>
    /// Gets or sets the currently selected prefab for placement.
    /// </summary>
    public GameObject SelectedPrefab
    {
        get => this.selectedPrefab;
        set => this.selectedPrefab = value;
    }

    /// <summary>
    /// Deletes all selected entities.
    /// </summary>
    public override void OnDelete()
    {
        Debug.Log("Deleting entities");
    }

    /// <summary>
    /// Executes entity editing logic based on the resolved editor action.
    /// </summary>
    /// <param name="tool">The active editor tool.</param>
    protected override void OnApplyTool(EditorTool tool)
    {
        EditorAction action = this.GetAction(tool);

        switch (action)
        {
            case EditorAction.Paint:
                this.TryPlace();
                break;

            case EditorAction.Erase:
                this.TryErase();
                break;
        }
    }


    /// <summary>
    /// Called when the player clicks to select.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected override void OnClickSelect(Vector3 position)
    {
        float radius = 0.5f;

        var closest = FindObjectsByType<SaveableEntity>()
            .OrderBy(e => Vector3.Distance(e.transform.position, position))
            .FirstOrDefault();

        if (closest != null &&
            Vector3.Distance(closest.transform.position, position) < radius)
        {
            MapEditorManager.Instance.SelectedEntities =
                new List<SaveableEntity> { closest };
        }
        else
        {
            MapEditorManager.Instance.SelectedEntities.Clear();
        }
    }

    /// <summary>
    /// Called when the player drags to select.
    /// </summary>
    /// <param name="start">The start position of the drag selection.</param>
    /// <param name="end">The end position of the drag selection.</param>
    protected override void OnBoxSelect(Vector3 start, Vector3 end)
    {
        Rect rect = this.GetWorldRect(start, end);

        var selected = new List<SaveableEntity>();

        foreach (var entity in FindObjectsByType<SaveableEntity>())
        {
            if (rect.Contains(entity.transform.position))
            {
                selected.Add(entity);
            }
        }

        MapEditorManager.Instance.SelectedEntities = selected;
    }


    /// <summary>
    /// Initializes the entity parent container.
    /// </summary>
    private void Start()
    {
        this.entityParent = GameObject.FindWithTag("Entity parent").transform;
    }

    /// <summary>
    /// Attempts to place a new entity at the pointer position.
    /// </summary>
    private void TryPlace()
    {
        if (this.selectedPrefab == null)
        {
            return;
        }

        Vector3 snappedPos = new Vector3(
            Mathf.Floor(this.CurrentPos.x) + 0.5f,
            Mathf.Floor(this.CurrentPos.y) + 0.5f,
            0f);

        foreach (var buildEntity in BuilderRegistry.GetAll())
        {
            if (Vector3.Distance(buildEntity.transform.position, snappedPos) < 0.1f)
            {
                return;
            }
        }

        PrefabIdentity identity = this.selectedPrefab.GetComponent<PrefabIdentity>();
        if (identity == null)
        {
            return;
        }

        GameObject go = new GameObject("BuilderEntity");
        go.transform.position = snappedPos;
        go.transform.SetParent(this.entityParent);

        var builder = go.AddComponent<BuilderEntity>();
        builder.Initialize(identity.PrefabID);
    }

    /// <summary>
    /// Attempts to erase an entity near the pointer position.
    /// </summary>
    private void TryErase()
    {
        foreach (var builder in BuilderRegistry.GetAll())
        {
            if (Vector3.Distance(builder.transform.position, this.CurrentPos) < 0.5f)
            {
                Destroy(builder.gameObject);
                return;
            }
        }
    }
}