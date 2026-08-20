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
            Vector2.Distance(closestEntity.transform.position, position) <= EntitySelectRadius)
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
    }
}
