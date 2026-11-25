
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField]
    private Button backButton;

    [Header("Reference Navigation")]
    [SerializeField]
    private float referenceCameraMoveDuration = 0.5f;

    private List<GameObject> generatedRows = new List<GameObject>();

    private string lastSelectionKey;

    private RuntimePropertyField referenceField;
    private int referenceIndex = -1;

    private RuntimePositionField positionField;
    private EntitySelectionManager selectionManager;
    private CameraController cameraController;

    private Stack<NavigationState> navigationHistory =
        new Stack<NavigationState>();

    /// <summary>
    /// Stores the selection and camera position before navigating
    /// to another referenced entity.
    /// </summary>
    private class NavigationState
    {
        public List<SaveableEntity> SelectedEntities;
        public List<Vector2Int> SelectedTiles;
        public Vector3 CameraPosition;
        public bool HasCameraPosition;
    }

    /// <summary>
    /// Gets the instance of the runtime property editor.
    /// </summary>
    public static RuntimePropertyEditor Instance
    {
        get; private set;
    }

    /// <summary>
    /// Rebuilds the inspector.
    /// </summary>
    public void Rebuild()
    {
        string selectionKey = this.BuildSelectionKey();

        if (selectionKey == this.lastSelectionKey)
        {
            return;
        }

        this.Clear();

        if (MapEditorManager.Instance == null)
        {
            return;
        }

        this.lastSelectionKey = selectionKey;

        if (MapEditorManager.Instance.SelectedEntities.Count > 0)
        {
            this.BuildEntityInspector();
        }
        else if (MapEditorManager.Instance.SelectedTiles.Count > 0)
        {
            this.BuildTileInspector();
        }
    }

    /// <summary>
    /// Navigates to a referenced entity.
    /// The previous selection and camera position are stored so
    /// the Back button can restore them.
    /// </summary>
    /// <param name="entity">The referenced entity.</param>
    public void NavigateToReference(SaveableEntity entity)
    {
        if (entity == null ||
            MapEditorManager.Instance == null)
        {
            return;
        }

        MapEditorManager manager =
            MapEditorManager.Instance;

        this.navigationHistory.Push(this.CaptureNavigationState());

        // Determine this before changing the selection.
        bool isConsumed = entity.ConsumedBy != null;

        manager.SelectEntityForReference(entity);
        this.selectionManager?.RefreshSelection();

        this.Rebuild();
        this.UpdateBackButton();

        SaveableEntity cameraTarget =
            isConsumed
                ? this.GetConsumerInWorld(entity)
                : entity.GetWorldRepresentative();

        if (this.cameraController != null &&
            cameraTarget != null)
        {
            this.cameraController.MoveTo(
                cameraTarget.transform.position,
                this.referenceCameraMoveDuration);
        }
    }

    /// <summary>
    /// Navigates to several referenced entities at once, so their shared fields can
    /// be edited together. The previous selection and camera position are stored so
    /// the Back button can restore them.
    /// </summary>
    /// <param name="entities">The referenced entities to select.</param>
    public void NavigateToReferences(IReadOnlyList<SaveableEntity> entities)
    {
        if (entities == null ||
            MapEditorManager.Instance == null)
        {
            return;
        }

        List<SaveableEntity> targets = new List<SaveableEntity>();

        foreach (SaveableEntity entity in entities)
        {
            if (entity != null &&
                !targets.Contains(entity))
            {
                targets.Add(entity);
            }
        }

        if (targets.Count == 0)
        {
            return;
        }

        if (targets.Count == 1)
        {
            this.NavigateToReference(targets[0]);

            return;
        }

        this.navigationHistory.Push(this.CaptureNavigationState());

        MapEditorManager manager = MapEditorManager.Instance;
        manager.SelectEntitiesForReference(targets);
        this.selectionManager?.RefreshSelection();

        this.Rebuild();
        this.UpdateBackButton();

        SaveableEntity cameraTarget = this.GetCameraTarget(targets[0]);

        if (this.cameraController != null &&
            cameraTarget != null)
        {
            this.cameraController.MoveTo(
                cameraTarget.transform.position,
                this.referenceCameraMoveDuration);
        }
    }

    /// <summary>
    /// Returns to the previously selected entity or tile selection.
    /// </summary>
    public void NavigateBack()
    {
        if (this.navigationHistory.Count == 0 ||
            MapEditorManager.Instance == null)
        {
            return;
        }

        NavigationState state =
            this.navigationHistory.Pop();

        MapEditorManager manager =
            MapEditorManager.Instance;

        // Restore the previous selection.
        manager.SelectedEntities =
            new List<SaveableEntity>(state.SelectedEntities);

        manager.SelectedTiles =
            new List<Vector2Int>(state.SelectedTiles);

        this.selectionManager?.RefreshSelection();

        this.Rebuild();
        this.UpdateBackButton();

        SaveableEntity first =
            state.SelectedEntities.FirstOrDefault(e => e != null);

        SaveableEntity cameraTarget =
            first != null
                ? this.GetCameraTarget(first)
                : null;

        if (this.cameraController == null)
        {
            return;
        }

        if (cameraTarget != null)
        {
            this.cameraController.MoveTo(
                cameraTarget.transform.position,
                this.referenceCameraMoveDuration);
        }
        else if (state.HasCameraPosition)
        {
            this.cameraController.MoveTo(
                state.CameraPosition,
                this.referenceCameraMoveDuration);
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
    /// <param name="propertyField">The field to reference for.</param>
    public void BeginReferenceSelection(RuntimePropertyField propertyField)
    {
        this.BeginReferenceSelection(propertyField, -1);
    }

    /// <summary>
    /// Begins reference selection with a specified index.
    /// </summary>
    /// <param name="propertyField">The field to reference for.</param>
    /// <param name="index">The index to reference for.</param>
    public void BeginReferenceSelection(
        RuntimePropertyField propertyField,
        int index)
    {
        if (this.selectionManager == null)
        {
            return;
        }

        this.referenceField = propertyField;
        this.referenceIndex = index;

        this.selectionManager.BeginReferenceSelection();

        if (!this.selectionManager.IsReferenceSelectionMode)
        {
            this.referenceField = null;
            this.referenceIndex = -1;
        }
    }

    /// <summary>
    /// Confirms the reference selection and sets the property field.
    /// </summary>
    /// <param name="entities">The entities to reference.</param>
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
                    propertyField.AddReferences(entities);
                }
            }
            else if (entities != null &&
                     entities.Count == 1)
            {
                propertyField.SetReference(entities[0]);
            }
        }

        this.Rebuild();
    }

    /// <summary>
    /// Applies picked entities to the reference field that started
    /// the reference selection.
    /// </summary>
    /// <returns>
    /// True when reference selection is finished. False when it should
    /// stay open, for example when a list can accept more references.
    /// </returns>
    /// <param name="entities">The entities to reference.</param>
    public bool TryApplyReferences(
        IReadOnlyList<SaveableEntity> entities)
    {
        RuntimePropertyField propertyField = this.referenceField;

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
                propertyField.SetReference(
                    this.referenceIndex,
                    valid[0]);
            }
            else
            {
                propertyField.AddReferences(valid);
            }

            return true;
        }

        propertyField.SetReference(valid[0]);
        return true;
    }

    /// <summary>
    /// Cancels reference selection without setting a reference.
    /// </summary>
    public void CancelReferenceSelection()
    {
        this.referenceField = null;
        this.referenceIndex = -1;

        if (this.selectionManager != null)
        {
            this.selectionManager.CancelReferenceSelection();
        }
    }

    /// <summary>
    /// Forces to rebuild the inspector. Needed after redo/undo action or consuming a referenced entity.
    /// </summary>
    public void ForceRebuild()
    {
        this.lastSelectionKey = null;
        this.Rebuild();
    }

    private void Awake()
    {
        this.selectionManager =
            FindAnyObjectByType<EntitySelectionManager>();

        this.cameraController =
            FindAnyObjectByType<CameraController>();

        Instance = this;

        if (this.backButton != null)
        {
            this.backButton.onClick.RemoveListener(this.NavigateBack);
            this.backButton.onClick.AddListener(this.NavigateBack);
            this.backButton.gameObject.SetActive(false);
        }
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

        // A selection change made outside reference navigation starts
        // a new navigation history.
        this.navigationHistory.Clear();
        this.UpdateBackButton();

        this.Rebuild();
    }

    private void UpdateBackButton()
    {
        if (this.backButton != null)
        {
            this.backButton.gameObject.SetActive(
                this.navigationHistory.Count > 0);
        }
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

        // Consumed entities do not have their own world position.
        bool hasConsumedEntity = selected.Any(
            entity => entity != null && entity.ConsumedBy != null);

        if (!hasConsumedEntity)
        {
            this.positionField =
                this.CreateRow<RuntimePositionField>(
                    this.positionRowPrefab);

            if (this.positionField != null)
            {
                this.positionField.Initialize(selected);
            }
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

        foreach (KeyValuePair<string, RuntimeEditableValue> entry in
            fields.OrderByDescending(entry =>
                this.GetFieldPriority(entry.Value?.Field)))
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

            string displayName =
                this.GetFieldDisplayName(editableValue.Field);

            property.Initialize(
                displayName,
                fieldName,
                builderEntities,
                this.Rebuild);
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

            SaveFieldAttribute attribute =
                field.GetCustomAttribute<SaveFieldAttribute>();

            string displayName =
                !string.IsNullOrEmpty(attribute?.DisplayName)
                    ? attribute.DisplayName
                    : this.GetDisplayName(field.Name);

            property.Initialize(
                displayName,
                field,
                targets,
                this.Rebuild);
        }
    }

    private int GetFieldPriority(FieldInfo field)
    {
        if (field == null)
        {
            return 0;
        }

        RuntimeEditableAttribute runtimeAttribute =
            field.GetCustomAttribute<RuntimeEditableAttribute>();

        if (runtimeAttribute != null)
        {
            return runtimeAttribute.Priority;
        }

        SaveFieldAttribute saveAttribute =
            field.GetCustomAttribute<SaveFieldAttribute>();

        return saveAttribute != null
            ? saveAttribute.Priority
            : 0;
    }

    private string GetFieldDisplayName(FieldInfo field)
    {
        if (field == null)
        {
            return string.Empty;
        }

        RuntimeEditableAttribute runtimeAttribute =
            field.GetCustomAttribute<RuntimeEditableAttribute>();

        if (!string.IsNullOrEmpty(runtimeAttribute?.DisplayName))
        {
            return runtimeAttribute.DisplayName;
        }

        SaveFieldAttribute saveAttribute =
            field.GetCustomAttribute<SaveFieldAttribute>();

        if (!string.IsNullOrEmpty(saveAttribute?.DisplayName))
        {
            return saveAttribute.DisplayName;
        }

        return this.GetDisplayName(field.Name);
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

        return fields
            .OrderByDescending(field =>
                field.GetCustomAttribute<SaveFieldAttribute>().Priority)
            .ToList();
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

        if (type.IsEnum)
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

            return typeof(SaveableEntity).IsAssignableFrom(elementType);
        }

        return false;
    }

    private string GetDisplayName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return fieldName;
        }

        return char.ToUpper(fieldName[0]) +
               fieldName.Substring(1) +
               ":";
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
            Instantiate(prefab, this.propertyContainer);

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

    /// <summary>
    /// Captures the current selection and camera position for the navigation history.
    /// </summary>
    /// <returns>The state to push before navigating.</returns>
    private NavigationState CaptureNavigationState()
    {
        MapEditorManager manager = MapEditorManager.Instance;

        NavigationState state = new NavigationState
        {
            SelectedEntities = manager.SelectedEntities != null
                ? new List<SaveableEntity>(manager.SelectedEntities)
                : new List<SaveableEntity>(),

            SelectedTiles = manager.SelectedTiles != null
                ? new List<Vector2Int>(manager.SelectedTiles)
                : new List<Vector2Int>(),
        };

        if (this.cameraController != null)
        {
            state.CameraPosition =
                this.cameraController.transform.position;

            state.HasCameraPosition = true;
        }

        return state;
    }

    private SaveableEntity GetConsumerInWorld(SaveableEntity entity)
    {
        SaveableEntity current = entity;
        int safety = 32;

        while (current.ConsumedBy != null && safety-- > 0)
        {
            current = current.ConsumedBy;
        }

        SaveableEntity representative = current.GetWorldRepresentative();

        return representative != null ? representative : current;
    }

    private SaveableEntity GetCameraTarget(SaveableEntity entity)
    {
        return entity.ConsumedBy != null
            ? this.GetConsumerInWorld(entity)
            : entity.GetWorldRepresentative();
    }
}
