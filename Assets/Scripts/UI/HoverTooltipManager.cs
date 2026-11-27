using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the hover popup after the cursor rested on a UI element for a while.
/// Add one to the builder scene and assign the popup prefab.
/// </summary>
public class HoverTooltipManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The popup prefab with the HoverTooltip component on its root.")]
    private HoverTooltip tooltipPrefab;

    [SerializeField]
    [Tooltip("Optional. The popup is parented here. Defaults to this object.")]
    private Transform tooltipParent;

    [SerializeField]
    [Tooltip("Seconds the cursor has to rest on an element before the popup shows.")]
    private float hoverDelay = 0.6f;

    private HoverTooltip tooltip;
    private HoverInfoTarget hoveredTarget;
    private float hoverTime;
    private bool isShown;
    private string shownName = string.Empty;
    private string shownDescription = string.Empty;
    private Vector2 lastPointerPosition;

    /// <summary>
    /// Gets the hover tooltip manager of the current scene.
    /// </summary>
    public static HoverTooltipManager Instance { get; private set; }

    /// <summary>
    /// Starts the hover delay for a UI element.
    /// </summary>
    /// <param name="target">The element the cursor entered.</param>
    /// <param name="screenPosition">The cursor position in screen pixels.</param>
    public void BeginHover(HoverInfoTarget target, Vector2 screenPosition)
    {
        if (target == null ||
            !target.Info.HasContent)
        {
            return;
        }

        if (this.hoveredTarget != target)
        {
            this.Hide();
        }

        this.hoveredTarget = target;
        this.lastPointerPosition = screenPosition;
        this.hoverTime = 0f;
    }

    /// <summary>
    /// Hides the popup when the cursor leaves a UI element.
    /// </summary>
    /// <param name="target">The element the cursor left.</param>
    public void EndHover(HoverInfoTarget target)
    {
        if (this.hoveredTarget != target)
        {
            return;
        }

        this.hoveredTarget = null;
        this.Hide();
    }

    /// <summary>
    /// Updates the popup when the info of the hovered element changed.
    /// </summary>
    /// <param name="target">The element whose info changed.</param>
    public void Refresh(HoverInfoTarget target)
    {
        if (this.hoveredTarget != target)
        {
            return;
        }

        HoverInfo info = target.Info;

        if (!info.HasContent)
        {
            this.hoveredTarget = null;
            this.Hide();
            return;
        }

        if (this.isShown)
        {
            this.Show(info);
        }
    }

    /// <summary>
    /// Counts the hover time and keeps the popup next to the cursor.
    /// </summary>
    private void Update()
    {
        if (this.hoveredTarget == null)
        {
            if (this.isShown)
            {
                this.Hide();
            }

            return;
        }

        HoverInfo info = this.hoveredTarget.Info;

        if (!this.hoveredTarget.isActiveAndEnabled ||
            !info.HasContent)
        {
            this.EndHover(this.hoveredTarget);
            return;
        }

        if (!this.isShown)
        {
            this.hoverTime += Time.unscaledDeltaTime;

            if (this.hoverTime < this.hoverDelay)
            {
                return;
            }

            this.Show(info);
            return;
        }

        if (info.Name != this.shownName ||
            info.Description != this.shownDescription)
        {
            this.Show(info);
        }

        this.UpdatePosition();
    }

    /// <summary>
    /// Shows the popup with the given info.
    /// </summary>
    /// <param name="info">The info to show.</param>
    private void Show(HoverInfo info)
    {
        HoverTooltip popup = this.GetTooltip();

        if (popup == null)
        {
            return;
        }

        popup.SetVisible(true);
        popup.SetContent(info);

        this.isShown = true;
        this.shownName = info.Name;
        this.shownDescription = info.Description;

        this.UpdatePosition();
    }

    /// <summary>
    /// Hides the popup and forgets what it showed.
    /// </summary>
    private void Hide()
    {
        this.isShown = false;
        this.shownName = string.Empty;
        this.shownDescription = string.Empty;

        if (this.tooltip != null)
        {
            this.tooltip.SetVisible(false);
        }
    }

    /// <summary>
    /// Creates the popup on first use.
    /// </summary>
    /// <returns>The popup, or null when no prefab is assigned.</returns>
    private HoverTooltip GetTooltip()
    {
        if (this.tooltip != null)
        {
            return this.tooltip;
        }

        if (this.tooltipPrefab == null)
        {
            Debug.LogWarning(
                "HoverTooltipManager: No tooltip prefab is assigned.",
                this);

            return null;
        }

        Transform parent = this.tooltipParent != null
            ? this.tooltipParent
            : this.transform;

        this.tooltip = Instantiate(this.tooltipPrefab, parent);
        this.tooltip.name = this.tooltipPrefab.name;
        this.tooltip.gameObject.SetActive(false);

        return this.tooltip;
    }

    /// <summary>
    /// Places the popup next to the cursor.
    /// </summary>
    private void UpdatePosition()
    {
        if (this.tooltip == null)
        {
            return;
        }

        if (Mouse.current != null)
        {
            this.lastPointerPosition = Mouse.current.position.ReadValue();
        }

        this.tooltip.SetScreenPosition(this.lastPointerPosition);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (this.tooltip != null)
        {
            Destroy(this.tooltip.gameObject);
        }
    }
}
