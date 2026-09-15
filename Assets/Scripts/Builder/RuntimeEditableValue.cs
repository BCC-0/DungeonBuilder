using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Stores a runtime-editable field and its current value.
/// </summary>
public class RuntimeEditableValue
{
    /// <summary>
    /// The original field information from the source object.
    /// </summary>
    public FieldInfo Field { get; }

    /// <summary>
    /// The current runtime value of the field.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Creates a runtime-editable value.
    /// </summary>
    /// <param name="field">The source field.</param>
    /// <param name="value">The current value.</param>
    public RuntimeEditableValue(
        FieldInfo field,
        object value)
    {
        this.Field = field;
        this.Value = value;
    }
}
