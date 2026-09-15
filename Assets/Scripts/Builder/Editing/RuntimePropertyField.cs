using System;
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

    // TODO: Add a slider with a min and max value if they are set for the runtime editable attributes.
    // TODO: Add a way to reference other objects in the map.
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

    private void Setup()
    {
        if (this.field == null)
        {
            return;
        }

        Type type = this.field.FieldType;

        if (this.inputField != null)
        {
            this.inputField.gameObject.SetActive(type != typeof(bool));
            this.inputField.onEndEdit.RemoveAllListeners();
        }

        if (this.toggle != null)
        {
            this.toggle.gameObject.SetActive(type == typeof(bool));
            this.toggle.onValueChanged.RemoveAllListeners();
        }

        if (type == typeof(bool))
        {
            this.SetupBool();
        }
        else
        {
            this.SetupInput();
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
        this.inputField.text = value != null ? value.ToString() : string.Empty;
        this.updating = false;

        this.inputField.onEndEdit.AddListener(this.OnInputChanged);
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

        // Existing TileData reflection behavior.
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
}