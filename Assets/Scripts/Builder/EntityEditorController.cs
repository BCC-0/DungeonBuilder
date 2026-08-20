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
    /// Initializes the entity parent container and sets the correct selection manager.
    /// </summary>
    private void Start()
    {
        this.entityParent = GameObject.FindWithTag("Entity parent").transform;
        this.SelectionManager = FindAnyObjectByType<EntitySelectionManager>();
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

        foreach (BuilderEntity buildEntity in BuilderRegistry.GetAll())
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

        GameObject go = new GameObject(identity.PrefabID);
        go.transform.position = snappedPos;
        go.transform.SetParent(this.entityParent);

        BuilderEntity builder = go.AddComponent<BuilderEntity>();
        builder.Initialize(identity.PrefabID);
    }

    /// <summary>
    /// Attempts to erase an entity near the pointer position.
    /// </summary>
    private void TryErase()
    {
        foreach (BuilderEntity builder in BuilderRegistry.GetAll())
        {
            if (Vector3.Distance(builder.transform.position, this.CurrentPos) < 0.5f)
            {
                Destroy(builder.gameObject);
                return;
            }
        }
    }
}