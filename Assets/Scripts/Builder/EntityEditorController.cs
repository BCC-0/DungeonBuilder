using UnityEngine;

/// <summary>
/// Controls entity placement, movement, and deletion in the map editor foreground layer.
/// Works with Unity Events, no subscriptions required.
/// </summary>
public class EntityEditorController : EditorControllerBase
{
    [SerializeField]
    private GameObject selectedPrefab;

    private Transform entityParent;

    /// <summary>
    /// Gets or sets the currently selected prefab for placing entities.
    /// </summary>
    public GameObject SelectedPrefab
    {
        get => this.selectedPrefab;
        set => this.selectedPrefab = value;
    }

    /// <summary>
    /// Deletes all selected entities.
    /// </summary>
    public void OnDelete()
    {
        Debug.Log("Deleting entities");

        // TODO: implement bulk deletion logic
    }

    /// <summary>
    /// Executes entity editing logic based on the active tool.
    /// </summary>
    /// <param name="tool">The current editor tool.</param>
    protected override void OnApplyTool(EditorTool tool)
    {
        switch (tool)
        {
            case EditorTool.Brush:
                if (this.PrimaryHolding)
                {
                    this.TryPlace();
                }
                else if (this.SecondaryHolding)
                {
                    this.TryErase();
                }

                break;

            case EditorTool.Eraser:
                if (this.PrimaryHolding || this.SecondaryHolding)
                {
                    this.TryErase();
                }

                break;
        }
    }

    /// <summary>
    /// Finds the entity parent container in the scene.
    /// </summary>
    private void Start()
    {
        this.entityParent = GameObject.FindWithTag("Entity parent").transform;
    }

    /// <summary>
    /// Attempts to place a new entity at the current pointer position.
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