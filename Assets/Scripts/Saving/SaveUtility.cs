using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Handles capturing and restoring fields marked with [SaveField].
/// Automatically converts references to UniqueIDs.
/// </summary>
public static class SaveUtility
{
    /// <summary>
    /// Capture all fields marked [SaveField] in the object.
    /// </summary>
    /// <param name="obj">The object to capture field from.</param>
    /// <returns>A dictionary of found objects.</returns>
    public static Dictionary<string, object> Capture(object obj)
    {
        var data = new Dictionary<string, object>();

        var fields = obj.GetType().GetFields(BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
            {
                object value = field.GetValue(obj);

                // If the field is a SaveableEntity reference, store its UniqueID
                if (value is SaveableEntity entity)
                {
                    value = entity.GetUniqueID();
                }

                // If the field is a list of SaveableEntities, convert to list of IDs
                else if (value is IList list && list.Count > 0 &&
                         list[0] is SaveableEntity)
                {
                    List<string> idlist = new List<string>();
                    foreach (var item in list)
                    {
                        idlist.Add(((SaveableEntity)item).GetUniqueID());
                    }

                    value = idlist;
                }

                data[field.Name] = value;
            }
        }

        return data;
    }

    /// <summary>
    /// Restore all [SaveField] variables from saved data.
    /// </summary>
    /// <param name="obj">The object to restore fields for.</param>
    /// <param name="state"> The state of the object to restore.</param>
    public static void Restore(object obj, object state)
    {
        if (!(state is Dictionary<string, object> data))
        {
            return;
        }

        var fields = obj.GetType().GetFields(BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (!Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
            {
                continue;
            }

            if (!data.TryGetValue(field.Name, out object value))
            {
                continue;
            }

            // Restore SaveableEntity references
            if (field.FieldType.IsSubclassOf(typeof(SaveableEntity)))
            {
                string id = value as string;
                if (!string.IsNullOrEmpty(id))
                {
                    SaveableEntity entity = SaveManager.GetEntityByID(id);
                    field.SetValue(obj, entity);
                }
            }

            // Restore list of SaveableEntities
            else if (typeof(IList).IsAssignableFrom(field.FieldType) &&
                     field.FieldType.IsGenericType &&
                     field.FieldType.GetGenericArguments()[0].IsSubclassOf(typeof(SaveableEntity)))
            {
                Type elementType = field.FieldType.GetGenericArguments()[0];
                IList list = (IList)Activator.CreateInstance(field.FieldType);
                foreach (string id in (List<string>)value)
                {
                    SaveableEntity entity = SaveManager.GetEntityByID(id);
                    list.Add(entity);
                }

                field.SetValue(obj, list);
            }

            // Restore simple values directly
            else
            {
                field.SetValue(obj, value);
            }
        }
    }
}