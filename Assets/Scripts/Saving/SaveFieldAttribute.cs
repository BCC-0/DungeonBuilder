using System;
using UnityEngine;

/// <summary>
/// Defines how a SaveableEntity reference behaves.
/// </summary>
public enum ReferenceMode
{
    /// <summary>
    /// A shared reference will still exist in the world, meaning other entities can also reference it.
    /// </summary>
    Shared,

    /// <summary>
    /// A consumed reference will 'disappear' and can only be referenced by one entity.+
    /// </summary>
    Consuming,
}

/// <summary>
/// Marks a field as savable and optionally defines its valid range.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SaveFieldAttribute : PropertyAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveFieldAttribute"/> class.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="referenceMode">How a SaveableEntity reference should behave.</param>
    /// <param name="minReferences">How many references this field should have minimally.</param>
    /// <param name="maxReferences">How many references this field can have.</param>
    /// <param name="priority">The inspector display priority. Higher values appear first.</param>
    /// <param name="displayName">The custom inspector display name.</param>
    /// <param name="description">The description shown when the player hovers over the field.</param>
    public SaveFieldAttribute(
        float min = float.MinValue,
        float max = float.MaxValue,
        ReferenceMode referenceMode = ReferenceMode.Consuming,
        int minReferences = 1,
        int maxReferences = 1,
        int priority = 0,
        string displayName = null,
        string description = null)
    {
        this.HasRange = min != float.MinValue || max != float.MaxValue;
        this.Min = min;
        this.Max = max;
        this.ReferenceMode = referenceMode;
        this.MinReferences = minReferences;
        this.MaxReferences = maxReferences;
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
    /// Gets how a SaveableEntity reference should behave.
    /// </summary>
    public ReferenceMode ReferenceMode { get; }

    /// <summary>
    /// Gets how many SaveableEntity references this should minimally contain.
    /// </summary>
    public int MinReferences { get; }

    /// <summary>
    /// Gets how many SaveableEntity references this could contain.
    /// </summary>
    public int MaxReferences { get; }

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
