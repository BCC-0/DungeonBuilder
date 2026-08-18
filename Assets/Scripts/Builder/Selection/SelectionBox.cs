using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The selection box visualizes where the player has dragged to while selecting.
/// It sets the selection box to be visible and sizes it with the player mouse or finger movement.
/// </summary>
public class SelectionBox : MonoBehaviour
{
    [SerializeField]
    private Image[] images;

    [SerializeField]
    private RectTransform rectTransform;

    /// <summary>
    /// Visualizes the selection box starting at the given position.
    /// </summary>
    /// <param name="startPos">The position to start dragging at.</param>
    public void StartSelection(Vector2 startPos)
    {
        foreach (Image image in this.images)
        {
            image.enabled = true;
        }
        Debug.Log("Started selection");
        this.SetPosition(startPos, startPos);
    }

    /// <summary>
    /// Sizes the selection box between two UI-space points, recomputed every
    /// frame from world space so the box tracks correctly even if the camera moves.
    /// </summary>
    /// <param name="startPos">The current UI-space position of the drag's start corner.</param>
    /// <param name="currentPos">The current UI-space position of the drag's active corner.</param>
    public void SetPosition(Vector2 startPos, Vector2 currentPos)
    {
        Debug.Log("Set position:" + currentPos);
        Vector2 min = Vector2.Min(startPos, currentPos);
        Vector2 max = Vector2.Max(startPos, currentPos);
        this.rectTransform.anchoredPosition = min;
        this.rectTransform.sizeDelta = max - min;
    }

    /// <summary>
    /// Sets the images of the selection box to invisible.
    /// </summary>
    public void StopSelection()
    {
        Debug.Log("Stopped selection");
        foreach (Image image in this.images) { image.enabled = false; }
    }

    /// <summary>
    /// Disables the images at the start.
    /// </summary>
    private void Awake()
    {
        this.StopSelection();
    }
}