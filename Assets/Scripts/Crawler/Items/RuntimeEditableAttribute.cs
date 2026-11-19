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
    /// <param name="priority">The inspector display priority. Higher values appear first.</param>
    /// <param name="displayName">The custom inspector display name.</param>
    public RuntimeEditableAttribute(
        int priority = 0,
        string displayName = null)
    {
        this.Priority = priority;
        this.DisplayName = displayName;
    }

    /// <summary>
    /// Gets the inspector display priority.
    /// Higher values appear first.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets the custom inspector display name.
    /// </summary>
    public string DisplayName { get; }
}
