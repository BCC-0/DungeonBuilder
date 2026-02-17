#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor for editing items from within the unity editor.
/// </summary>
[CustomEditor(typeof(ItemObject))]
public class ItemObjectEditor : Editor
{
    /// <summary>
    /// Adds the runtime copy object for editing item objects.
    /// </summary>
    public override void OnInspectorGUI()
    {
        ItemObject itemObj = (ItemObject)this.target;

        // Draw the default inspector
        this.DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Item Editor", EditorStyles.boldLabel);

        if (itemObj.Item == null)
        {
            if (GUILayout.Button("Create Runtime Copy"))
            {
                itemObj.CreateRuntimeCopy();
            }
            return;
        }

        Item runtimeItem = itemObj.Item;

        // Collect all fields including inherited
        List<FieldInfo> allFields = new List<FieldInfo>();
        System.Type type = runtimeItem.GetType();
        while (type != null && type != typeof(ScriptableObject))
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            allFields.AddRange(fields);
            type = type.BaseType;
        }

        // Show only fields marked as [RuntimeEditable] and [SerializeField] or public
        foreach (var field in allFields)
        {
            if ((field.IsPublic || field.GetCustomAttribute<SerializeField>() != null) &&
                field.GetCustomAttribute<RuntimeEditableAttribute>() != null)
            {
                object value = field.GetValue(runtimeItem);
                EditorGUI.BeginChangeCheck();

                if (field.FieldType == typeof(int))
                {
                    value = EditorGUILayout.IntField(field.Name, (int)value);
                }
                else if (field.FieldType == typeof(float))
                {
                    value = EditorGUILayout.FloatField(field.Name, (float)value);
                }
                else if (field.FieldType == typeof(string))
                {
                    value = EditorGUILayout.TextField(field.Name, (string)value);
                }
                else if (field.FieldType == typeof(bool))
                {
                    value = EditorGUILayout.Toggle(field.Name, (bool)value);
                }
                else if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
                {
                    value = EditorGUILayout.ObjectField(field.Name, (UnityEngine.Object)value, field.FieldType, false);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(runtimeItem, "Edit Runtime Item Field");
                    field.SetValue(runtimeItem, value);
                    EditorUtility.SetDirty(runtimeItem);
                }
            }
        }
    }
}
#endif
