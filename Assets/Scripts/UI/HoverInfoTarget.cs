using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shows the hover popup for the UI element it is attached to after the
/// player rested the cursor on it.
/// </summary>
public class HoverInfoTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private HoverInfo info = HoverInfo.Empty;

    /// <summary>
    /// Gets the info shown while the player hovers over this element.
    /// </summary>
    public HoverInfo Info => this.info;

    /// <summary>
    /// Adds hover info to a UI element, reusing the target that is already there.
    /// </summary>
    /// <param name="target">The element the player hovers over.</param>
    /// <param name="info">The info to show.</param>
    /// <returns>The hover target on the element, or null when there is no element.</returns>
    public static HoverInfoTarget Attach(GameObject target, HoverInfo info)
    {
        if (target == null)
        {
            return null;
        }

        HoverInfoTarget hoverTarget = target.GetComponent<HoverInfoTarget>();

        if (hoverTarget == null)
        {
            hoverTarget = target.AddComponent<HoverInfoTarget>();
        }

        hoverTarget.SetInfo(info);
        return hoverTarget;
    }

    /// <summary>
    /// Sets the info shown while the player hovers over this element.
    /// </summary>
    /// <param name="info">The info to show. Null clears it.</param>
    public void SetInfo(HoverInfo info)
    {
        this.info = info ?? HoverInfo.Empty;

        if (HoverTooltipManager.Instance != null)
        {
            HoverTooltipManager.Instance.Refresh(this);
        }
    }

    /// <summary>
    /// Starts the hover delay when the cursor enters this element.
    /// </summary>
    /// <param name="eventData">The pointer event data.</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (HoverTooltipManager.Instance != null)
        {
            HoverTooltipManager.Instance.BeginHover(this, eventData.position);
        }
    }

    /// <summary>
    /// Hides the popup when the cursor leaves this element.
    /// </summary>
    /// <param name="eventData">The pointer event data.</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (HoverTooltipManager.Instance != null)
        {
            HoverTooltipManager.Instance.EndHover(this);
        }
    }

    /// <summary>
    /// Hides the popup when this element is hidden while the cursor is on it.
    /// </summary>
    private void OnDisable()
    {
        if (HoverTooltipManager.Instance != null)
        {
            HoverTooltipManager.Instance.EndHover(this);
        }
    }
}
