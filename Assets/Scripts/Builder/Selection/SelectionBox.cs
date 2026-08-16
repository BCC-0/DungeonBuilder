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
    private Vector2 startPosition;

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

        this.startPosition = startPos;
    }

    /// <summary>
    /// Sizes the selection box with the current drag position.
    /// </summary>
    /// <param name="currentPos">The current drag position.</param>
    public void SetPosition(Vector2 currentPos)
    {
        Vector2 min = Vector2.Min(this.startPosition, currentPos);
        Vector2 max = Vector2.Max(this.startPosition, currentPos);

        this.rectTransform.anchoredPosition = min;
        this.rectTransform.sizeDelta = max - min;
    }

    /// <summary>
    /// Sets the images of the selection box to invisible.
    /// </summary>
    public void StopSelection()
    {
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
