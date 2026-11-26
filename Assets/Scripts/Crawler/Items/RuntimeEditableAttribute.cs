using System;
using UnityEngine;

/// <summary>
/// Marks a field as editable in runtime copies.
/// Only fields with this will show in the ItemObject runtime editor.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RuntimeEditableAttribute : PropertyAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeEditableAttribute"/> class.
    /// </summary>
    /// <param name="min">The minimum value. Usable for ints and floats.</param>
    /// <param name="max">The maximum value. Usable for ints and floats.</param>
    /// <param name="priority">The inspector display priority. Higher values appear first.</param>
    /// <param name="displayName">The custom inspector display name.</param>
    /// <param name="description">The description shown when the player hovers over the field.</param>
    public RuntimeEditableAttribute(
        float min = float.MinValue,
        float max = float.MaxValue,
        int priority = 0,
        string displayName = null,
        string description = null)
    {
        this.HasRange = min != float.MinValue || max != float.MaxValue;
        this.Min = min;
        this.Max = max;
        this.Priority = priority;
        this.DisplayName = displayName;
        this.Description = description;
    }

    /// <summary>
    /// Gets a value indicating whether this field has a min and max value set.
    /// </summary>
    public bool HasRange { get; }

    /// <summary>
    /// Gets the minimum value. Usable for ints and floats.
    /// </summary>
    public float Min { get; }

    /// <summary>
    /// Gets the maximum value. Usable for ints and floats.
    /// </summary>
    public float Max { get; }

    /// <summary>
    /// Gets the inspector display priority.
    /// Higher values appear first.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets the custom inspector display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the description shown when the player hovers over the field.
    /// </summary>
    public string Description { get; }
}
