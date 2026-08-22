using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Selection manager for entities.
/// </summary>
public class EntitySelectionManager : SelectionManagerBase
{
    /// <summary>
    /// Max distance from a click position for an entity to be considered "under" it.
    /// </summary>
    [SerializeField]
    private float entitySelectRadius = 0.5f;

    /// <summary>
    /// Visualizer that outlines the current entity selection.
    /// </summary>
    private EntitySelectionVisualizer selectionVisualizer;

    /// <summary>
    /// Cell positions of the selected entities when movement started.
    /// </summary>
    private Dictionary<SaveableEntity, Vector2Int> moveStartCells = new ();

    /// <summary>
    /// Cell positions of the selected entities when movement is continued on mobile.
    /// </summary>
    private Dictionary<SaveableEntity, Vector2Int> extraMoveStartCells = new ();

    /// <summary>
    /// Cell under the pointer when movement started.
    /// </summary>
    private Vector2Int moveStartPointerCell;

    /// <summary>
    /// Prevents confirming immediately on the same frame movement starts.
    /// </summary>
    private bool startedMovingThisFrame;

    /// <inheritdoc/>
    public override void ClearSelection()
    {
        this.SetSelection(new List<SaveableEntity>());
    }

    /// <inheritdoc/>
    public override void DeleteSelected()
    {
        foreach (SaveableEntity entity in MapEditorManager.Instance.SelectedEntities)
        {
            Destroy(entity.gameObject);
        }

        this.ClearSelection();
    }

    /// <inheritdoc/>
    public override void MoveSelected()
    {
        if (this.IsMovementMode)
        {
            return;
        }

        this.startedMovingThisFrame = true;

        List<SaveableEntity> selectedEntities = MapEditorManager.Instance.SelectedEntities;

        if (selectedEntities == null || selectedEntities.Count == 0)
        {
            return;
        }

        this.IsMovementMode = true;
        this.IsMoving = true;

        this.moveStartCells.Clear();

        this.moveStartPointerCell = (Vector2Int)this.Grid.WorldToCell(this.CurrentPos);

        foreach (SaveableEntity entity in selectedEntities)
        {
            Vector2Int cell = (Vector2Int)this.Grid.WorldToCell(entity.transform.position);
            this.moveStartCells[entity] = cell;
        }

        this.extraMoveStartCells = new Dictionary<SaveableEntity, Vector2Int>(this.moveStartCells);
        this.DisableSelectionButtons();
        this.EnableMoveButtons(this.GetBoundingRect(selectedEntities.Select(e => (Vector2)e.transform.position)));
    }

    /// <summary>
    /// Updates the preview position of the selected entities based on the pointer position.
    /// </summary>
    public void Update()
    {
        if (!this.IsMovementMode || !this.IsMoving)
        {
            return;
        }

        if (this.startedMovingThisFrame)
        {
            this.startedMovingThisFrame = false;
            return;
        }

        Vector2Int currentPointerCell = (Vector2Int)this.Grid.WorldToCell(this.CurrentPos);

        Vector2Int cellDelta = currentPointerCell - this.moveStartPointerCell;

        foreach (KeyValuePair<SaveableEntity, Vector2Int> entry in this.extraMoveStartCells)
        {
            SaveableEntity entity = entry.Key;

            if (entity == null)
            {
                continue;
            }

            Vector3Int previewCell = (Vector3Int)(entry.Value + cellDelta);

            entity.transform.position = this.Grid.GetCellCenterWorld(previewCell);
        }

        this.selectionVisualizer.Refresh();
    }

    /// <inheritdoc/>
    public override void ConfirmMovingSelected()
    {
        if (this.startedMovingThisFrame)
        {
            this.startedMovingThisFrame = false;
            return;
        }

        if (!this.IsMovementMode)
        {
            return;
        }

        List<SaveableEntity> selectedEntities =
            MapEditorManager.Instance.SelectedEntities;

        HashSet<SaveableEntity> movingEntities =
            new HashSet<SaveableEntity>(selectedEntities);

        foreach (KeyValuePair<SaveableEntity, Vector2Int> entry in this.extraMoveStartCells)
        {
            SaveableEntity entity = entry.Key;

            if (entity == null)
            {
                continue;
            }

            Vector2Int finalCell = (Vector2Int)this.Grid.WorldToCell(entity.transform.position);

            SaveableEntity entityAtDestination =
                FindObjectsByType<SaveableEntity>()
                    .FirstOrDefault(other =>
                        other != entity &&
                        !movingEntities.Contains(other) &&
                        (Vector2Int)this.Grid.WorldToCell(other.transform.position) == finalCell);

            if (entityAtDestination != null)
            {
                Destroy(entityAtDestination.gameObject);
            }
        }

        this.IsMovementMode = false;
        this.IsMoving = false;
        this.moveStartCells.Clear();
        this.extraMoveStartCells.Clear();

        this.DisableMoveButtons();
        this.ClearSelection();
    }

    /// <inheritdoc/>
    public override void CancelMovingSelected()
    {
        if (!this.IsMovementMode)
        {
            return;
        }

        foreach (KeyValuePair<SaveableEntity, Vector2Int> entry in this.moveStartCells)
        {
            SaveableEntity entity = entry.Key;

            if (entity == null)
            {
                continue;
            }

            entity.transform.position = this.Grid.GetCellCenterWorld((Vector3Int)entry.Value);
        }

        this.IsMovementMode = false;
        this.IsMoving = false;
        this.moveStartCells.Clear();

        this.DisableMoveButtons();
        this.ClearSelection();
    }

    /// <inheritdoc/>
    protected override void ContinueMove(Vector2 worldPos)
    {
        if (!this.IsMovementMode)
        {
            return;
        }

        this.moveStartPointerCell = (Vector2Int)this.Grid.WorldToCell(worldPos);

        foreach (SaveableEntity entity in this.extraMoveStartCells.Keys.ToList())
        {
            if (entity == null)
            {
                continue;
            }

            this.extraMoveStartCells[entity] = (Vector2Int)this.Grid.WorldToCell(entity.transform.position);
        }

        this.CurrentPos = worldPos;
        this.startedMovingThisFrame = true;
        this.IsMoving = true;
    }

    /// <summary>
    /// Selects the closest entity within range of the click, if any.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected override void OnClickSelect(Vector2 position)
    {
        SaveableEntity closestEntity = FindObjectsByType<SaveableEntity>()
            .OrderBy(e => Vector2.Distance(e.transform.position, position))
            .FirstOrDefault();

        if (closestEntity != null &&
            Vector2.Distance(closestEntity.transform.position, position) <= this.entitySelectRadius)
        {
            this.SetSelection(new List<SaveableEntity> { closestEntity });
        }
        else
        {
            this.ClearSelection();
        }
    }

    /// <summary>
    /// Selects every entity whose position falls within the dragged rectangle.
    /// </summary>
    /// <param name="rect">The world-space rectangle of the drag.</param>
    protected override void OnBoxSelect(Rect rect)
    {
        List<SaveableEntity> selected = FindObjectsByType<SaveableEntity>()
            .Where(e =>
                e.GetComponent<Tilemap>() == null &&
                rect.Contains(e.transform.position))
            .ToList();

        this.SetSelection(selected);
    }

    /// <summary>
    /// Finds the correct visualizer.
    /// </summary>
    protected override void Awake()
    {
        this.selectionVisualizer = FindAnyObjectByType<EntitySelectionVisualizer>();
        base.Awake();
    }

    /// <summary>
    /// Writes the given selection to <see cref="MapEditorManager"/> and refreshes
    /// the visualizer.
    /// </summary>
    /// <param name="entities">The entities to select.</param>
    private void SetSelection(List<SaveableEntity> entities)
    {
        MapEditorManager.Instance.SelectedEntities = entities;
        this.selectionVisualizer.Refresh();

        if (entities == null || entities.Count == 0)
        {
            this.DisableSelectionButtons();
        }
        else
        {
            this.EnableSelectionButtons(this.GetBoundingRect(entities.Select(e => (Vector2)e.transform.position)));
        }
    }
}
