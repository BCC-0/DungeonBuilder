using System;
using UnityEngine;

/// <summary>
/// Marks a field as editable in runtime copies.
/// Only fields with this will show in the ItemObject runtime editor.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RuntimeEditableAttribute : PropertyAttribute
{
}
