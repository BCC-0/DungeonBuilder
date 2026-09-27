using System;
using UnityEngine;

/// <summary>
/// Defines how a SaveableEntity reference behaves.
/// </summary>
public enum ReferenceMode
{
    Shared,
    Consuming
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
    public SaveFieldAttribute(
        float min = float.MinValue,
        float max = float.MaxValue,
        ReferenceMode referenceMode = ReferenceMode.Consuming)
    {
        this.HasRange = min != float.MinValue || max != float.MaxValue;
        this.Min = min;
        this.Max = max;
        this.ReferenceMode = referenceMode;
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
}