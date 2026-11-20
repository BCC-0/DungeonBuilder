using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placing or deleting entities. Entities are never destroyed by this action.
/// They are deactivated ("soft deleted") so every reference to them stays valid
/// and undo can simply activate them again.
/// </summary>
public class EntityPresenceAction : IUndoableAction
{
    private readonly List<GameObject> objects;
    private readonly bool presentAfterRedo;

    private EntityPresenceAction(
        List<GameObject> objects,
        bool presentAfterRedo)
    {
        this.objects = objects;
        this.presentAfterRedo = presentAfterRedo;
    }

    /// <summary>
    /// Creates an action for an entity that was just placed.
    /// </summary>
    /// <param name="entity">The placed entity.</param>
    /// <returns>The action, ready to register.</returns>
    public static EntityPresenceAction Placed(SaveableEntity entity)
    {
        return new EntityPresenceAction(
            new List<GameObject> { entity.gameObject },
            presentAfterRedo: true);
    }

    /// <summary>
    /// Deletes the entities now (soft delete), together with every entity they
    /// consume, and returns the action to register.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <returns>The action, ready to register.</returns>
    public static EntityPresenceAction DeleteNow(
        IEnumerable<SaveableEntity> entities)
    {
        List<GameObject> objects = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        foreach (SaveableEntity entity in entities)
        {
            Collect(entity, objects, seen);
        }

        EntityPresenceAction action =
            new EntityPresenceAction(objects, presentAfterRedo: false);

        action.SetActive(false);

        return action;
    }

    /// <inheritdoc/>
    public void Undo()
    {
        this.SetActive(!this.presentAfterRedo);
    }

    /// <inheritdoc/>
    public void Redo()
    {
        this.SetActive(this.presentAfterRedo);
    }

    private static void Collect(
        SaveableEntity entity,
        List<GameObject> result,
        HashSet<GameObject> seen)
    {
        if (entity == null ||
            !seen.Add(entity.gameObject))
        {
            return;
        }

        result.Add(entity.gameObject);

        // A deleted enemy body must take its consumed head with it.
        foreach (BuilderEntity other in BuilderRegistry.GetAll())
        {
            if (other != null &&
                other.ConsumedBy == entity)
            {
                Collect(other, result, seen);
            }
        }
    }

    private void SetActive(bool active)
    {
        foreach (GameObject go in this.objects)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
