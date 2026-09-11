using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class RuntimePropertyEditor : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Transform propertyContainer;

    [SerializeField]
    private GameObject propertyRowPrefab;

    [SerializeField]
    private GameObject transformRowPrefab;

    private List<GameObject> generatedRows = new List<GameObject>();

    private string lastSelectionKey;

    /// <summary>
    /// Rebuilds the selection inspector.
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

        string selectionKey = this.BuildSelectionKey();

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
        IReadOnlyList<SaveableEntity> selected = MapEditorManager.Instance.SelectedEntities;

        if (selected == null || selected.Count == 0)
        {
            return;
        }

        RuntimeTransformEditor transformEditor = this.CreateRow<RuntimeTransformEditor>(this.transformRowPrefab);

        if (transformEditor != null)
        {
            transformEditor.Initialize(selected);
        }

        // Build the list of actual objects whose
        // RuntimeEditable fields we want to inspect.
        List<object> targets = new List<object>();

        foreach (SaveableEntity entity in selected)
        {
            if (entity == null)
            {
                continue;
            }

            // ItemObject exposes its runtime Item instead
            // of editing fields on ItemObject itself.
            if (entity is ItemObject itemObject)
            {
                if (itemObject.Item != null)
                {
                    targets.Add(itemObject.Item);
                }
            }
            else
            {
                targets.Add(entity);
            }
        }

        if (targets.Count == 0)
        {
            return;
        }

        // Multi-selection only works when all targets
        // have the same runtime type.
        Type targetType = targets[0].GetType();

        for (int i = 1; i < targets.Count; i++)
        {
            if (targets[i].GetType() != targetType)
            {
                return;
            }
        }

        FieldInfo[] fields = targetType.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            if (!Attribute.IsDefined(
                    field,
                    typeof(RuntimeEditableAttribute)))
            {
                continue;
            }

            if (!this.IsSupportedType(field.FieldType))
            {
                Debug.LogWarning(
                    $"RuntimePropertyEditor: Field '{field.Name}' " +
                    $"on {targetType.Name} has unsupported type " +
                    $"{field.FieldType.Name}.");

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
                targets);
        }
    }

    private void BuildTileInspector()
    {
        IReadOnlyList<Vector2Int> selected = MapEditorManager.Instance.SelectedTiles;

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

        List<object> targets = new List<object>();

        foreach (Vector2Int position in selected)
        {
            TileData data = tilemap.GetTileData(position);

            if (data != null)
            {
                targets.Add(data);
            }
        }

        if (targets.Count == 0)
        {
            return;
        }

        Type targetType = targets[0].GetType();

        for (int i = 1; i < targets.Count; i++)
        {
            if (targets[i].GetType() != targetType)
            {
                return;
            }
        }

        FieldInfo[] fields = targetType.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            if (!Attribute.IsDefined(
                    field,
                    typeof(RuntimeEditableAttribute)))
            {
                continue;
            }

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
                () => this.RefreshTiles(tilemap, selected));
        }
    }

    private void RefreshTiles(SaveableTilemap tilemap, IReadOnlyList<Vector2Int> positions)
    {
        foreach (Vector2Int position in positions)
        {
            TileData data = tilemap.GetTileData(position);

            if (data != null)
            {
                tilemap.UpdateTileData(position, data);
            }
        }
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

        GameObject row = Instantiate(
            prefab,
            this.propertyContainer);

        this.generatedRows.Add(row);

        T component = row.GetComponent<T>();

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
                    key += entity.GetUniqueID() + ";";
                }
            }

            return key;
        }

        IReadOnlyList<Vector2Int> tiles = MapEditorManager.Instance.SelectedTiles;

        if (tiles != null && tiles.Count > 0)
        {
            string key = "T:";

            foreach (Vector2Int tile in tiles)
            {
                key += $"{tile.x},{tile.y};";
            }

            return key;
        }

        return string.Empty;
    }
}