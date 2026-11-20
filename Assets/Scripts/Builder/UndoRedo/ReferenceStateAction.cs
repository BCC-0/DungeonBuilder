/// <summary>
/// A change of entity references and consumption, undone by restoring
/// the snapshot taken before the change.
/// </summary>
public class ReferenceStateAction : IUndoableAction
{
    private readonly ReferenceSnapshot before;
    private readonly ReferenceSnapshot after;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceStateAction"/> class.
    /// </summary>
    /// <param name="before">The state before the change.</param>
    /// <param name="after">The state after the change.</param>
    public ReferenceStateAction(
        ReferenceSnapshot before,
        ReferenceSnapshot after)
    {
        this.before = before;
        this.after = after;
    }

    /// <inheritdoc/>
    public void Undo()
    {
        this.before.Restore();
    }

    /// <inheritdoc/>
    public void Redo()
    {
        this.after.Restore();
    }
}
