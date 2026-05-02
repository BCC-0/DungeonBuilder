using UnityEngine;

/// <summary>
/// Base class for all the editor controllers (tiles and entities).
/// Handles shared input state, pointer tracking, and tool application flow.
/// </summary>
public abstract class EditorControllerBase : MonoBehaviour
{
    private Vector3 currentPos;
    private bool primaryHolding;
    private bool secondaryHolding;

    /// <summary>
    /// Gets the current world position of the pointer.
    /// </summary>
    protected Vector3 CurrentPos => this.currentPos;

    /// <summary>
    /// Gets a value indicating whether the primary mouse button (or input) is held.
    /// </summary>
    protected bool PrimaryHolding => this.primaryHolding;

    /// <summary>
    /// Gets a value indicating whether the secondary mouse button (or input) is held.
    /// </summary>
    protected bool SecondaryHolding => this.secondaryHolding;

    /// <summary>
    /// Updates the pointer world position.
    /// </summary>
    /// <param name="worldPos">The world position of the pointer.</param>
    public void OnPointerMoved(Vector3 worldPos)
    {
        this.currentPos = worldPos;
    }

    /// <summary>
    /// Called when the primary input is pressed.
    /// </summary>
    public void OnPrimaryDown()
    {
        this.primaryHolding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Called when the primary input is released.
    /// </summary>
    public void OnPrimaryUp()
    {
        this.primaryHolding = false;
    }

    /// <summary>
    /// Called when the secondary input is pressed.
    /// </summary>
    public void OnSecondaryDown()
    {
        this.secondaryHolding = true;
        this.ApplyTool();
    }

    /// <summary>
    /// Called when the secondary input is released.
    /// </summary>
    public void OnSecondaryUp()
    {
        this.secondaryHolding = false;
    }

    /// <summary>
    /// Unity update loop. Applies tool continuously while input is held.
    /// </summary>
    protected virtual void Update()
    {
        if (this.primaryHolding || this.secondaryHolding)
        {
            this.ApplyTool();
        }
    }

    /// <summary>
    /// Resolves the current editor tool and forwards execution to derived classes.
    /// </summary>
    protected void ApplyTool()
    {
        EditorTool tool = MapEditorManager.Instance.CurrentTool;
        this.OnApplyTool(tool);
    }

    /// <summary>
    /// Implemented by derived classes to define tool behavior.
    /// </summary>
    /// <param name="tool">The active editor tool.</param>
    protected abstract void OnApplyTool(EditorTool tool);
}