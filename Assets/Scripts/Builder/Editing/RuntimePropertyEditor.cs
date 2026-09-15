using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script that populates the inspector of the builder and allows
/// the editing of marked properties.
/// </summary>
public class RuntimePropertyEditor : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Transform propertyContainer;

    [SerializeField]
    private GameObject propertyRowPrefab;

    [SerializeField]
    private GameObject positionRowPrefab;

    private List<GameObject> generatedRows = new List<GameObject>();

    private string lastSelectionKey;

    /// <summary>
    /// Rebuild the inspector.
    /// </summary>
    public void Rebuild()
    {
        this.Clear();

        if (MapEditorManager.Instance == null)
        {
            return;
        }

        if (MapEditorManager.Instance.SelectedEntities.Count > 0)
        {
            this.BuildEntityInspector();
            return;
        }

        if (MapEditorManager.Instance.SelectedTiles.Count > 0)
        {
            this.BuildTileInspector();
        }
    }

    private void Update()
    {
        if (MapEditorManager.Instance == null)
        {
            return;
        }

        string selectionKey =
            this.BuildSelectionKey();

        if (selectionKey == this.lastSelectionKey)
        {
            return;
        }

        this.lastSelectionKey = selectionKey;
        this.Rebuild();
    }

    private void Clear()
    {
        foreach (GameObject row in this.generatedRows)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }

        this.generatedRows.Clear();
    }

    private void BuildEntityInspector()
    {
        IReadOnlyList<SaveableEntity> selected =
            MapEditorManager.Instance.SelectedEntities;

        if (selected == null || selected.Count == 0)
        {
            return;
        }

        RuntimePositionField positionEditor = this.CreateRow<RuntimePositionField>(this.positionRowPrefab);

        if (positionEditor != null)
        {
            positionEditor.Initialize(selected);
        }

        List<BuilderEntity> builderEntities = new List<BuilderEntity>();

        foreach (SaveableEntity entity in selected)
        {
            if (entity is BuilderEntity builderEntity)
            {
                builderEntities.Add(builderEntity);
            }
        }

        if (builderEntities.Count == 0)
        {
            return;
        }

        BuilderEntity firstEntity = builderEntities[0];

        IReadOnlyDictionary<string, RuntimeEditableValue> fields = firstEntity.RuntimeEditableFields;

        if (fields == null || fields.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<string, RuntimeEditableValue> entry in fields)
        {
            string fieldName = entry.Key;
            RuntimeEditableValue editableValue = entry.Value;

            if (editableValue == null ||
                editableValue.Field == null)
            {
                continue;
            }

            Type fieldType =
                editableValue.Field.FieldType;

            if (!this.IsSupportedType(fieldType))
            {
                continue;
            }

            bool validForAll = true;

            foreach (BuilderEntity entity in builderEntities)
            {
                if (!entity.TryGetRuntimeEditableField(
                        fieldName,
                        out RuntimeEditableValue matchingValue))
                {
                    validForAll = false;
                    break;
                }

                if (matchingValue == null ||
                    matchingValue.Field == null ||
                    matchingValue.Field.FieldType != fieldType)
                {
                    validForAll = false;
                    break;
                }
            }

            if (!validForAll)
            {
                continue;
            }

            RuntimePropertyField property =
                this.CreateRow<RuntimePropertyField>(
                    this.propertyRowPrefab);

            if (property == null)
            {
                continue;
            }

            property.Initialize(
                this.GetDisplayName(fieldName),
                fieldName,
                builderEntities);
        }
    }

    private void BuildTileInspector()
    {
        IReadOnlyList<Vector2Int> selected =
            MapEditorManager.Instance.SelectedTiles;

        if (selected == null || selected.Count == 0)
        {
            return;
        }

        if (MapEditorManager.Instance.ActiveController == null)
        {
            return;
        }

        SaveableTilemap tilemap =
            MapEditorManager.Instance.ActiveController.Tilemap;

        if (tilemap == null)
        {
            return;
        }

        List<object> targets =
            new List<object>();

        foreach (Vector2Int position in selected)
        {
            TileData data =
                tilemap.GetTileData(position);

            if (data != null)
            {
                targets.Add(data);
            }
        }

        if (targets.Count == 0)
        {
            return;
        }

        Type targetType =
            targets[0].GetType();

        for (int i = 1; i < targets.Count; i++)
        {
            if (targets[i].GetType() != targetType)
            {
                return;
            }
        }

        List<System.Reflection.FieldInfo> fields =
            this.GetSaveFields(targetType);

        foreach (System.Reflection.FieldInfo field in fields)
        {
            if (!this.IsSupportedType(field.FieldType))
            {
                continue;
            }

            RuntimePropertyField property =
                this.CreateRow<RuntimePropertyField>(
                    this.propertyRowPrefab);

            if (property == null)
            {
                continue;
            }

            property.Initialize(
                this.GetDisplayName(field.Name),
                field,
                targets,
                () => this.RefreshTiles(
                    tilemap,
                    selected));
        }
    }

    private void RefreshTiles(
        SaveableTilemap tilemap,
        IReadOnlyList<Vector2Int> positions)
    {
        foreach (Vector2Int position in positions)
        {
            TileData data =
                tilemap.GetTileData(position);

            if (data != null)
            {
                tilemap.UpdateTileData(
                    position,
                    data);
            }
        }
    }

    private List<System.Reflection.FieldInfo> GetSaveFields(Type type)
    {
        List<System.Reflection.FieldInfo> fields =
            new List<System.Reflection.FieldInfo>();

        while (type != null)
        {
            System.Reflection.FieldInfo[] declaredFields =
                type.GetFields(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);

            foreach (System.Reflection.FieldInfo field in declaredFields)
            {
                if (Attribute.IsDefined(
                        field,
                        typeof(SaveFieldAttribute)))
                {
                    fields.Add(field);
                }
            }

            type = type.BaseType;
        }

        return fields;
    }

    private bool IsSupportedType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(float) ||
               type == typeof(bool) ||
               type == typeof(string);
    }

    private string GetDisplayName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return fieldName;
        }

        return char.ToUpper(fieldName[0]) +
               fieldName.Substring(1);
    }

    private T CreateRow<T>(GameObject prefab)
        where T : Component
    {
        if (prefab == null)
        {
            Debug.LogError(
                $"RuntimePropertyEditor: Prefab for " +
                $"{typeof(T).Name} is not assigned.");

            return null;
        }

        if (this.propertyContainer == null)
        {
            Debug.LogError(
                "RuntimePropertyEditor: Property Container " +
                "is not assigned.");

            return null;
        }

        GameObject row =
            Instantiate(
                prefab,
                this.propertyContainer);

        this.generatedRows.Add(row);

        T component =
            row.GetComponent<T>();

        if (component == null)
        {
            Debug.LogError(
                $"RuntimePropertyEditor: Prefab does not contain " +
                $"{typeof(T).Name}.");

            Destroy(row);
            return null;
        }

        return component;
    }

    private string BuildSelectionKey()
    {
        if (MapEditorManager.Instance == null)
        {
            return string.Empty;
        }

        IReadOnlyList<SaveableEntity> entities =
            MapEditorManager.Instance.SelectedEntities;

        if (entities != null && entities.Count > 0)
        {
            string key = "E:";

            foreach (SaveableEntity entity in entities)
            {
                if (entity != null)
                {
                    key +=
                        entity.GetUniqueID() + ";";
                }
            }

            return key;
        }

        IReadOnlyList<Vector2Int> tiles =
            MapEditorManager.Instance.SelectedTiles;

        if (tiles != null && tiles.Count > 0)
        {
            string key = "T:";

            foreach (Vector2Int tile in tiles)
            {
                key +=
                    $"{tile.x},{tile.y};";
            }

            return key;
        }

        return string.Empty;
    }
}