using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    private RuntimePositionField positionField;

    /// <summary>
    /// Gets the instance of the runtime property editor.
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

        // Remember what we built for, so Update does not rebuild it again.
        this.lastSelectionKey = this.BuildSelectionKey();

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
    /// Refreshes the shown position without rebuilding the whole inspector.
    /// Used while entities are being moved.
    /// </summary>
    public void RefreshPosition()
    {
        if (this.positionField != null)
        {
            this.positionField.Refresh();
        }
    }

    /// <summary>
    /// Begins reference selection.
    /// </summary>
    /// <param name="propertyField">The field to set the reference to.</param>
    public void BeginReferenceSelection(
        RuntimePropertyField propertyField)
    {
        this.BeginReferenceSelection(propertyField, -1);
    }

    /// <summary>
    /// Begins reference selection with a specified index.
    /// </summary>
    /// <param name="propertyField">The field to set the reference to.</param>
    /// <param name="index">The index of the field.</param>
    public void BeginReferenceSelection(
        RuntimePropertyField propertyField,
        int index)
    {
        EntitySelectionManager selectionManager =
            FindAnyObjectByType<EntitySelectionManager>();

        if (selectionManager == null)
        {
            return;
        }

        this.referenceField = propertyField;
        this.referenceIndex = index;

        selectionManager.BeginReferenceSelection();

        if (!selectionManager.IsReferenceSelectionMode)
        {
            this.referenceField = null;
            this.referenceIndex = -1;
        }
    }

    /// <summary>
    /// Confirms the reference selection and sets the property field.
    /// </summary>
    /// <param name="entities">The entities that were referenced.</param>
    public void ConfirmReferenceSelection(
        IReadOnlyList<SaveableEntity> entities)
    {
        RuntimePropertyField propertyField = this.referenceField;
        int index = this.referenceIndex;

        this.referenceField = null;
        this.referenceIndex = -1;

        if (propertyField != null)
        {
            if (propertyField.IsReferenceList())
            {
                if (index >= 0)
                {
                    if (entities != null &&
                        entities.Count == 1)
                    {
                        propertyField.SetReference(
                            index,
                            entities[0]);
                    }
                }
                else
                {
                    propertyField.AddReferences(
                        entities);
                }
            }
            else if (entities != null &&
                     entities.Count == 1)
            {
                propertyField.SetReference(
                    entities[0]);
            }
        }

        this.Rebuild();
    }

    /// <summary>
    /// Applies picked entities to the reference field that started the
    /// reference selection.
    /// </summary>
    /// <param name="entities">The entities that were picked.</param>
    /// <returns>
    /// True when reference selection is finished (single reference set, list
    /// element replaced, or the field is gone). False when it should stay open,
    /// e.g. for lists where more entities can be added, or when the pick did
    /// not fit the field.
    /// </returns>
    public bool TryApplyReferences(
        IReadOnlyList<SaveableEntity> entities)
    {
        RuntimePropertyField propertyField = this.referenceField;

        // The row was destroyed (inspector rebuilt), nothing to apply to.
        if (propertyField == null)
        {
            return true;
        }

        List<SaveableEntity> valid =
            entities
                .Where(e => e != null && propertyField.CanReference(e))
                .ToList();

        if (valid.Count == 0)
        {
            return false;
        }

        if (propertyField.IsReferenceList())
        {
            if (this.referenceIndex >= 0)
            {
                // Replacing one existing element needs exactly one entity.
                if (valid.Count != 1)
                {
                    return false;
                }

                propertyField.SetReference(
                    this.referenceIndex,
                    valid[0]);

                return true;
            }

            // Adding: keep going, the user can add more.
            propertyField.AddReferences(valid);
            return false;
        }

        if (valid.Count != 1)
        {
            return false;
        }

        propertyField.SetReference(valid[0]);
        return true;
    }

    /// <summary>
    /// Cancel the reference selection, doesn't set a reference to the property field.
    /// </summary>
    public void CancelReferenceSelection()
    {
        this.referenceField = null;
        this.referenceIndex = -1;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (MapEditorManager.Instance == null)
        {
            return;
        }

        // While the user picks references the selection changes on purpose.
        // The inspector must stay as it is, otherwise the field that started
        // the reference selection would be destroyed.
        if (this.referenceField != null)
        {
            return;
        }

        string selectionKey =
            this.BuildSelectionKey();

        if (selectionKey == this.lastSelectionKey)
        {
            return;
        }

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
        this.positionField = null;
    }

    private void BuildEntityInspector()
    {
        IReadOnlyList<SaveableEntity> selected =
            MapEditorManager.Instance.SelectedEntities;

        if (selected == null || selected.Count == 0)
        {
            return;
        }

        this.positionField =
            this.CreateRow<RuntimePositionField>(
                this.positionRowPrefab);

        if (this.positionField != null)
        {
            this.positionField.Initialize(selected);
        }

        List<BuilderEntity> builderEntities =
            new List<BuilderEntity>();

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

        IReadOnlyDictionary<string, RuntimeEditableValue> fields =
            firstEntity.RuntimeEditableFields;

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

            // The field must be editable on every selected entity and have
            // the same type everywhere.
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
            Debug.LogWarning(
                "RuntimePropertyEditor: MapEditorManager has no " +
                "ActiveController, tile fields cannot be shown.");

            return;
        }

        SaveableTilemap tilemap =
            MapEditorManager.Instance.ActiveController.Tilemap;

        if (tilemap == null)
        {
            Debug.LogWarning(
                "RuntimePropertyEditor: The active controller has " +
                "no Tilemap, tile fields cannot be shown.");

            return;
        }

        // Position is always available for selected tiles.
        this.positionField =
            this.CreateRow<RuntimePositionField>(
                this.positionRowPrefab);

        if (this.positionField != null)
        {
            this.positionField.Initialize(selected, tilemap);
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

        List<object> targets =
            new List<object>(behaviours);

        // Only properties common to every selected behaviour
        // should be displayed.
        List<FieldInfo> fields =
            this.GetSaveFields(behaviours[0].GetType());

        foreach (FieldInfo field in fields)
        {
            if (!this.IsSupportedType(field.FieldType))
            {
                continue;
            }

            bool validForAll = true;

            foreach (TileBehaviour behaviour in behaviours)
            {
                FieldInfo matchingField =
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
                targets,
                null);
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

    private FieldInfo FindField(Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }

    private bool IsSupportedType(Type type)
    {
        if (type == typeof(int) ||
            type == typeof(float) ||
            type == typeof(bool) ||
            type == typeof(string))
        {
            return true;
        }

        if (typeof(SaveableEntity).IsAssignableFrom(type))
        {
            return true;
        }

        if (typeof(IList).IsAssignableFrom(type) &&
            type.IsGenericType)
        {
            Type elementType =
                type.GetGenericArguments()[0];

            return typeof(SaveableEntity).IsAssignableFrom(
                elementType);
        }

        return false;
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

            this.generatedRows.Remove(row);
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
            List<string> ids = new List<string>();

            foreach (SaveableEntity entity in entities)
            {
                if (entity != null)
                {
                    ids.Add(entity.GetUniqueID());
                }
            }

            ids.Sort();

            return "E:" + string.Join("|", ids);
        }

        IReadOnlyList<Vector2Int> tiles =
            MapEditorManager.Instance.SelectedTiles;

        if (tiles != null && tiles.Count > 0)
        {
            List<string> positions = new List<string>();

            foreach (Vector2Int tile in tiles)
            {
                positions.Add(tile.x + "," + tile.y);
            }

            positions.Sort();

            return "T:" + string.Join("|", positions);
        }

        return string.Empty;
    }
}