using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// A copy of everything that reference editing can change: which entity
/// consumes which, and the entity references stored in every builder entity.
/// Taking one snapshot before and one after an edit makes the edit undoable,
/// including the side effects of consuming (references removed from other
/// entities, the world presence of consumed entities).
/// </summary>
public class ReferenceSnapshot
{
    private readonly List<EntityState> entityStates = new ();
    private readonly List<PlainState> plainStates = new ();

    private class EntityState
    {
        public BuilderEntity Entity;
        public SaveableEntity ConsumedBy;
        public Dictionary<string, object> Values = new ();
    }

    private class PlainState
    {
        public object Target;
        public FieldInfo Field;
        public object Value;
    }

    /// <summary>
    /// Captures the current reference state of all active builder entities.
    /// </summary>
    /// <param name="plainTargets">
    /// Edited objects that are not builder entities (for example tile
    /// behaviours). Their value of the given field is captured as well.
    /// </param>
    /// <param name="plainField">The field that is edited on the plain targets.</param>
    /// <returns>The snapshot.</returns>
    public static ReferenceSnapshot Capture(
        IEnumerable<object> plainTargets = null,
        FieldInfo plainField = null)
    {
        ReferenceSnapshot snapshot = new ReferenceSnapshot();

        // Deactivated (soft deleted) entities are not part of the snapshot.
        // They are not touched while they are deleted.
        foreach (BuilderEntity entity in BuilderRegistry.GetAll())
        {
            if (entity == null)
            {
                continue;
            }

            EntityState state = new EntityState
            {
                Entity = entity,
                ConsumedBy = entity.ConsumedBy,
            };

            foreach (KeyValuePair<string, RuntimeEditableValue> entry in
                     entity.RuntimeEditableFields)
            {
                FieldInfo field = entry.Value?.Field;

                if (field == null ||
                    !IsReferenceType(field.FieldType))
                {
                    continue;
                }

                state.Values[entry.Key] =
                    CopyProxyValue(entry.Value.Value);
            }

            snapshot.entityStates.Add(state);
        }

        if (plainTargets != null &&
            plainField != null &&
            IsReferenceType(plainField.FieldType))
        {
            foreach (object target in plainTargets)
            {
                if (target == null ||
                    target is BuilderEntity)
                {
                    continue;
                }

                snapshot.plainStates.Add(new PlainState
                {
                    Target = target,
                    Field = plainField,
                    Value = CopyTypedValue(
                        plainField,
                        plainField.GetValue(target)),
                });
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Whether two snapshots hold exactly the same state.
    /// </summary>
    /// <param name="other">The snapshot to compare with.</param>
    /// <returns>True when nothing differs.</returns>
    public bool IsSameAs(ReferenceSnapshot other)
    {
        if (other == null ||
            this.entityStates.Count != other.entityStates.Count ||
            this.plainStates.Count != other.plainStates.Count)
        {
            return false;
        }

        for (int i = 0; i < this.entityStates.Count; i++)
        {
            EntityState a = this.entityStates[i];
            EntityState b = other.entityStates[i];

            if (a.Entity != b.Entity ||
                a.ConsumedBy != b.ConsumedBy ||
                a.Values.Count != b.Values.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, object> entry in a.Values)
            {
                if (!b.Values.TryGetValue(entry.Key, out object otherValue) ||
                    !ValueEquals(entry.Value, otherValue))
                {
                    return false;
                }
            }
        }

        for (int i = 0; i < this.plainStates.Count; i++)
        {
            PlainState a = this.plainStates[i];
            PlainState b = other.plainStates[i];

            if (!ReferenceEquals(a.Target, b.Target) ||
                !ValueEquals(a.Value, b.Value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Brings every captured entity back to the captured state.
    /// </summary>
    public void Restore()
    {
        // 1. Release consumption that differs. Nothing can be consumed
        //    by a new consumer before it was released by the old one.
        foreach (EntityState state in this.entityStates)
        {
            if (state.Entity == null)
            {
                continue;
            }

            SaveableEntity current = state.Entity.ConsumedBy;

            if (current != null &&
                current != state.ConsumedBy)
            {
                state.Entity.ReleaseConsumedBy(current);
            }
        }

        // 2. Consume again where needed. This strips references from other
        //    entities as a side effect, step 3 puts the exact values back.
        foreach (EntityState state in this.entityStates)
        {
            if (state.Entity == null ||
                state.ConsumedBy == null)
            {
                continue;
            }

            if (state.Entity.ConsumedBy != state.ConsumedBy)
            {
                state.Entity.SetConsumedBy(state.ConsumedBy);
            }
        }

        // 3. Restore the reference values.
        foreach (EntityState state in this.entityStates)
        {
            if (state.Entity == null)
            {
                continue;
            }

            foreach (KeyValuePair<string, object> entry in state.Values)
            {
                state.Entity.SetRuntimeEditableValue(
                    entry.Key,
                    CopyProxyValue(entry.Value));
            }
        }

        foreach (PlainState state in this.plainStates)
        {
            if (state.Target is UnityEngine.Object unityObject &&
                unityObject == null)
            {
                continue;
            }

            state.Field.SetValue(
                state.Target,
                CopyTypedValue(state.Field, state.Value));
        }
    }

    private static bool IsReferenceType(Type type)
    {
        if (typeof(SaveableEntity).IsAssignableFrom(type))
        {
            return true;
        }

        return typeof(IList).IsAssignableFrom(type) &&
               type.IsGenericType &&
               typeof(SaveableEntity).IsAssignableFrom(
                   type.GetGenericArguments()[0]);
    }

    /// <summary>
    /// Builder entities store lists of entity references as List of SaveableEntity.
    /// </summary>
    private static object CopyProxyValue(object value)
    {
        if (value is IList list)
        {
            return new List<SaveableEntity>(
                list.Cast<SaveableEntity>());
        }

        return value;
    }

    /// <summary>
    /// Other objects store the list in the real type of the field.
    /// </summary>
    private static object CopyTypedValue(FieldInfo field, object value)
    {
        if (value is IList list)
        {
            IList copy =
                (IList)Activator.CreateInstance(field.FieldType);

            foreach (object item in list)
            {
                copy.Add(item);
            }

            return copy;
        }

        return value;
    }

    private static bool ValueEquals(object a, object b)
    {
        if (a is IList listA &&
            b is IList listB)
        {
            if (listA.Count != listB.Count)
            {
                return false;
            }

            for (int i = 0; i < listA.Count; i++)
            {
                if (!Equals(listA[i], listB[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return Equals(a, b);
    }
}
