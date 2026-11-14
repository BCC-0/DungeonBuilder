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

            if (entity is BuilderEntity builderEntity)
            {
                BuilderEntity head = builderEntity.GetConsumedHead();

                if (head != null)
                {
                    this.SetHeadOutline(builderEntity, head, true);
                }
            }
        }
    }

    /// <summary>
    /// Clears all current outlines without applying new ones.
    /// </summary>
    public void Clear()
    {
        foreach (SaveableEntity entity in this.highlightedEntities)
        {
            if (entity == null)
            {
                continue;
            }

            this.SetEntityOutline(entity, false);

            if (entity is BuilderEntity body)
            {
                BuilderEntity head = body.GetConsumedHead();

                if (head != null)
                {
                    this.SetHeadOutline(body, head, false);
                }
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
        EntitySelectionOutline outline = this.FindOwnOutline(entity);
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

    /// <summary>
    /// Finds the entity's own outline, ignoring any outline that was
    /// re-parented under the body's HeadRenderer.
    /// </summary>
    /// <param name="entity">The entity to find the outline for.</param>
    /// <returns>The entity's own outline, or null when there is none.</returns>
    private EntitySelectionOutline FindOwnOutline(SaveableEntity entity)
    {
        foreach (EntitySelectionOutline candidate in
                 entity.GetComponentsInChildren<EntitySelectionOutline>(true))
        {
            if (candidate.transform.parent != null &&
                candidate.transform.parent.name == "HeadRenderer")
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    /// <summary>
    /// Enables or disables the selection outline on a consumed head.
    /// The outline is parented to the body's HeadRenderer and follows its transform.
    /// </summary>
    /// <param name="body">The enemy body displaying the head.</param>
    /// <param name="head">The consumed head.</param>
    /// <param name="enabled">Whether the outline should be visible.</param>
    private void SetHeadOutline(BuilderEntity body, BuilderEntity head, bool enabled)
    {
        Transform headRenderer = body.transform.Find("HeadRenderer");

        if (headRenderer == null)
        {
            return;
        }

        // The outline is moved under the HeadRenderer on first use, so look
        // there first and fall back to the head itself.
        EntitySelectionOutline outline =
            headRenderer.GetComponentInChildren<EntitySelectionOutline>(true);

        if (outline == null)
        {
            outline = head.GetComponentInChildren<EntitySelectionOutline>(true);
        }

        if (outline == null)
        {
            return;
        }

        outline.transform.SetParent(headRenderer, false);
        outline.transform.localPosition = Vector3.zero;

        if (enabled)
        {
            outline.ShowSelection();
        }
        else
        {
            outline.HideSelection();
        }
    }
}