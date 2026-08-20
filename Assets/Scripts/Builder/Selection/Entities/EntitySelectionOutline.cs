using UnityEngine;

/// <summary>
/// Enables an entity that is selectable and provides its selection visual.
/// </summary>
public class EntitySelectionOutline : MonoBehaviour
{
    private SpriteRenderer selectionRenderer;
    private Animator animator;

    /// <summary>
    /// Shows the selection visual.
    /// </summary>
    public void ShowSelection()
    {
        this.selectionRenderer.enabled = true;
        this.animator.enabled = true;
    }

    /// <summary>
    /// Hides the selection visual.
    /// </summary>
    public void HideSelection()
    {
        this.selectionRenderer.enabled = false;
        this.animator.enabled = false;
    }

    /// <summary>
    /// Disable the selection renderer at the start.
    /// </summary>
    private void Awake()
    {
        this.selectionRenderer = this.GetComponent<SpriteRenderer>();
        this.animator = this.GetComponent<Animator>();

        this.selectionRenderer.enabled = false;
        this.animator.enabled = false;
    }
}