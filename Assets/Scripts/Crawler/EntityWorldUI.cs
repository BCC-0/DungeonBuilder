using System.Collections.Generic;
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
    private readonly bool showVitals;
    private readonly List<Image> statusIcons = new List<Image>();

    private readonly Vector3 offset;

    private Transform statusContainer;
    private GameObject statusIconTemplate;

    private string displayName;
    private float? currentHealth;
    private float? maxHealth;

    private bool isBehindCamera;
    private int statusEffectCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityWorldUI"/> class.
    /// </summary>
    /// <param name="prefab">The prefab containing the health slider and name text.</param>
    /// <param name="canvas">The canvas to use for the UI prefab.</param>
    /// <param name="worldCamera">The main camera.</param>
    /// <param name="offset">By how much the UI should be offset.</param>
    /// <param name="parent">The parent of the UI prefab.</param>
    /// <param name="includeVitals">
    /// Whether the name and health of the entity are shown. False turns this
    /// UI into a status effect display only.
    /// </param>
    public EntityWorldUI(
        GameObject prefab,
        Canvas canvas,
        Camera worldCamera,
        Vector3 offset,
        Transform parent = null,
        bool includeVitals = true)
    {
        this.canvas = canvas;
        this.worldCamera = worldCamera;
        this.offset = offset;
        this.showVitals = includeVitals;

        GameObject instance = Object.Instantiate(
            prefab,
            parent != null ? parent : canvas.transform);

        this.uiTransform = instance.GetComponent<RectTransform>();

        Transform statusRoot = instance.transform.Find("StatusEffects");

        if (statusRoot != null)
        {
            this.statusContainer = statusRoot;

            Transform iconTemplate = statusRoot.Find("StatusIcon");

            if (iconTemplate != null)
            {
                this.statusIconTemplate = iconTemplate.gameObject;
                this.statusIconTemplate.SetActive(false);
            }
        }

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
    /// Gets a value indicating whether the UI has anything to show.
    /// </summary>
    private bool HasContent =>
        this.statusEffectCount > 0 ||
        (this.showVitals &&
            (!string.IsNullOrEmpty(this.displayName) ||
             (this.currentHealth.HasValue &&
              this.maxHealth.HasValue &&
              this.maxHealth.Value > 0)));

    /// <summary>
    /// Enables or disables all entity world UI instances.
    /// </summary>
    /// <param name="enabled">A value indicating whether the UI prefab should be enabled.</param>
    public static void SetGlobalEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    /// <summary>
    /// Finds a canvas that belongs to the scene of the given object, so world
    /// UI is not parented to a canvas of another scene, such as the loading
    /// overlay that survives a scene change.
    /// </summary>
    /// <param name="owner">The object the UI belongs to.</param>
    /// <returns>The canvas to parent the world UI to, or null when there is none.</returns>
    public static Canvas FindWorldUICanvas(GameObject owner)
    {
        Canvas fallback = null;

        foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (owner != null && candidate.gameObject.scene == owner.scene)
            {
                return candidate;
            }

            if (fallback == null &&
                candidate.GetComponentInParent<BuilderLoadingOverlay>() == null)
            {
                fallback = candidate;
            }
        }

        return fallback;
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

        if (this.showVitals && this.nameText != null)
        {
            this.nameText.gameObject.SetActive(showName);

            if (showName)
            {
                this.nameText.SetText(displayName);
            }
        }

        if (this.showVitals && this.healthSlider != null)
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
    /// Shows an icon for every active status effect.
    /// </summary>
    /// <param name="effects">The active effects, in display order.</param>
    public void SetStatusEffects(IReadOnlyList<StatusEffectDefinition> effects)
    {
        int count = effects != null ? effects.Count : 0;
        this.statusEffectCount = count;

        if (this.statusContainer != null)
        {
            this.statusContainer.gameObject.SetActive(count > 0);
        }

        if (this.statusContainer == null || this.statusIconTemplate == null)
        {
            this.UpdateVisibility();
            return;
        }

        while (this.statusIcons.Count < count)
        {
            GameObject iconObject = Object.Instantiate(
                this.statusIconTemplate,
                this.statusContainer);

            iconObject.name = "StatusIcon" + this.statusIcons.Count;
            this.statusIcons.Add(iconObject.GetComponent<Image>());
        }

        for (int i = 0; i < this.statusIcons.Count; i++)
        {
            Image icon = this.statusIcons[i];

            if (icon == null)
            {
                continue;
            }

            bool visible = i < count;
            icon.gameObject.SetActive(visible);

            if (visible)
            {
                StatusEffectDefinition definition = effects[i];
                icon.sprite = definition != null ? definition.Icon : null;
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

        if (!this.HasContent)
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

        this.uiTransform.gameObject.SetActive(
            IsEnabled && this.HasContent && !this.isBehindCamera);
    }
}
