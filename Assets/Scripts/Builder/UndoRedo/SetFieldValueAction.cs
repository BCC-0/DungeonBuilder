using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Changing one field on one or more targets. The value before the change is
/// stored per target, so undoing a multi-selection edit restores each
/// target's own old value.
/// </summary>
public class SetFieldValueAction : IUndoableAction, IMergeableAction
{
    private const float MergeWindowSeconds = 0.75f;

    private readonly string fieldName;
    private readonly List<Entry> entries = new ();
    private readonly Action<object, object> setter;
    private readonly Action onApplied;
    private readonly bool allowMerge;

    private float lastChangeTime;

    private class Entry
    {
        public object Target;
        public object Before;
        public object After;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetFieldValueAction"/> class.
    /// </summary>
    /// <param name="fieldName">Identifies the field, used to decide whether two actions can merge.</param>
    /// <param name="setter">Writes a value to a target: (target, value).</param>
    /// <param name="onApplied">Optional callback after undo or redo, for example to rebuild the inspector.</param>
    /// <param name="allowMerge">True for continuous input such as sliders, so quick successive changes become one step.</param>
    public SetFieldValueAction(
        string fieldName,
        Action<object, object> setter,
        Action onApplied = null,
        bool allowMerge = false)
    {
        this.fieldName = fieldName;
        this.setter = setter;
        this.onApplied = onApplied;
        this.allowMerge = allowMerge;
        this.lastChangeTime = Time.unscaledTime;
    }

    /// <summary>
    /// Gets a value indicating whether any target actually changed value.
    /// </summary>
    public bool HasChanges
    {
        get
        {
            foreach (Entry entry in this.entries)
            {
                if (!Equals(entry.Before, entry.After))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Records the change of one target. Read the before value
    /// before writing the new value.
    /// </summary>
    /// <param name="target">The object that was changed.</param>
    /// <param name="before">The value of the target before the change.</param>
    /// <param name="after">The value of the target after the change.</param>
    public void Add(object target, object before, object after)
    {
        this.entries.Add(new Entry
        {
            Target = target,
            Before = before,
            After = after,
        });
    }

    /// <inheritdoc/>
    public void Undo()
    {
        foreach (Entry entry in this.entries)
        {
            if (IsDestroyed(entry.Target))
            {
                continue;
            }

            this.setter(entry.Target, entry.Before);
        }

        this.onApplied?.Invoke();
    }

    /// <inheritdoc/>
    public void Redo()
    {
        foreach (Entry entry in this.entries)
        {
            if (IsDestroyed(entry.Target))
            {
                continue;
            }

            this.setter(entry.Target, entry.After);
        }

        this.onApplied?.Invoke();
    }

    /// <inheritdoc/>
    public bool TryMerge(IUndoableAction next)
    {
        if (!this.allowMerge ||
            next is not SetFieldValueAction other ||
            !other.allowMerge ||
            other.fieldName != this.fieldName ||
            other.entries.Count != this.entries.Count ||
            other.lastChangeTime - this.lastChangeTime > MergeWindowSeconds)
        {
            return false;
        }

        // Same targets in the same order are required.
        for (int i = 0; i < this.entries.Count; i++)
        {
            if (!ReferenceEquals(this.entries[i].Target, other.entries[i].Target))
            {
                return false;
            }
        }

        // Keep our "before", take the newest "after".
        for (int i = 0; i < this.entries.Count; i++)
        {
            this.entries[i].After = other.entries[i].After;
        }

        this.lastChangeTime = other.lastChangeTime;
        return true;
    }

    private static bool IsDestroyed(object target)
    {
        return target is UnityEngine.Object unityObject &&
               unityObject == null;
    }
}
