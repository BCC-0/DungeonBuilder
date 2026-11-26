/// <summary>
/// How a click or box-select combines with the selection that already exists.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// The picked items replace the selection.
    /// </summary>
    Replace,

    /// <summary>
    /// The picked items are added to the selection.
    /// </summary>
    Add,

    /// <summary>
    /// The picked items are added to the selection, or removed from it when they
    /// are already selected.
    /// </summary>
    Toggle,
}
