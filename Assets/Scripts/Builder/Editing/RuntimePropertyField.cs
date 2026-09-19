using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private TextMeshProUGUI sliderMinLabel;
    [SerializeField]
    private TextMeshProUGUI sliderValueLabel;
    [SerializeField]
    private TextMeshProUGUI sliderMaxLabel;
    [SerializeField]
    private Button referenceButton;
    [SerializeField]
    private Transform referenceContainer;
    [SerializeField]
    private Button referenceButtonPrefab;

    private FieldInfo field;
    private List<object> targets;
    private Action onValueChanged;
    private bool updating;
    private List<BuilderEntity> builderTargets;
    private string builderFieldName;
    private bool usingBuilderValues;

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
        this.builderTargets = null;
        this.builderFieldName = null;
        this.usingBuilderValues = false;
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
        if (targets == null || targets.Count == 0)
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
        this.targets = null;
        this.builderTargets = targets;
        this.builderFieldName = fieldName;
        this.usingBuilderValues = true;
        this.onValueChanged = onValueChanged;

        if (this.label != null)
        {
            this.label.text = displayName;
        }

        this.Setup();
    }

    /// <summary>
    /// Sets the reference to this property field after choosing to select an entity.
    /// </summary>
    /// <param name="entity">The entity to select.</param>
    public void SetReference(SaveableEntity entity)
    {
        if (this.field == null ||
            !typeof(SaveableEntity).IsAssignableFrom(
                this.field.FieldType))
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

            SaveableEntity previous =
                this.field.GetValue(target) as SaveableEntity;

            if (previous != null &&
                previous.OwnedBy == (SaveableEntity)target)
            {
                previous.ReleaseOwner();
            }

            this.field.SetValue(target, entity);

            if (attribute != null &&
                !attribute.ReferenceOnly)
            {
                entity.SetOwner((SaveableEntity)target);
            }
        }

        this.UpdateReferenceButton();
        this.onValueChanged?.Invoke();
    }

    /// <summary>
    /// Sets a reference in a reference list.
    /// </summary>
    /// <param name="index">The index of the reference.</param>
    /// <param name="entity">The entity to select.</param>
    public void SetReference(int index, SaveableEntity entity)
    {
        if (this.field == null ||
            entity == null ||
            !this.IsReferenceList())
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

            IList list = this.field.GetValue(target) as IList;

            if (list == null ||
                index < 0 ||
                index >= list.Count)
            {
                continue;
            }

            SaveableEntity previous =
                list[index] as SaveableEntity;

            if (previous != null &&
                previous.OwnedBy == (SaveableEntity)target)
            {
                previous.ReleaseOwner();
            }

            list[index] = entity;

            if (attribute != null &&
                !attribute.ReferenceOnly)
            {
                entity.SetOwner((SaveableEntity)target);
            }
        }

        this.UpdateReferenceButtons();
        this.onValueChanged?.Invoke();
    }

    private void Setup()
    {
        if (this.field == null)
        {
            return;
        }

        Type type = this.field.FieldType;

        SaveFieldAttribute saveAttribute =
            this.field.GetCustomAttribute<SaveFieldAttribute>();

        bool isReference =
            typeof(SaveableEntity).IsAssignableFrom(type);

        bool isReferenceList =
            this.IsReferenceList();

        bool useSlider =
            (type == typeof(int) || type == typeof(float)) &&
            saveAttribute != null &&
            saveAttribute.HasRange &&
            saveAttribute.Min <= saveAttribute.Max;

        if (this.inputField != null)
        {
            this.inputField.gameObject.SetActive(
                !isReference &&
                !isReferenceList &&
                type != typeof(bool) &&
                !useSlider);

            this.inputField.onEndEdit.RemoveAllListeners();
        }

        if (this.toggle != null)
        {
            this.toggle.gameObject.SetActive(
                !isReference &&
                !isReferenceList &&
                type == typeof(bool));

            this.toggle.onValueChanged.RemoveAllListeners();
        }

        if (this.slider != null)
        {
            this.slider.gameObject.SetActive(
                !isReference &&
                !isReferenceList &&
                useSlider);

            this.slider.onValueChanged.RemoveAllListeners();
        }

        if (this.sliderMinLabel != null)
        {
            this.sliderMinLabel.gameObject.SetActive(
                !isReference &&
                !isReferenceList &&
                useSlider);
        }

        if (this.sliderValueLabel != null)
        {
            this.sliderValueLabel.gameObject.SetActive(
                !isReference &&
                !isReferenceList &&
                useSlider);
        }

        if (this.sliderMaxLabel != null)
        {
            this.sliderMaxLabel.gameObject.SetActive(
                !isReference &&
                !isReferenceList &&
                useSlider);
        }

        if (this.referenceButton != null)
        {
            this.referenceButton.gameObject.SetActive(isReference);
            this.referenceButton.onClick.RemoveAllListeners();

            if (isReference)
            {
                this.referenceButton.onClick.AddListener(
                    this.BeginReferenceSelection);

                this.UpdateReferenceButton();
            }
        }

        if (this.referenceContainer != null)
        {
            this.referenceContainer.gameObject.SetActive(isReferenceList);
        }

        if (isReferenceList)
        {
            this.SetupReferenceList();
            return;
        }

        if (isReference)
        {
            return;
        }

        if (type == typeof(bool))
        {
            this.SetupBool();
        }
        else if (useSlider)
        {
            this.SetupSlider(saveAttribute);
        }
        else
        {
            this.SetupInput();
        }
    }

    private void SetupReferenceList()
    {
        this.ClearReferenceButtons();

        if (this.referenceContainer == null ||
            this.referenceButtonPrefab == null)
        {
            return;
        }

        IList list =
            this.GetCurrentValue() as IList;

        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            int index = i;

            Button button =
                Instantiate(
                    this.referenceButtonPrefab,
                    this.referenceContainer);

            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () => this.BeginReferenceSelection(index));

            SaveableEntity entity =
                list[i] as SaveableEntity;

            TextMeshProUGUI buttonText =
                button.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
            {
                buttonText.text =
                    entity != null
                        ? entity.name
                        : "Select Reference";
            }
        }
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
            Destroy(this.referenceContainer.GetChild(i).gameObject);
        }
    }

    private void SetupBool()
    {
        if (this.toggle == null)
        {
            return;
        }

        object currentValue = this.GetCurrentValue();

        if (currentValue == null)
        {
            return;
        }

        bool value = Convert.ToBoolean(currentValue);

        this.updating = true;
        this.toggle.isOn = value;
        this.updating = false;

        this.toggle.onValueChanged.AddListener(this.OnToggleChanged);
    }

    private void SetupInput()
    {
        if (this.inputField == null)
        {
            return;
        }

        object value = this.GetCurrentValue();

        this.updating = true;
        this.inputField.text =
            value != null ? value.ToString() : string.Empty;
        this.updating = false;

        this.inputField.onEndEdit.AddListener(this.OnInputChanged);
    }

    private void SetupSlider(SaveFieldAttribute saveAttribute)
    {
        if (this.slider == null)
        {
            return;
        }

        object currentValue = this.GetCurrentValue();

        if (currentValue == null)
        {
            return;
        }

        float min = saveAttribute.Min;
        float max = saveAttribute.Max;
        float value = Convert.ToSingle(currentValue);

        this.slider.minValue = min;
        this.slider.maxValue = max;

        this.updating = true;
        this.slider.value = Mathf.Clamp(value, min, max);
        this.updating = false;

        if (this.sliderMinLabel != null)
        {
            this.sliderMinLabel.text = min.ToString();
        }

        if (this.sliderMaxLabel != null)
        {
            this.sliderMaxLabel.text = max.ToString();
        }

        this.UpdateSliderValueLabel(this.slider.value);

        this.slider.onValueChanged.AddListener(this.OnSliderChanged);
    }

    private void UpdateSliderValueLabel(float value)
    {
        if (this.sliderValueLabel == null)
        {
            return;
        }

        if (this.field.FieldType == typeof(int))
        {
            this.sliderValueLabel.text = Mathf.RoundToInt(value).ToString();
        }
        else
        {
            this.sliderValueLabel.text = value.ToString("0.##");
        }
    }

    private object GetCurrentValue()
    {
        if (this.usingBuilderValues)
        {
            if (this.builderTargets == null ||
                this.builderTargets.Count == 0)
            {
                return null;
            }

            return this.builderTargets[0]
                .GetRuntimeEditableValue(this.builderFieldName);
        }

        if (this.targets == null ||
            this.targets.Count == 0)
        {
            return null;
        }

        return this.field.GetValue(this.targets[0]);
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
            this.Refresh();
            return;
        }

        this.ApplyValue(converted);
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

        object converted;

        if (this.field.FieldType == typeof(int))
        {
            converted = Mathf.RoundToInt(value);
        }
        else
        {
            converted = value;
        }

        this.UpdateSliderValueLabel(value);
        this.ApplyValue(converted);
    }

    private void ApplyValue(object value)
    {
        if (this.usingBuilderValues)
        {
            if (this.builderTargets == null)
            {
                return;
            }

            foreach (BuilderEntity target in this.builderTargets)
            {
                if (target == null)
                {
                    continue;
                }

                target.SetRuntimeEditableValue(
                    this.builderFieldName,
                    value);
            }

            this.onValueChanged?.Invoke();
            return;
        }

        if (this.targets == null)
        {
            return;
        }

        foreach (object target in this.targets)
        {
            if (target == null)
            {
                continue;
            }

            this.field.SetValue(target, value);
        }

        this.onValueChanged?.Invoke();
    }

    private void Refresh()
    {
        this.Setup();
    }

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

        if (type == typeof(int))
        {
            if (int.TryParse(value, out int intValue))
            {
                result = intValue;
                return true;
            }

            return false;
        }

        if (type == typeof(float))
        {
            if (float.TryParse(value, out float floatValue))
            {
                result = floatValue;
                return true;
            }

            return false;
        }

        if (type == typeof(bool))
        {
            if (bool.TryParse(value, out bool boolValue))
            {
                result = boolValue;
                return true;
            }
        }

        return false;
    }

    private void BeginReferenceSelection()
    {
        if (RuntimePropertyEditor.Instance == null)
        {
            return;
        }

        RuntimePropertyEditor.Instance.BeginReferenceSelection(this);
    }

    private void BeginReferenceSelection(int index)
    {
        if (RuntimePropertyEditor.Instance == null)
        {
            return;
        }

        RuntimePropertyEditor.Instance.BeginReferenceSelection(
            this,
            index);
    }

    private void UpdateReferenceButton()
    {
        if (this.referenceButton == null ||
            this.field == null)
        {
            return;
        }

        SaveableEntity reference =
            this.GetCurrentValue() as SaveableEntity;

        TextMeshProUGUI buttonText =
            this.referenceButton.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText == null)
        {
            return;
        }

        if (reference == null)
        {
            buttonText.text = "Select Reference";
        }
        else
        {
            buttonText.text = reference.name;
        }
    }

    private void UpdateReferenceButtons()
    {
        this.SetupReferenceList();
    }

    private bool IsReferenceList()
    {
        if (this.field == null ||
            !typeof(IList).IsAssignableFrom(this.field.FieldType) ||
            !this.field.FieldType.IsGenericType)
        {
            return false;
        }

        Type elementType =
            this.field.FieldType.GetGenericArguments()[0];

        return typeof(SaveableEntity).IsAssignableFrom(elementType);
    }
}
