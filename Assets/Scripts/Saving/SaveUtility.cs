using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class SaveUtility
{
    /// <summary>
    /// Capture all fields marked [SaveField] in the object.
    /// </summary>
    public static Dictionary<string, object> Capture(object obj)
    {
        var data = new Dictionary<string, object>();
        var fields = obj.GetType().GetFields(BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (!Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
                continue;

            object value = field.GetValue(obj);

            // SaveableEntity reference
            if (value is SaveableEntity entity)
                value = entity.GetUniqueID();

            // List of SaveableEntities
            else if (value is IList list && list.Count > 0 && list[0] is SaveableEntity)
            {
                List<string> idlist = new List<string>();
                foreach (var item in list)
                    idlist.Add(((SaveableEntity)item).GetUniqueID());

                value = idlist;
            }

            data[field.Name] = value;
        }

        Debug.Log($"[SaveUtility] Captured {data.Count} fields for {obj.GetType().Name}");
        return data;
    }

    /// <summary>
    /// Restore all [SaveField] variables from saved data.
    /// </summary>
    public static void Restore(object obj, Dictionary<string, object> state)
    {
        var fields = obj.GetType().GetFields(BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (!Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
                continue;

            if (!state.TryGetValue(field.Name, out object value))
                continue;

            if (field.FieldType.IsSubclassOf(typeof(SaveableEntity)))
            {
                string id = value as string;
                field.SetValue(obj, SaveManager.GetEntityByID(id));
            }
            else if (typeof(IList).IsAssignableFrom(field.FieldType) &&
                     field.FieldType.IsGenericType &&
                     field.FieldType.GetGenericArguments()[0].IsSubclassOf(typeof(SaveableEntity)))
            {
                Type elementType = field.FieldType.GetGenericArguments()[0];
                IList list = (IList)Activator.CreateInstance(field.FieldType);
                foreach (string id in (List<string>)value)
                    list.Add(SaveManager.GetEntityByID(id));

                field.SetValue(obj, list);
            }
            else
            {
                field.SetValue(obj, value);
            }
        }
        Debug.Log($"[SaveUtility] Restored fields for {obj.GetType().Name}");
    }
}