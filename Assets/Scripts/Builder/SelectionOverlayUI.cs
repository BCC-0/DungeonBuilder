using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Draws the selection when dragging.
/// </summary>
public class SelectionOverlayUI : MonoBehaviour
{
    private VisualElement root;
    private VisualElement selectionBox;

    /// <summary>
    /// Enables the selection overlay.
    /// </summary>
    /// <param name="visible">A value indicating visibility.</param>
    public void SetVisible(bool visible)
    {
        this.selectionBox.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// Creates a rectangle between corners A and B.
    /// </summary>
    /// <param name="screenA">Point A.</param>
    /// <param name="screenB">Point B.</param>
    public void SetScreenRect(Vector2 screenA, Vector2 screenB)
    {
        Vector2 min = Vector2.Min(screenA, screenB);
        Vector2 max = Vector2.Max(screenA, screenB);

        this.selectionBox.style.left = min.x;
        this.selectionBox.style.top = min.y;
        this.selectionBox.style.width = max.x - min.x;
        this.selectionBox.style.height = max.y - min.y;
    }

    private void Start()
    {
        var uiDocument = this.GetComponent<UIDocument>();
        this.root = uiDocument.rootVisualElement;

        // Create selection rectangle
        this.selectionBox = new VisualElement();
        this.selectionBox.style.position = Position.Absolute;

        this.selectionBox.style.backgroundColor = new Color(0f, 0.5f, 1f, 0.15f);
        this.selectionBox.style.borderLeftWidth = 2;
        this.selectionBox.style.borderRightWidth = 2;
        this.selectionBox.style.borderTopWidth = 2;
        this.selectionBox.style.borderBottomWidth = 2;

        this.selectionBox.style.borderLeftColor = Color.cyan;
        this.selectionBox.style.borderRightColor = Color.cyan;
        this.selectionBox.style.borderTopColor = Color.cyan;
        this.selectionBox.style.borderBottomColor = Color.cyan;

        this.selectionBox.style.display = DisplayStyle.None;
        this.selectionBox.pickingMode = PickingMode.Ignore;

        this.root.style.flexGrow = 1;
        this.root.style.position = Position.Absolute;
        this.root.style.left = 0;
        this.root.style.top = 0;
        this.root.style.right = 0;
        this.root.style.bottom = 0;

        this.root.Add(this.selectionBox);
    }
}