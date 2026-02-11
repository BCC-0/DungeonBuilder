using UnityEngine;

/// <summary>
/// The base class for tool items.
/// Tools are equippable items used for utility purposes, such as mining, chopping, or interacting with the environment.
/// </summary>
public abstract class Tool : EquippableItem
{
    [Header("Tool Stats")]
    [SerializeField]
    private float efficiency;

    /// <summary>
    /// Gets the efficiency of this tool.
    /// Efficiency can be used to calculate speed of action, effectiveness, etc.
    /// </summary>
    public float Efficiency => this.efficiency;

    /// <summary>
    /// Tool action, can check first if it is allowed here.
    /// Default checks the CanUse action.
    /// </summary>
    protected override void PerformAction()
    {
        if (!this.CanUse())
        {
            return;
        }

        this.PerformToolAction();
    }

    /// <summary>
    /// Executes the tool action.
    /// Must be implemented in implementations.
    /// </summary>
    protected abstract void PerformToolAction();
}
