using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moving one or more entities. The position before and after is stored
/// per entity.
/// </summary>
public class MoveEntitiesAction : IUndoableAction
{
    private readonly List<Entry> entries = new ();

    private class Entry
    {
        public SaveableEntity Entity;
        public Vector3 Before;
        public Vector3 After;
    }

    public bool HasChanges
    {
        get
        {
            foreach (Entry entry in this.entries)
            {
                if (entry.Before != entry.After)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void Add(
        SaveableEntity entity,
        Vector3 before,
        Vector3 after)
    {
        this.entries.Add(new Entry
        {
            Entity = entity,
            Before = before,
            After = after,
        });
    }

    public void Undo()
    {
        foreach (Entry entry in this.entries)
        {
            Apply(entry.Entity, entry.Before);
        }
    }

    public void Redo()
    {
        foreach (Entry entry in this.entries)
        {
            Apply(entry.Entity, entry.After);
        }
    }

    private static void Apply(
        SaveableEntity entity,
        Vector3 position)
    {
        if (entity == null)
        {
            return;
        }

        entity.transform.position = position;

        BuilderEntity builderEntity = entity as BuilderEntity;

        if (builderEntity != null)
        {
            builderEntity.RefreshWallPosition();
        }
    }
}
