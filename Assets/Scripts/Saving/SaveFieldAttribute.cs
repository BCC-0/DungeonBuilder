using System;
using UnityEngine;

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
    /// <param name="referenceOnly">Whether a SaveableEntity reference should remain in the world.</param>
    public SaveFieldAttribute(
        float min = float.MinValue,
        float max = float.MaxValue,
        bool referenceOnly = false)
    {
        this.HasRange = min != float.MinValue || max != float.MaxValue;
        this.Min = min;
        this.Max = max;
        this.ReferenceOnly = referenceOnly;
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
    /// Gets a value indicating whether a SaveableEntity reference should remain in the world.
    /// </summary>
    public bool ReferenceOnly { get; }
}