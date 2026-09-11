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
        this.onValueChanged = onValueChanged;

        if (this.label != null)
        {
            this.label.text = displayName;
        }

        this.Setup();
    }

    private void Setup()
    {
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

        bool value = Convert.ToBoolean(
            this.field.GetValue(this.targets[0]));

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

        object value = this.field.GetValue(this.targets[0]);

        this.updating = true;
        this.inputField.text = value != null ? value.ToString() : string.Empty;
        this.updating = false;

        this.inputField.onEndEdit.AddListener(this.OnInputChanged);
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