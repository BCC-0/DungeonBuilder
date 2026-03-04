using System;
using UnityEngine;

/// <summary>
/// Marks a field as savable.
/// All fields with this attribute should be saved in a map object.
/// Used by SaveUtility.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SaveFieldAttribute : PropertyAttribute
{
}
