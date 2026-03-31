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

    private void ApplyTool()
    {
        Debug.Log("Foreground triggered.");
        EditorTool tool = MapEditorManager.Instance.CurrentTool;

        switch (tool)
        {
            case EditorTool.Brush:
                if (this.primaryHolding)
                {
                    this.TryPlace();
                }
                else if (this.secondaryHolding)
                {
                    this.TryErase();
                }

                break;

            case EditorTool.Eraser:
                if (this.primaryHolding || this.secondaryHolding)
                {
                    this.TryErase();
                }

                break;
        }
    }

    private void TryPlace()
    {
        Debug.Log("Trying to place 1.");
        if (this.selectedPrefab == null)
        {
            return;
        }

        Vector3 snappedPos = new Vector3(
            Mathf.Floor(this.currentPos.x) + 0.5f,
            Mathf.Floor(this.currentPos.y) + 0.5f,
            0f);

        foreach (var buildEntity in BuilderRegistry.GetAll())
        {
            if (Vector3.Distance(buildEntity.transform.position, snappedPos) < 0.1f)
            {
                return;
            }
        }

        Debug.Log("Trying to place 2.");
        PrefabIdentity identity = this.selectedPrefab.GetComponent<PrefabIdentity>();
        if (identity == null)
        {
            Debug.LogWarning("Selected prefab does not have a PrefabIdentity component.");
            return;
        }

        GameObject go = new GameObject("BuilderEntity");
        go.transform.position = snappedPos;
        var builder = go.AddComponent<BuilderEntity>();
        builder.Initialize(identity.PrefabID);
    }

    private void TryErase()
    {
        foreach (var builder in BuilderRegistry.GetAll())
        {
            if (Vector3.Distance(builder.transform.position, this.currentPos) < 0.5f)
            {
                Destroy(builder.gameObject);
                return;
            }
        }
    }

    private void Update()
    {
        if (this.primaryHolding || this.secondaryHolding)
        {
            this.ApplyTool();
        }
    }
}