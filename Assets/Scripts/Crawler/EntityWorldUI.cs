using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Can be called by entities to display a name and/or health bar at a given position.
/// </summary>
public class EntityWorldUI
{
    private readonly RectTransform uiTransform;
    private readonly Camera worldCamera;
    private readonly Canvas canvas;

    private readonly SpriteFontText nameText;
    private readonly Slider healthSlider;

    private readonly Vector3 offset;

    private string displayName;
    private float? currentHealth;
    private float? maxHealth;

    private bool isBehindCamera;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityWorldUI"/> class.
    /// </summary>
    /// <param name="prefab">The prefab containing the health slider and name text.</param>
    /// <param name="canvas">The canvas to use for the UI prefab.</param>
    /// <param name="worldCamera">The main camera.</param>
    /// <param name="offset">By how much the UI should be offset.</param>
    /// <param name="parent">The parent of the UI prefab.</param>
    public EntityWorldUI(
        GameObject prefab,
        Canvas canvas,
        Camera worldCamera,
        Vector3 offset,
        Transform parent = null)
    {
        this.canvas = canvas;
        this.worldCamera = worldCamera;
        this.offset = offset;

        GameObject instance = Object.Instantiate(
            prefab,
            parent != null ? parent : canvas.transform);

        this.uiTransform = instance.GetComponent<RectTransform>();

        this.nameText = instance.transform
            .Find("DisplayName")
            ?.GetComponent<SpriteFontText>();

        this.healthSlider = instance.transform
            .Find("Health")
            ?.GetComponent<Slider>();

        this.uiTransform.SetAsFirstSibling();
        this.uiTransform.gameObject.SetActive(false);
    }

    /// <summary>
    /// Gets a value indicating whether the world UI prefabs are globally enabled.
    /// </summary>
    public static bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Enables or disables all entity world UI instances.
    /// </summary>
    /// <param name="enabled">A value indicating whether the UI prefab should be enabled.</param>
    public static void SetGlobalEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    /// <summary>
    /// Updates the displayed name and health.
    /// </summary>
    /// <param name="displayName">The display name to set.</param>
    /// <param name="currentHealth">The current health of the entity.</param>
    /// <param name="maxHealth">The max health of the entity.</param>
    public void UpdateUI(
        string displayName = null,
        float? currentHealth = null,
        float? maxHealth = null)
    {
        this.displayName = displayName;
        this.currentHealth = currentHealth;
        this.maxHealth = maxHealth;

        bool showName = !string.IsNullOrEmpty(displayName);

        bool showHealth = currentHealth.HasValue &&
                          maxHealth.HasValue &&
                          maxHealth.Value > 0;

        if (this.nameText != null)
        {
            this.nameText.gameObject.SetActive(showName);

            if (showName)
            {
                this.nameText.SetText(displayName);
            }
        }

        if (this.healthSlider != null)
        {
            this.healthSlider.gameObject.SetActive(showHealth);

            if (showHealth)
            {
                this.healthSlider.maxValue = maxHealth.Value;
                this.healthSlider.value = Mathf.Clamp(
                    maxHealth.Value - currentHealth.Value, 0, maxHealth.Value);
            }
        }

        this.UpdateVisibility();
    }

    /// <summary>
    /// Updates the position of the UI to follow the entity.
    /// </summary>
    /// <param name="worldPosition">The world position of the entity.</param>
    public void UpdateUIPosition(Vector3 worldPosition)
    {
        if (!IsEnabled || this.uiTransform == null)
        {
            this.uiTransform.gameObject.SetActive(false);
            return;
        }

        bool hasContent =
            !string.IsNullOrEmpty(this.displayName) ||
            (this.currentHealth.HasValue &&
             this.maxHealth.HasValue &&
             this.maxHealth.Value > 0);

        if (!hasContent)
        {
            this.uiTransform.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPosition = this.worldCamera.WorldToScreenPoint(
            worldPosition + this.offset);

        // Hide UI when the entity is behind the camera.
        this.isBehindCamera = screenPosition.z <= 0;

        this.UpdateVisibility();

        if (this.isBehindCamera)
        {
            return;
        }

        RectTransform canvasRect = this.canvas.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            this.canvas.worldCamera,
            out Vector2 localPoint);

        this.uiTransform.localPosition = localPoint;
    }

    /// <summary>
    /// Sets the visibility of the UI prefab.
    /// </summary>
    /// <param name="visible">A value indicating whether the UI prefab should be visible.</param>
    public void SetVisible(bool visible)
    {
        if (this.uiTransform != null)
        {
            this.uiTransform.gameObject.SetActive(
                visible && IsEnabled);
        }
    }

    /// <summary>
    /// Destroys the UI prefab.
    /// </summary>
    public void Destroy()
    {
        if (this.uiTransform != null)
        {
            Object.Destroy(this.uiTransform.gameObject);
        }
    }

    private void UpdateVisibility()
    {
        if (this.uiTransform == null)
        {
            return;
        }

        bool hasContent =
            !string.IsNullOrEmpty(this.displayName) ||
            (this.currentHealth.HasValue &&
             this.maxHealth.HasValue &&
             this.maxHealth.Value > 0);

        this.uiTransform.gameObject.SetActive(
            IsEnabled && hasContent && !this.isBehindCamera);
    }
}
