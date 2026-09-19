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
    [SerializeField]
    private TextMeshProUGUI label;
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Toggle toggle;

    [Header("Slider (optional)")]
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private TextMeshProUGUI sliderMinLabel;
    [SerializeField]
    private TextMeshProUGUI sliderValueLabel;
    [SerializeField]
    private TextMeshProUGUI sliderMaxLabel;

    [Header("References (optional)")]
    [SerializeField]
    private Button referenceButton;
    [SerializeField]
    private Transform referenceContainer;
    [SerializeField]
    private Button referenceButtonPrefab;
    [SerializeField]
    private Button addReferenceButton;

    private FieldInfo field;
    private List<object> targets;
    private Action onValueChanged;
    private bool updating;

    /// <summary>
    /// When set, the values live on <see cref="BuilderEntity"/> runtime editable
    /// values and are read and written through the entity instead of the field.
    /// The field then only describes the type, it is not a field of the target.
    /// </summary>
    private string builderFieldName;

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
            this.label.text = displayName;
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
            this.label.text = displayName;
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

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            SaveableEntity owner = ResolveOwner(target);

            SaveableEntity previous =
                this.GetFieldValue(target) as SaveableEntity;

            if (previous != null &&
                owner != null &&
                previous.OwnedBy == owner)
            {
                previous.ReleaseOwner();
            }

            this.SetValue(target, entity);

            if (attribute != null &&
                !attribute.ReferenceOnly &&
                owner != null)
            {
                entity.SetOwner(owner);
            }
        }

        this.UpdateReferenceButton();
        this.onValueChanged?.Invoke();
    }

    /// <summary>
    /// Replaces one element of the referenced list.
    /// </summary>
    /// <param name="index">The list index to replace.</param>
    /// <param name="entity">The new entity.</param>
    public void SetReference(int index, SaveableEntity entity)
    {
        if (!this.CanReference(entity) ||
            !this.IsReferenceList() ||
            this.targets == null)
        {
            return;
        }

        if (this.WarnIfBuilderList())
        {
            return;
        }

        SaveFieldAttribute attribute =
            this.field.GetCustomAttribute<SaveFieldAttribute>();

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            IList list =
                this.GetFieldValue(target) as IList;

            if (list == null ||
                index < 0 ||
                index >= list.Count)
            {
                continue;
            }

            SaveableEntity owner = ResolveOwner(target);

            SaveableEntity previous =
                list[index] as SaveableEntity;

            if (previous != null &&
                owner != null &&
                previous.OwnedBy == owner)
            {
                previous.ReleaseOwner();
            }

            list[index] = entity;

            if (attribute != null &&
                !attribute.ReferenceOnly &&
                owner != null)
            {
                entity.SetOwner(owner);
            }
        }

        this.SetupReferenceList();
        this.onValueChanged?.Invoke();
    }

    /// <summary>
    /// Adds entities to the referenced list. Entities already in the list
    /// and entities of the wrong type are skipped.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    public void AddReferences(
        IReadOnlyList<SaveableEntity> entities)
    {
        if (entities == null ||
            entities.Count == 0 ||
            !this.IsReferenceList() ||
            this.targets == null)
        {
            return;
        }

        if (this.WarnIfBuilderList())
        {
            return;
        }

        SaveFieldAttribute attribute =
            this.field.GetCustomAttribute<SaveFieldAttribute>();

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

            SaveableEntity owner = ResolveOwner(target);

            foreach (SaveableEntity entity in entities)
            {
                if (!this.CanReference(entity) ||
                    list.Contains(entity))
                {
                    continue;
                }

                list.Add(entity);

                if (attribute != null &&
                    !attribute.ReferenceOnly &&
                    owner != null)
                {
                    entity.SetOwner(owner);
                }
            }
        }

        this.SetupReferenceList();
        this.onValueChanged?.Invoke();
    }

    /// <summary>
    /// Whether an entity fits a required field type. Entities in the builder
    /// are <see cref="BuilderEntity"/> proxies, so for those the class of the
    /// prefab they stand for is checked instead of the proxy itself.
    /// </summary>
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

    private static string FormatNumber(float value)
    {
        return value.ToString(
            "0.##",
            CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Lists on builder entities are not supported yet: the typed list of
    /// the real class cannot hold builder proxies.
    /// </summary>
    /// <returns>True when the field is a list on builder entities.</returns>
    private bool WarnIfBuilderList()
    {
        if (this.builderFieldName == null)
        {
            return false;
        }

        Debug.LogWarning(
            $"RuntimePropertyField: reference lists on builder " +
            $"entities are not supported yet ({this.builderFieldName}).");

        return true;
    }

    /// <summary>
    /// Gets the slider range of the field, if it has one.
    /// </summary>
    private bool TryGetRange(out float min, out float max)
    {
        // TODO: if BuilderEntity runtime editable values carry their own
        // min/max, read them here as well.
        SaveFieldAttribute attribute =
            this.field != null
                ? this.field.GetCustomAttribute<SaveFieldAttribute>()
                : null;

        if (attribute != null &&
            attribute.HasRange)
        {
            min = attribute.Min;
            max = attribute.Max;
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

        if (this.referenceButton != null)
        {
            this.referenceButton.gameObject.SetActive(false);
        }

        if (this.referenceContainer != null)
        {
            this.referenceContainer.gameObject.SetActive(false);
        }

        if (this.addReferenceButton != null)
        {
            this.addReferenceButton.gameObject.SetActive(false);
        }

        if (this.field == null ||
            this.targets == null ||
            this.targets.Count == 0)
        {
            return;
        }

        Type type = this.field.FieldType;

        if (this.IsReferenceList())
        {
            this.SetupReferenceList();
            return;
        }

        if (typeof(SaveableEntity).IsAssignableFrom(type))
        {
            this.SetupReference();
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

        if (this.sliderMinLabel != null)
        {
            this.sliderMinLabel.text = FormatNumber(min);
        }

        if (this.sliderMaxLabel != null)
        {
            this.sliderMaxLabel.text = FormatNumber(max);
        }

        this.RefreshSlider();

        this.slider.onValueChanged.AddListener(this.OnSliderChanged);
    }

    private void SetupReference()
    {
        if (this.referenceButton == null)
        {
            return;
        }

        this.referenceButton.gameObject.SetActive(true);
        this.referenceButton.onClick.RemoveAllListeners();
        this.referenceButton.onClick.AddListener(
            this.BeginReferenceSelection);

        this.UpdateReferenceButton();
    }

    private void SetupReferenceList()
    {
        if (this.referenceContainer == null ||
            this.referenceButtonPrefab == null ||
            this.addReferenceButton == null ||
            this.targets == null ||
            this.targets.Count == 0)
        {
            return;
        }

        this.referenceContainer.gameObject.SetActive(true);
        this.addReferenceButton.gameObject.SetActive(true);

        this.addReferenceButton.onClick.RemoveAllListeners();
        this.addReferenceButton.onClick.AddListener(
            this.BeginReferenceSelection);

        this.ClearReferenceButtons();

        IList list =
            this.GetFieldValue(this.targets[0]) as IList;

        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                SaveableEntity entity = list[i] as SaveableEntity;

                Button button =
                    Instantiate(
                        this.referenceButtonPrefab,
                        this.referenceContainer);

                int index = i;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(
                    () => this.BeginReferenceSelection(index));

                TextMeshProUGUI buttonText =
                    button.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText != null)
                {
                    buttonText.text =
                        entity != null
                            ? entity.name
                            : "None";
                }
            }
        }

        this.RebuildLayout();
    }

    private void ClearReferenceButtons()
    {
        if (this.referenceContainer == null)
        {
            return;
        }

        for (int i = this.referenceContainer.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child = this.referenceContainer.GetChild(i);

            // Destroy only takes effect at the end of the frame. Detach first,
            // so the old button no longer counts towards the row height.
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Recalculates the layout so the row and the inspector around it resize
    /// to fit the current number of reference buttons.
    /// </summary>
    private void RebuildLayout()
    {
        RectTransform row = this.transform as RectTransform;

        if (row == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(row);

        RectTransform parent = row.parent as RectTransform;

        if (parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
    }

    private void UpdateReferenceButton()
    {
        if (this.referenceButton == null ||
            this.targets == null ||
            this.targets.Count == 0 ||
            this.field == null)
        {
            return;
        }

        SaveableEntity entity =
            this.GetFieldValue(this.targets[0])
                as SaveableEntity;

        TextMeshProUGUI buttonText =
            this.referenceButton
                .GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            buttonText.text =
                entity != null
                    ? entity.name
                    : "None";
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
            RuntimePropertyEditor.Instance.BeginReferenceSelection(this, index);
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

        return this.GetFieldValue(this.targets[0]);
    }

    /// <summary>
    /// Writes a value to one target.
    /// </summary>
    private void SetValue(object target, object value)
    {
        if (this.builderFieldName != null &&
            target is BuilderEntity builderEntity)
        {
            builderEntity.SetRuntimeEditableValue(
                this.builderFieldName,
                value);
            return;
        }

        this.field.SetValue(target, value);
    }

    private void OnInputChanged(string value)
    {
        if (this.updating)
        {
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
            int rounded = Mathf.RoundToInt(value);
            result = rounded;
            this.UpdateSliderValueLabel(rounded);
        }
        else
        {
            result = value;
            this.UpdateSliderValueLabel(value);
        }

        this.ApplyValue(result);
    }

    private void ApplyValue(object value)
    {
        if (this.targets == null)
        {
            return;
        }

        if ((value is int || value is float) &&
            this.TryGetRange(out float min, out float max))
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

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            this.SetValue(target, value);
        }

        this.onValueChanged?.Invoke();
    }

    private void RefreshInput()
    {
        if (this.inputField == null)
        {
            return;
        }

        object value = this.GetCurrentValue();

        this.updating = true;
        this.inputField.SetTextWithoutNotify(
            value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : string.Empty);
        this.updating = false;
    }

    private void RefreshSlider()
    {
        object value = this.GetCurrentValue();

        if (this.slider == null ||
            !(value is int || value is float))
        {
            return;
        }

        float sliderValue = Convert.ToSingle(value);

        this.updating = true;
        this.slider.SetValueWithoutNotify(sliderValue);
        this.updating = false;

        this.UpdateSliderValueLabel(sliderValue);
    }

    private void UpdateSliderValueLabel(float value)
    {
        if (this.sliderValueLabel != null)
        {
            this.sliderValueLabel.text = FormatNumber(value);
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
            bool.TryParse(value, out bool boolValue))
        {
            result = boolValue;
            return true;
        }

        return false;
    }
}