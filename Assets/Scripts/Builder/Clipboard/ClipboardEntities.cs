using System.Collections.Generic;

/// <summary>
/// A copied set of builder entities, positioned relative to the clipboard's own origin cell.
/// </summary>
public class ClipboardEntities
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardEntities"/> class.
    /// </summary>
    /// <param name="entries">The copied entities.</param>
    public ClipboardEntities(List<ClipboardEntityEntry> entries)
    {
        this.Entries = entries;
    }

    /// <summary>
    /// Gets the copied entities.
    /// </summary>
    public List<ClipboardEntityEntry> Entries { get; }
}
