using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents a field that can be edited in the editor.
/// </summary>
public class RuntimePropertyField : MonoBehaviour
{
    /// <summary>
    /// Text shown for a value that differs between the selected targets.
    /// </summary>
    private const string MixedValuePlaceholder = "-";

    /// <summary>
    /// Font size of a label that fits on one line.
    /// </summary>
    private const float LabelFontSize = 27f;

    /// <summary>
    /// Font size of a label that is too wide for one line. Both sizes are a
    /// multiple of 9 so the pixel font stays consistent.
    /// </summary>
    private const float SmallLabelFontSize = 18f;

    [SerializeField]
    private SpriteFontText label;
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private SpriteFontText inputText;
    [SerializeField]
    private Toggle toggle;

    [Header("Slider (optional)")]
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private TMP_InputField sliderInputField;
    [SerializeField]
    private SpriteFontText sliderInputText;

    [Header("References (optional)")]
    [SerializeField]
    private ReferenceButton referenceButtonPrefab;
    [SerializeField]
    private Button addReferenceButton;
    [SerializeField]
    private Transform referenceContainer;

    [Header("Enum (optional)")]
    [SerializeField]
    private GameObject enumSelector;
    [SerializeField]
    private Button enumPreviousButton;
    [SerializeField]
    private Button enumNextButton;
    [SerializeField]
    private SpriteFontText enumText;

    private FieldInfo field;
    private List<object> targets;
    private Action onValueChanged;
    private bool updating;
    private string builderFieldName;

    /// <summary>
    /// Gets the name of the field as the owner knows it. Builder entities use the
    /// runtime editable name, everything else uses the real field name.
    /// </summary>
    private string ConsumingFieldName => this.builderFieldName ?? this.field.Name;

    /// <summary>
    /// Gets the transform the reference buttons are spawned under. Falls back to
    /// the row itself when no container is assigned.
    /// </summary>
    private Transform ReferenceParent => this.referenceContainer != null
        ? this.referenceContainer
        : this.transform;

    /// <summary>
    /// Initialize the property field.
    /// </summary>
    /// <param name="displayName">The name to display the property as.</param>
    /// <param name="field">The field to edit.</param>
    /// <param name="targets">The objects to set the field on.</param>
    /// <param name="onValueChanged">An action to invoke when the field has been changed.</param>
    public void Initialize(
            string displayName,
            FieldInfo field,
            List<object> targets,
            Action onValueChanged = null)
    {
        this.field = field;
        this.targets = targets;
        this.builderFieldName = null;

        this.onValueChanged = onValueChanged;

        if (this.label != null)
        {
            this.label.SetText(displayName);
            this.label.FitOnOneLine(LabelFontSize, SmallLabelFontSize);

            HoverInfoTarget.Attach(
                this.label.gameObject,
                HoverInfo.ForField(field, displayName));
        }

        this.Setup();
    }

    /// <summary>
    /// Initialize the property field.
    /// </summary>
    /// <param name="displayName">The name to display the property as.</param>
    /// <param name="fieldName">The name of the field to edit.</param>
    /// <param name="targets">The objects to set the field on.</param>
    /// <param name="onValueChanged">An action to invoke when the field has been changed.</param>
    public void Initialize(
            string displayName,
            string fieldName,
            List<BuilderEntity> targets,
            Action onValueChanged = null)
    {
        if (targets == null ||
            targets.Count == 0)
        {
            return;
        }

        if (!targets[0].TryGetRuntimeEditableField(
                fieldName,
                out RuntimeEditableValue editableValue))
        {
            return;
        }

        if (editableValue == null ||
            editableValue.Field == null)
        {
            return;
        }

        this.field = editableValue.Field;
        this.targets = new List<object>(targets);
        this.builderFieldName = fieldName;

        this.onValueChanged = onValueChanged;

        if (this.label != null)
        {
            this.label.SetText(displayName);
            this.label.FitOnOneLine(LabelFontSize, SmallLabelFontSize);

            HoverInfoTarget.Attach(
                this.label.gameObject,
                HoverInfo.ForField(editableValue.Field, displayName));
        }

        this.Setup();
    }

    /// <summary>
    /// Whether the given entity can be stored in this field
    /// (or added to it, for lists).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True when the entity has a fitting type.</returns>
    public bool CanReference(SaveableEntity entity)
    {
        if (this.field == null ||
            entity == null)
        {
            return false;
        }

        foreach (object target in this.targets)
        {
            SaveableEntity owner = ResolveOwner(target);

            if (owner == entity)
            {
                return false;
            }
        }

        if (this.IsReferenceList())
        {
            return FitsType(
                this.field.FieldType.GetGenericArguments()[0],
                entity);
        }

        return typeof(SaveableEntity).IsAssignableFrom(this.field.FieldType) &&
               FitsType(this.field.FieldType, entity);
    }

    /// <summary>
    /// Whether this field holds a list of entity references.
    /// </summary>
    /// <returns>True when the field is a list of entities.</returns>
    public bool IsReferenceList()
    {
        if (this.field == null ||
            !typeof(IList).IsAssignableFrom(
                this.field.FieldType) ||
            !this.field.FieldType.IsGenericType)
        {
            return false;
        }

        Type elementType =
            this.field.FieldType.GetGenericArguments()[0];

        return typeof(SaveableEntity).IsAssignableFrom(
            elementType);
    }

    /// <summary>
    /// Gets the maximum number of references this field can contain.
    /// </summary>
    /// <returns>The maximum number of references, or -1 for unlimited.</returns>
    public int GetMaxReferences()
    {
        if (this.field == null)
        {
            return 0;
        }

        SaveFieldAttribute attribute =
            this.field.GetCustomAttribute<SaveFieldAttribute>();

        if (!this.IsReferenceList())
        {
            return 1;
        }

        return attribute != null
            ? attribute.MaxReferences
            : -1;
    }

    /// <summary>
    /// Gets the reference at one visible index from every target, so a reference
    /// button can select all of them for multi-editing. Targets that have no
    /// reference at that index are skipped.
    /// </summary>
    /// <param name="index">The visible reference index that was clicked.</param>
    /// <returns>The referenced entities, without duplicates.</returns>
    public List<SaveableEntity> GetReferences(int index)
    {
        List<SaveableEntity> references = new List<SaveableEntity>();

        if (this.targets == null ||
            index < 0)
        {
            return references;
        }

        foreach (object target in this.targets)
        {
            SaveableEntity entity =
                this.GetReference(target, index);

            if (entity != null &&
                !references.Contains(entity))
            {
                references.Add(entity);
            }
        }

        return references;
    }

    /// <summary>
    /// Sets the single entity this field references.
    /// </summary>
    /// <param name="entity">The entity to reference.</param>
    public void SetReference(SaveableEntity entity)
    {
        if (!this.CanReference(entity) ||
            this.IsReferenceList() ||
            this.targets == null)
        {
            return;
        }

        SaveFieldAttribute attribute =
            this.field.GetCustomAttribute<SaveFieldAttribute>();

        ReferenceSnapshot before = this.CaptureReferenceState();
        List<SaveableEntity> deleted = new List<SaveableEntity>();

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            SaveableEntity owner = ResolveOwner(target);

            if (attribute != null &&
                attribute.ReferenceMode == ReferenceMode.Consuming &&
                entity.ConsumedBy != null &&
                entity.ConsumedBy != owner)
            {
                continue;
            }

            SaveableEntity previous =
                this.GetFieldValue(target) as SaveableEntity;

            if (previous != null &&
                owner != null &&
                previous.ConsumedBy == owner)
            {
                previous.ReleaseConsumedBy(owner);
            }

            this.SetValue(target, entity);

            if (attribute != null &&
                attribute.ReferenceMode == ReferenceMode.Consuming &&
                owner != null)
            {
                entity.SetConsumedBy(owner, this.ConsumingFieldName);
            }
        }

        this.FinishReferenceChange(before, deleted);
    }

    /// <summary>
    /// Replaces one element of the referenced list.
    /// The supplied index is the visible/UI index, not necessarily the
    /// underlying list index because consumed references may be hidden.
    /// </summary>
    /// <param name="index">The visible list index to replace.</param>
    /// <param name="entity">The new entity.</param>
    public void SetReference(int index, SaveableEntity entity)
    {
        if (!this.CanReference(entity) ||
            !this.IsReferenceList() ||
            this.targets == null)
        {
            return;
        }

        SaveFieldAttribute attribute =
            this.field.GetCustomAttribute<SaveFieldAttribute>();

        ReferenceSnapshot before = this.CaptureReferenceState();
        List<SaveableEntity> deleted = new List<SaveableEntity>();

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            IList list =
                this.GetFieldValue(target) as IList;

            if (list == null)
            {
                continue;
            }

            int actualIndex =
                this.GetActualReferenceIndex(target, index);

            if (actualIndex < 0 ||
                actualIndex >= list.Count)
            {
                continue;
            }

            SaveableEntity owner = ResolveOwner(target);

            if (attribute != null &&
                attribute.ReferenceMode == ReferenceMode.Consuming &&
                entity.ConsumedBy != null &&
                entity.ConsumedBy != owner)
            {
                continue;
            }

            SaveableEntity previous =
                list[actualIndex] as SaveableEntity;

            if (previous != null &&
                owner != null &&
                previous.ConsumedBy == owner)
            {
                previous.ReleaseConsumedBy(owner);
            }

            list[actualIndex] = entity;

            if (attribute != null &&
                attribute.ReferenceMode == ReferenceMode.Consuming &&
                owner != null)
            {
                entity.SetConsumedBy(owner, this.ConsumingFieldName);
            }
        }

        this.FinishReferenceChange(before, deleted);
    }

    /// <summary>
    /// Adds entities to the referenced list. Entities already in the list
    /// and entities of the wrong type are skipped.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    public void AddReferences(IReadOnlyList<SaveableEntity> entities)
    {
        if (entities == null ||
            entities.Count == 0 ||
            this.targets == null)
        {
            return;
        }

        SaveFieldAttribute attribute =
            this.field.GetCustomAttribute<SaveFieldAttribute>();
        ReferenceSnapshot before = this.CaptureReferenceState();
        List<SaveableEntity> deleted = new List<SaveableEntity>();

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            SaveableEntity owner = ResolveOwner(target);

            if (this.IsReferenceList())
            {
                IList list =
                    this.GetFieldValue(target) as IList;

                if (list == null)
                {
                    continue;
                }

                int maxReferences = this.GetMaxReferences();

                foreach (SaveableEntity entity in entities)
                {
                    if (!this.CanReference(entity) ||
                        list.Contains(entity) ||
                        (maxReferences >= 0 &&
                         this.GetVisibleReferenceCount(target) >= maxReferences))
                    {
                        continue;
                    }

                    if (attribute != null &&
                        attribute.ReferenceMode == ReferenceMode.Consuming &&
                        entity.ConsumedBy != null &&
                        entity.ConsumedBy != owner)
                    {
                        continue;
                    }

                    list.Add(entity);

                    if (attribute != null &&
                        attribute.ReferenceMode == ReferenceMode.Consuming &&
                        owner != null)
                    {
                        entity.SetConsumedBy(owner, this.ConsumingFieldName);
                    }
                }
            }
            else
            {
                SaveableEntity previous =
                    this.GetFieldValue(target) as SaveableEntity;

                if (previous != null)
                {
                    continue;
                }

                SaveableEntity entity = entities[0];

                if (!this.CanReference(entity))
                {
                    continue;
                }

                if (attribute != null &&
                    attribute.ReferenceMode == ReferenceMode.Consuming &&
                    entity.ConsumedBy != null &&
                    entity.ConsumedBy != owner)
                {
                    continue;
                }

                this.SetValue(target, entity);

                if (attribute != null &&
                    attribute.ReferenceMode == ReferenceMode.Consuming &&
                    owner != null)
                {
                    entity.SetConsumedBy(owner, this.ConsumingFieldName);
                }
            }
        }

        this.FinishReferenceChange(before, deleted);
    }

    private static string FormatNumber(float value)
    {
        return value.ToString(
            "0.##",
            CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Removes one reference.
    /// The supplied index is the visible/UI index.
    /// </summary>
    /// <param name="index">The visible reference index.</param>
    private void RemoveReference(int index)
    {
        if (this.targets == null ||
            this.field == null)
        {
            return;
        }

        SaveFieldAttribute attribute =
            this.field.GetCustomAttribute<SaveFieldAttribute>();
        ReferenceSnapshot before = this.CaptureReferenceState();
        List<SaveableEntity> deleted = new List<SaveableEntity>();

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            SaveableEntity owner = ResolveOwner(target);

            if (this.IsReferenceList())
            {
                IList list =
                    this.GetFieldValue(target) as IList;

                if (list == null)
                {
                    continue;
                }

                int actualIndex =
                    this.GetActualReferenceIndex(target, index);

                if (actualIndex < 0 ||
                    actualIndex >= list.Count)
                {
                    continue;
                }

                SaveableEntity entity =
                    list[actualIndex] as SaveableEntity;

                list.RemoveAt(actualIndex);

                if (entity != null &&
                    attribute != null &&
                    attribute.ReferenceMode == ReferenceMode.Consuming &&
                    entity.ConsumedBy == owner)
                {
                    deleted.Add(entity);
                }
            }
            else
            {
                SaveableEntity entity =
                    this.GetFieldValue(target) as SaveableEntity;

                this.SetValue(target, null);

                if (entity != null &&
                    attribute != null &&
                    attribute.ReferenceMode == ReferenceMode.Consuming &&
                    entity.ConsumedBy == owner)
                {
                    deleted.Add(entity);
                }
            }
        }

        this.FinishReferenceChange(before, deleted);
    }

    /// <summary>
    /// Whether an entity fits a required field type. Entities in the builder
    /// are <see cref="BuilderEntity"/> proxies, so for those the class of the
    /// prefab they stand for is checked instead of the proxy itself.
    /// </summary>
    /// <param name="required">The required type.</param>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True when the entity fits the required type.</returns>
    private static bool FitsType(Type required, SaveableEntity entity)
    {
        if (required == null ||
            entity == null)
        {
            return false;
        }

        if (entity is BuilderEntity builder)
        {
            GameObject prefab =
                SaveRegistry.GetPrefab(builder.PrefabID);

            SaveableEntity source =
                prefab != null
                    ? prefab.GetComponent<SaveableEntity>()
                    : null;

            return source != null &&
                   required.IsInstanceOfType(source);
        }

        return required.IsInstanceOfType(entity);
    }

    /// <summary>
    /// Gets the SaveableEntity that owns the edited object, if there is one.
    /// The edited object can be an entity itself, or a component on an entity.
    /// Plain objects (for example tile behaviours) have no owner.
    /// </summary>
    private static SaveableEntity ResolveOwner(object target)
    {
        if (target is SaveableEntity entity)
        {
            return entity;
        }

        if (target is Component component)
        {
            return component.GetComponent<SaveableEntity>();
        }

        return null;
    }

    private static void WriteFieldValue(
        FieldInfo field,
        string builderFieldName,
        object target,
        object value)
    {
        if (builderFieldName != null &&
            target is BuilderEntity builderEntity)
        {
            builderEntity.SetRuntimeEditableValue(builderFieldName, value);
            return;
        }

        field.SetValue(target, value);
    }

    /// <summary>
    /// Determines whether a reference should be visible in this property field.
    ///
    /// An unconsumed reference is visible.
    /// A reference consumed by this field's owner is also visible.
    /// A reference consumed by a different owner is hidden.
    /// </summary>
    private bool IsReferenceVisible(
        SaveableEntity entity,
        SaveableEntity owner)
    {
        if (entity == null || !entity.gameObject.activeSelf)
        {
            return false;
        }

        if (entity.ConsumedBy == null)
        {
            return true;
        }

        return entity.ConsumedBy == owner;
    }

    /// <summary>
    /// Gets the number of references that should actually be displayed.
    /// Consumed references belonging to another owner are excluded.
    /// </summary>
    private int GetVisibleReferenceCount(object target)
    {
        if (target == null)
        {
            return 0;
        }

        SaveableEntity owner =
            ResolveOwner(target);

        if (!this.IsReferenceList())
        {
            SaveableEntity entity =
                this.GetFieldValue(target) as SaveableEntity;

            return this.IsReferenceVisible(entity, owner)
                ? 1
                : 0;
        }

        IList list =
            this.GetFieldValue(target) as IList;

        if (list == null)
        {
            return 0;
        }

        int count = 0;

        foreach (object value in list)
        {
            SaveableEntity entity =
                value as SaveableEntity;

            if (this.IsReferenceVisible(entity, owner))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Converts a visible/UI reference index into the actual index in the
    /// underlying list.
    /// </summary>
    private int GetActualReferenceIndex(
        object target,
        int visibleIndex)
    {
        if (target == null ||
            visibleIndex < 0)
        {
            return -1;
        }

        SaveableEntity owner =
            ResolveOwner(target);

        IList list =
            this.GetFieldValue(target) as IList;

        if (list == null)
        {
            return -1;
        }

        int currentVisibleIndex = 0;

        for (int i = 0; i < list.Count; i++)
        {
            SaveableEntity entity =
                list[i] as SaveableEntity;

            if (!this.IsReferenceVisible(entity, owner))
            {
                continue;
            }

            if (currentVisibleIndex == visibleIndex)
            {
                return i;
            }

            currentVisibleIndex++;
        }

        return -1;
    }

    /// <summary>
    /// Gets the slider range of the field, if it has one. Fields that are
    /// marked with a <see cref="SaveFieldAttribute"/> range use that one,
    /// fields that are only runtime editable use their range instead.
    /// </summary>
    private bool TryGetRange(out float min, out float max)
    {
        SaveFieldAttribute saveAttribute =
            this.field != null
                ? this.field.GetCustomAttribute<SaveFieldAttribute>()
                : null;

        if (saveAttribute != null &&
            saveAttribute.HasRange)
        {
            min = saveAttribute.Min;
            max = saveAttribute.Max;
            return true;
        }

        RuntimeEditableAttribute runtimeAttribute =
            this.field != null
                ? this.field.GetCustomAttribute<RuntimeEditableAttribute>()
                : null;

        if (runtimeAttribute != null &&
            runtimeAttribute.HasRange)
        {
            min = runtimeAttribute.Min;
            max = runtimeAttribute.Max;
            return true;
        }

        min = 0f;
        max = 0f;
        return false;
    }

    private void Setup()
    {
        if (this.inputField != null)
        {
            this.inputField.gameObject.SetActive(false);
            this.inputField.onEndEdit.RemoveAllListeners();
        }

        if (this.toggle != null)
        {
            this.toggle.gameObject.SetActive(false);
            this.toggle.onValueChanged.RemoveAllListeners();
        }

        if (this.slider != null)
        {
            this.slider.gameObject.SetActive(false);
            this.slider.onValueChanged.RemoveAllListeners();
        }

        if (this.sliderInputField != null)
        {
            this.sliderInputField.gameObject.SetActive(false);
            this.sliderInputField.onEndEdit.RemoveAllListeners();
        }

        if (this.addReferenceButton != null)
        {
            this.addReferenceButton.gameObject.SetActive(false);
        }

        if (this.enumSelector != null)
        {
            this.enumSelector.SetActive(false);
        }

        if (this.enumPreviousButton != null)
        {
            this.enumPreviousButton.onClick.RemoveAllListeners();
        }

        if (this.enumNextButton != null)
        {
            this.enumNextButton.onClick.RemoveAllListeners();
        }

        if (this.field == null ||
            this.targets == null ||
            this.targets.Count == 0)
        {
            return;
        }

        Type type = this.field.FieldType;

        if (this.IsReferenceList() ||
            typeof(SaveableEntity).IsAssignableFrom(type))
        {
            this.SetupReferenceList();
            return;
        }

        if (type.IsEnum)
        {
            this.SetupEnum();
            return;
        }

        if (type == typeof(bool))
        {
            this.SetupBool();
            return;
        }

        if ((type == typeof(int) || type == typeof(float)) &&
            this.slider != null &&
            this.TryGetRange(out float min, out float max))
        {
            this.SetupSlider(min, max);
            return;
        }

        this.SetupInput();
    }

    private void SetupBool()
    {
        if (this.toggle == null)
        {
            return;
        }

        this.toggle.gameObject.SetActive(true);

        this.updating = true;
        this.toggle.isOn = Convert.ToBoolean(this.GetCurrentValue());
        this.updating = false;

        this.toggle.onValueChanged.AddListener(this.OnToggleChanged);
    }

    private void SetupInput()
    {
        if (this.inputField == null)
        {
            return;
        }

        this.inputField.gameObject.SetActive(true);

        this.RefreshInput();

        this.inputField.onEndEdit.AddListener(this.OnInputChanged);
    }

    private void SetupSlider(float min, float max)
    {
        this.slider.gameObject.SetActive(true);

        this.slider.minValue = min;
        this.slider.maxValue = max;
        this.slider.wholeNumbers =
            this.field.FieldType == typeof(int);

        if (this.sliderInputField != null)
        {
            this.sliderInputField.gameObject.SetActive(true);
        }

        this.RefreshSlider();

        this.slider.onValueChanged.AddListener(this.OnSliderChanged);

        if (this.sliderInputField != null)
        {
            this.sliderInputField.onEndEdit.AddListener(this.OnSliderInputChanged);
        }
    }

    /// <summary>
    /// Shows the previous / next selector for an enum field.
    /// </summary>
    private void SetupEnum()
    {
        if (this.enumSelector == null)
        {
            return;
        }

        this.enumSelector.SetActive(true);

        if (this.enumPreviousButton != null)
        {
            this.enumPreviousButton.onClick.AddListener(
                () => this.CycleEnum(-1));
        }

        if (this.enumNextButton != null)
        {
            this.enumNextButton.onClick.AddListener(
                () => this.CycleEnum(1));
        }

        this.RefreshEnum();
    }

    /// <summary>
    /// Selects the previous or next value of the enum and wraps around at the ends.
    /// </summary>
    /// <param name="step">The number of values to move, negative to go back.</param>
    private void CycleEnum(int step)
    {
        Array values =
            Enum.GetValues(this.field.FieldType);

        int index =
            Math.Max(
                0,
                Array.IndexOf(
                    values,
                    this.GetCurrentValue()));

        index =
            (index + step + values.Length) % values.Length;

        this.ApplyValue(values.GetValue(index));
        this.RefreshEnum();
    }

    /// <summary>
    /// Shows the name of the current enum value.
    /// </summary>
    private void RefreshEnum()
    {
        if (this.enumText == null)
        {
            return;
        }

        object value =
            this.GetCurrentValue();

        this.enumText.SetText(
            this.HasMixedValues()
                ? MixedValuePlaceholder
                : value != null
                    ? value.ToString()
                    : string.Empty);
    }

    private void SetupReferenceList()
    {
        if (this.referenceButtonPrefab == null ||
            this.targets == null ||
            this.targets.Count == 0)
        {
            return;
        }

        this.ClearReferenceButtons();

        int referenceCount =
            this.GetReferenceCount();

        int maxReferences =
            this.GetMaxReferences();

        if (this.addReferenceButton != null)
        {
            this.addReferenceButton.gameObject.SetActive(
                maxReferences < 0 ||
                referenceCount < maxReferences);

            this.addReferenceButton.onClick.RemoveAllListeners();
            this.addReferenceButton.onClick.AddListener(
                this.BeginReferenceSelection);

            this.addReferenceButton.transform.SetAsLastSibling();
        }

        for (int i = 0; i < referenceCount; i++)
        {
            SaveableEntity entity =
                this.GetReference(i);

            if (entity == null)
            {
                continue;
            }

            ReferenceButton button =
                Instantiate(
                    this.referenceButtonPrefab,
                    this.ReferenceParent);

            int index = i;

            button.SetEntity(entity);
            button.SetReferenceTargets(this.GetReferences(index));

            button.SetConsumed(
                entity.ConsumedBy != null);

            button.SetRemoveAction(
                () => this.RemoveReference(index));
        }

        if (this.addReferenceButton != null)
        {
            this.addReferenceButton.transform.SetAsLastSibling();
        }

        this.RebuildLayout();
    }

    private int GetReferenceCount()
    {
        if (this.targets == null ||
            this.targets.Count == 0)
        {
            return 0;
        }

        return this.GetVisibleReferenceCount(
            this.targets[0]);
    }

    private SaveableEntity GetReference(int index)
    {
        if (this.targets == null ||
            this.targets.Count == 0 ||
            index < 0)
        {
            return null;
        }

        return this.GetReference(this.targets[0], index);
    }

    private SaveableEntity GetReference(object target, int index)
    {
        if (target == null ||
            index < 0)
        {
            return null;
        }

        if (!this.IsReferenceList())
        {
            if (index != 0)
            {
                return null;
            }

            SaveableEntity entity =
                this.GetFieldValue(target) as SaveableEntity;

            return this.IsReferenceVisible(
                entity,
                ResolveOwner(target))
                    ? entity
                    : null;
        }

        IList list =
            this.GetFieldValue(target) as IList;

        if (list == null)
        {
            return null;
        }

        int actualIndex =
            this.GetActualReferenceIndex(
                target,
                index);

        if (actualIndex < 0 ||
            actualIndex >= list.Count)
        {
            return null;
        }

        return list[actualIndex] as SaveableEntity;
    }

    private void ClearReferenceButtons()
    {
        Transform parent =
            this.ReferenceParent;

        for (int i = parent.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                parent.GetChild(i);

            if ((this.addReferenceButton != null &&
                 child == this.addReferenceButton.transform) ||
                (this.label != null &&
                 child == this.label.transform))
            {
                continue;
            }

            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Recalculates the layout so the container, the row and the inspector
    /// around it resize to fit the current number of reference buttons.
    /// </summary>
    private void RebuildLayout()
    {
        RectTransform container =
            this.ReferenceParent as RectTransform;

        if (container != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        RectTransform row =
            this.transform as RectTransform;

        if (row == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(row);

        RectTransform parent =
            row.parent as RectTransform;

        if (parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
    }

    private void BeginReferenceSelection()
    {
        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.BeginReferenceSelection(this);
        }
    }

    private void BeginReferenceSelection(int index)
    {
        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.BeginReferenceSelection(
                this,
                index);
        }
    }

    /// <summary>
    /// Reads the value of the field on one target. For builder entities the
    /// value comes from the runtime editable values, because the field
    /// belongs to the prefab class and not to the builder entity.
    /// </summary>
    private object GetFieldValue(object target)
    {
        if (target == null)
        {
            return null;
        }

        if (this.builderFieldName != null &&
            target is BuilderEntity builderEntity)
        {
            return builderEntity.GetRuntimeEditableValue(
                this.builderFieldName);
        }

        return this.field.GetValue(target);
    }

    /// <summary>
    /// Reads the value of the first target.
    /// </summary>
    private object GetCurrentValue()
    {
        if (this.targets == null ||
            this.targets.Count == 0)
        {
            return null;
        }

        return this.GetFieldValue(
            this.targets[0]);
    }

    /// <summary>
    /// Gets a value indicating whether the field has different values on the targets.
    /// </summary>
    /// <returns>True when at least two targets disagree.</returns>
    private bool HasMixedValues()
    {
        if (this.targets == null ||
            this.targets.Count < 2)
        {
            return false;
        }

        object first = this.GetFieldValue(this.targets[0]);

        for (int i = 1; i < this.targets.Count; i++)
        {
            if (!object.Equals(
                    first,
                    this.GetFieldValue(this.targets[i])))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Writes a value to one target.
    /// </summary>
    private void SetValue(object target, object value)
    {
        WriteFieldValue(this.field, this.builderFieldName, target, value);
    }

    private void OnInputChanged(string value)
    {
        if (this.updating)
        {
            return;
        }

        // The placeholder is not an edit, it only shows that the values differ.
        if (value == MixedValuePlaceholder &&
            this.HasMixedValues())
        {
            this.RefreshInput();
            return;
        }

        if (!this.TryConvert(
                value,
                this.field.FieldType,
                out object converted))
        {
            // Invalid text, show the real value again.
            this.RefreshInput();
            return;
        }

        this.ApplyValue(converted);
        this.RefreshInput();
    }

    /// <summary>
    /// Applies a number typed into the slider's input field. The value is only
    /// applied when it parses for the field type and lies inside the allowed
    /// range, otherwise a warning is shown and the slider and input show the
    /// real value again.
    /// </summary>
    /// <param name="value">The text typed into the slider's input field.</param>
    private void OnSliderInputChanged(string value)
    {
        if (this.updating)
        {
            return;
        }

        if (!this.TryGetRange(
                out float min,
                out float max))
        {
            this.RefreshSlider();
            return;
        }

        if (!this.TryConvert(
                value,
                this.field.FieldType,
                out object converted))
        {
            this.WarnSliderValue(min, max);
            this.RefreshSlider();
            return;
        }

        float numericValue =
            Convert.ToSingle(converted);

        if (float.IsNaN(numericValue) ||
            float.IsInfinity(numericValue) ||
            numericValue < min ||
            numericValue > max)
        {
            this.WarnSliderValue(min, max);
            this.RefreshSlider();
            return;
        }

        this.ApplyValue(converted);
        this.RefreshSlider();
    }

    /// <summary>
    /// Warns that a typed slider value is not allowed, naming the range.
    /// </summary>
    /// <param name="min">The lowest allowed value.</param>
    /// <param name="max">The highest allowed value.</param>
    private void WarnSliderValue(float min, float max)
    {
        string kind = this.field.FieldType == typeof(int)
            ? "whole number"
            : "number";

        WarningManager.Warn(
            $"Slider value must be a {kind} between {FormatNumber(min)} and {FormatNumber(max)}.");
    }

    private void OnToggleChanged(bool value)
    {
        if (this.updating)
        {
            return;
        }

        this.ApplyValue(value);
    }

    private void OnSliderChanged(float value)
    {
        if (this.updating)
        {
            return;
        }

        object result;

        if (this.field.FieldType == typeof(int))
        {
            int rounded =
                Mathf.RoundToInt(value);

            result = rounded;

            this.UpdateSliderValueLabel(
                rounded);
        }
        else
        {
            result = value;

            this.UpdateSliderValueLabel(
                value);
        }

        this.ApplyValue(result, true);
    }

    private void ApplyValue(object value, bool continuous = false)
    {
        if (this.targets == null)
        {
            return;
        }

        if ((value is int || value is float) &&
            this.TryGetRange(
                out float min,
                out float max))
        {
            float numericValue =
                Mathf.Clamp(
                    Convert.ToSingle(value),
                    min,
                    max);

            value =
                this.field.FieldType == typeof(int)
                    ? Mathf.RoundToInt(numericValue)
                    : (object)numericValue;
        }

        FieldInfo capturedField = this.field;
        string capturedBuilderName = this.builderFieldName;

        SetFieldValueAction action = new SetFieldValueAction(
            this.ConsumingFieldName,
            (target, newValue) => WriteFieldValue(
                capturedField,
                capturedBuilderName,
                target,
                newValue),
            null,
            continuous);

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            object before = this.GetFieldValue(target);

            this.SetValue(target, value);

            action.Add(target, before, this.GetFieldValue(target));
        }

        if (action.HasChanges &&
            MapEditorManager.Instance != null)
        {
            MapEditorManager.Instance.RegisterAction(action);
        }

        this.onValueChanged?.Invoke();
    }

    private void RefreshInput()
    {
        if (this.inputField == null)
        {
            return;
        }

        object value =
            this.GetCurrentValue();

        string text =
            this.HasMixedValues()
                ? MixedValuePlaceholder
                : value != null
                    ? Convert.ToString(
                        value,
                        CultureInfo.InvariantCulture)
                    : string.Empty;

        this.updating = true;

        this.inputField.SetTextWithoutNotify(text);

        this.updating = false;

        if (this.inputText != null)
        {
            this.inputText.SetText(text);
        }
    }

    private void RefreshSlider()
    {
        object value =
            this.GetCurrentValue();

        if (this.slider == null ||
            !(value is int || value is float))
        {
            return;
        }

        float sliderValue =
            Convert.ToSingle(value);

        this.updating = true;

        this.slider.SetValueWithoutNotify(
            sliderValue);

        this.updating = false;

        if (this.HasMixedValues())
        {
            this.SetSliderValueText(MixedValuePlaceholder);
            return;
        }

        this.UpdateSliderValueLabel(
            sliderValue);
    }

    /// <summary>
    /// Shows the given value in the slider's input field without triggering
    /// its end edit handler.
    /// </summary>
    /// <param name="value">The value to display.</param>
    private void UpdateSliderValueLabel(float value)
    {
        this.SetSliderValueText(
            FormatNumber(value));
    }

    /// <summary>
    /// Shows text in the slider's input field and label without triggering the handlers.
    /// </summary>
    /// <param name="text">The text to display.</param>
    private void SetSliderValueText(string text)
    {
        if (this.sliderInputField != null)
        {
            this.sliderInputField.SetTextWithoutNotify(text);
        }

        if (this.sliderInputText != null)
        {
            this.sliderInputText.SetText(text);
        }
    }

    /// <summary>
    /// Parses text with the invariant culture so "1.5" is 1.5 on every system.
    /// A decimal comma is accepted as well.
    /// </summary>
    private bool TryConvert(
            string value,
            Type type,
            out object result)
    {
        result = null;

        if (type == typeof(string))
        {
            result = value;
            return true;
        }

        if (type == typeof(int) &&
            int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int intValue))
        {
            result = intValue;
            return true;
        }

        if (type == typeof(float) &&
            float.TryParse(
                value.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float floatValue))
        {
            result = floatValue;
            return true;
        }

        if (type == typeof(bool) &&
            bool.TryParse(
                value,
                out bool boolValue))
        {
            result = boolValue;
            return true;
        }

        return false;
    }

    private ReferenceSnapshot CaptureReferenceState()
    {
        return ReferenceSnapshot.Capture(this.targets, this.field);
    }

    private void FinishReferenceChange(
        ReferenceSnapshot before,
        List<SaveableEntity> deleted)
    {
        EntityPresenceAction deletion =
            deleted.Count > 0
                ? EntityPresenceAction.DeleteNow(deleted)
                : null;

        ReferenceSnapshot after =
            ReferenceSnapshot.Capture(this.targets, this.field);

        MapEditorManager manager = MapEditorManager.Instance;

        if (manager != null &&
            (deletion != null || !before.IsSameAs(after)))
        {
            manager.BeginTransaction();

            if (deletion != null)
            {
                manager.RegisterAction(deletion);
            }

            manager.RegisterAction(new ReferenceStateAction(before, after));
            manager.EndTransaction();
        }

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.ForceRebuild();
            return;
        }

        this.SetupReferenceList();
        this.onValueChanged?.Invoke();
    }
}