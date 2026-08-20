using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies outline visuals to the currently selected entities.
/// </summary>
public class EntitySelectionVisualizer : MonoBehaviour
{
    private readonly List<SaveableEntity> highlightedEntities = new List<SaveableEntity>();

    /// <summary>
    /// Clears existing outlines and re-applies them based on the current
    /// contents of <see cref="MapEditorManager.SelectedEntities"/>.
    /// </summary>
    public void Refresh()
    {
        this.Clear();

        foreach (SaveableEntity entity in MapEditorManager.Instance.SelectedEntities)
        {
            if (entity == null)
            {
                continue;
            }

            this.SetEntityOutline(entity, true);
            this.highlightedEntities.Add(entity);
        }
    }

    /// <summary>
    /// Clears all current outlines without applying new ones.
    /// </summary>
    public void Clear()
    {
        foreach (SaveableEntity entity in this.highlightedEntities)
        {
            if (entity != null)
            {
                this.SetEntityOutline(entity, false);
            }
        }

        this.highlightedEntities.Clear();
    }

    /// <summary>
    /// Enables or disables the outline on a single entity.
    /// </summary>
    /// <param name="entity">The entity to outline.</param>
    /// <param name="enabled">Whether the outline should be visible.</param>
    private void SetEntityOutline(SaveableEntity entity, bool enabled)
    {
        EntitySelectionOutline outline = entity.GetComponentInChildren<EntitySelectionOutline>();
        if (outline != null)
        {
            if (enabled)
            {
                outline.ShowSelection();
                return;
            }

            outline.HideSelection();
        }
    }
}
