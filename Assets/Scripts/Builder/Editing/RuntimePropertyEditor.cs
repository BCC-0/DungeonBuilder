using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
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
    private RuntimePropertyField referenceField;
    private int referenceIndex = -1;

    /// <summary>
    /// Gets the instance of the property editor.
    /// </summary>
    public static RuntimePropertyEditor Instance
    {
        get; private set;
    }

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

    /// <summary>
    /// Start selecting which objects to reference for a reference field.
    /// </summary>
    /// <param name="propertyField">The field to add the reference to.</param>
    public void BeginReferenceSelection(RuntimePropertyField propertyField)
    {
        this.referenceField = propertyField;
        this.referenceIndex = -1;
    }

    public void BeginReferenceSelection(RuntimePropertyField propertyField, int index)
    {
        this.referenceField = propertyField;
        this.referenceIndex = index;
    }


    public void SelectReference(SaveableEntity entity)
    {
        if (this.referenceField == null ||
            entity == null)
        {
            return;
        }

        this.referenceField.SetReference(entity);
        this.referenceField = null;
    }

    public void CancelReferenceSelection()
    {
        this.referenceField = null;
    }

    private void Start()
    {
        Instance = this;
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

        // Position is always available for selected tiles.
        RuntimePositionField positionEditor =
            this.CreateRow<RuntimePositionField>(
                this.positionRowPrefab);

        if (positionEditor != null)
        {
            positionEditor.Initialize(selected, tilemap);
        }

        List<TileBehaviour> behaviours =
            new List<TileBehaviour>();

        foreach (Vector2Int position in selected)
        {
            TileBehaviour behaviour =
                tilemap.GetTileBehaviour(position);

            if (behaviour != null)
            {
                behaviours.Add(behaviour);
            }
        }

        if (behaviours.Count == 0)
        {
            return;
        }

        // Only properties common to every selected behaviour
        // should be displayed.
        Type firstType =
            behaviours[0].GetType();

        List<System.Reflection.FieldInfo> fields =
            this.GetSaveFields(firstType);

        foreach (System.Reflection.FieldInfo field in fields)
        {
            if (!this.IsSupportedType(field.FieldType))
            {
                continue;
            }

            bool validForAll = true;

            foreach (TileBehaviour behaviour in behaviours)
            {
                System.Reflection.FieldInfo matchingField =
                    this.FindField(behaviour.GetType(), field.Name);

                if (matchingField == null ||
                    matchingField.FieldType != field.FieldType ||
                    !Attribute.IsDefined(
                        matchingField,
                        typeof(SaveFieldAttribute)))
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
                this.GetDisplayName(field.Name),
                field,
                new List<object>(behaviours),
                null);
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

    private List<FieldInfo> GetSaveFields(Type type)
    {
        List<FieldInfo> fields = new List<FieldInfo>();

        while (type != null)
        {
            FieldInfo[] declaredFields =
                type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

            foreach (FieldInfo field in declaredFields)
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

        return char.ToUpper(fieldName[0]) + fieldName.Substring(1) + ":";
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

        GameObject row = Instantiate(prefab, this.propertyContainer);

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

    private FieldInfo FindField(Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }
}