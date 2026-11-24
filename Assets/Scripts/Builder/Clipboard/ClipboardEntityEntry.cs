using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One entity on the clipboard: what to rebuild, where it sat, and its editable values.
/// </summary>
public class ClipboardEntityEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardEntityEntry"/> class.
    /// </summary>
    /// <param name="source">The entity that was copied, used to remap references between copies.</param>
    /// <param name="prefabID">The prefab ID the copy is rebuilt from.</param>
    /// <param name="offset">The cell offset of the entity from the clipboard's origin cell.</param>
    /// <param name="fieldValues">The runtime editable values of the copied entity.</param>
    public ClipboardEntityEntry(
        SaveableEntity source,
        string prefabID,
        Vector2Int offset,
        Dictionary<string, object> fieldValues)
    {
        this.Source = source;
        this.PrefabID = prefabID;
        this.Offset = offset;
        this.FieldValues = fieldValues;
    }

    /// <summary>
    /// Gets the entity that was copied.
    /// </summary>
    public SaveableEntity Source { get; }

    /// <summary>
    /// Gets the prefab ID the copy is rebuilt from.
    /// </summary>
    public string PrefabID { get; }

    /// <summary>
    /// Gets the cell offset of the entity from the clipboard's origin cell.
    /// </summary>
    public Vector2Int Offset { get; }

    /// <summary>
    /// Gets the runtime editable values the copy is initialized with.
    /// </summary>
    public Dictionary<string, object> FieldValues { get; }
}
